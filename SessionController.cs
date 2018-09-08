using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using JetBrains.Annotations;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Helpers;
using NFive.SDK.Core.Models.Player;
using NFive.SDK.Server.Controllers;
using NFive.SDK.Server.Events;
using NFive.SDK.Server.Rpc;
using NFive.SessionManager.Storage;
using NFive.SessionManager.Models;

namespace NFive.SessionManager
{
	[PublicAPI]
	public class SessionController : ConfigurableController<Configuration>
	{
		private readonly List<Action> sessionCallbacks = new List<Action>();
		//private List<Session> sessions = new List<Session>();
		private ConcurrentBag<Session> sessions = new ConcurrentBag<Session>();
		private Dictionary<Session, Tuple<Task, CancellationTokenSource>> threads = new Dictionary<Session, Tuple<Task, CancellationTokenSource>>();

		public Player CurrentHost { get; private set; }

		public SessionController(ILogger logger, IEventManager events, IRpcHandler rpc, Configuration configuration) : base(logger, events, rpc, configuration)
		{
			API.EnableEnhancedHostSupport(true);

			this.Events.On("serverInitialized", OnSeverInitialized);

			this.Rpc.Event("hostingSession").OnRaw(new Action<Player>(OnHostingSession));
			this.Rpc.Event("HostedSession").OnRaw(new Action<Player>(OnHostedSession));

			this.Rpc.Event("playerConnecting").OnRaw(new Action<Player, string, CallbackDelegate, ExpandoObject>(Connecting));
			this.Rpc.Event("playerDropped").OnRaw(new Action<Player, string, CallbackDelegate>(Dropped));
			this.Rpc.Event("clientInitialize").On<string>(Initialize);
			this.Rpc.Event("clientInitialized").On(Initialized);
		}

		private void OnSeverInitialized()
		{
			using (var context = new StorageContext())
			using (var transaction = context.Database.BeginTransaction())
			{
				var disconnectedSessions = context.Sessions.Where(s => s.Disconnected == null).ToList();
				foreach (Session disconnectedSession in disconnectedSessions)
				{
					disconnectedSession.Disconnected = DateTime.UtcNow;
					disconnectedSession.DisconnectReason = "Session killed, disconnect time set to server boot";
					context.Sessions.AddOrUpdate(disconnectedSession);
				}

				context.SaveChanges();
				transaction.Commit();
			}
		}

		private async void OnHostingSession([FromSource] Player player)
		{
			if (this.CurrentHost != null)
			{
				player.TriggerEvent("sessionHostResult", "wait");

				this.sessionCallbacks.Add(() => player.TriggerEvent("sessionHostResult", "free"));

				return;
			}

			string hostId;

			try
			{
				hostId = API.GetHostId();
			}
			catch (NullReferenceException)
			{
				hostId = null;
			}

			if (!string.IsNullOrEmpty(hostId) && API.GetPlayerLastMsg(API.GetHostId()) < 1000)
			{
				player.TriggerEvent("sessionHostResult", "conflict");

				return;
			}

			this.sessionCallbacks.Clear();
			this.CurrentHost = player;

			this.Logger.Info($"Game host is now {this.CurrentHost.Handle} \"{this.CurrentHost.Name}\"");

			player.TriggerEvent("sessionHostResult", "go");

			await BaseScript.Delay(5000);

			this.sessionCallbacks.ForEach(c => c());
			this.CurrentHost = null;
		}

		private void OnHostedSession([FromSource] Player player)
		{
			if (this.CurrentHost != null && this.CurrentHost != player) return;

			this.sessionCallbacks.ForEach(c => c());
			this.CurrentHost = null;
		}

		public async void Connecting([FromSource] Player player, string playerName, CallbackDelegate drop, ExpandoObject callbacks)
		{
			var client = new Client(int.Parse(player.Handle));
			var deferrals = new Deferrals(callbacks, drop);
			Session session = null;
			User user = null;

			await this.Events.RaiseAsync("clientConnecting", client, deferrals);

			using (var context = new StorageContext())
			using (var transaction = context.Database.BeginTransaction())
			{
				context.Configuration.ProxyCreationEnabled = false;
				context.Configuration.LazyLoadingEnabled = false;

				try
				{
					user = context.Users.SingleOrDefault(u => u.SteamId == client.SteamId);

					if (user == default(User))
					{
						await this.Events.RaiseAsync("userCreating", client);
						// Create user
						user = new User
						{
							Id = GuidGenerator.GenerateTimeBasedGuid(),
							SteamId = client.SteamId,
							Name = client.Name
						};

						context.Users.Add(user);
						await this.Events.RaiseAsync("userCreated", client, user);
					}
					else
					{
						// Update name
						user.Name = client.Name;
					}

					await this.Events.RaiseAsync("sessionCreating", client);
					// Create session
					session = new Session
					{
						Id = GuidGenerator.GenerateTimeBasedGuid(),
						User = user,
						IpAddress = client.EndPoint
					};

					context.Sessions.Add(session);

					// Save changes
					await context.SaveChangesAsync();
					transaction.Commit();
				}
				catch (Exception ex)
				{
					transaction.Rollback();

					this.Logger.Error(ex);
				}
			}

			if (user == null || session == null) throw new Exception($"Failed to create session for {player.Name}");

			if (this.sessions.Any(s => s.User.Id == user.Id)) this.Reconnecting(client, session);

			this.sessions.Add(session);
			var threadCancellationToken = new CancellationTokenSource();
			this.threads.Add(
				session,
				new Tuple<Task, CancellationTokenSource>(Task.Factory.StartNew(() => MonitorSession(session, client), threadCancellationToken.Token), threadCancellationToken)
				);
			await this.Events.RaiseAsync("sessionCreated", client, session, deferrals);

			await this.Events.RaiseAsync("clientConnected", client, session);
			this.Logger.Info($"[{session.Id}] Player \"{user.Name}\" connected from {session.IpAddress}");
		}

		public async void Reconnecting(Client client, Session session)
		{
			this.Logger.Debug($"Client reconnecting: {session.UserId}");
			var oldSession = this.sessions.SingleOrDefault(s => s.User.Id == session.UserId);
			if (oldSession == null) return;
			var oldThread = this.threads.SingleOrDefault(t => t.Key.UserId == session.UserId).Key;
			this.Logger.Debug($"Thread List: {string.Join(", ", this.threads.Select(t => t.Key.UserId))}");
			if (oldThread != null)
			{
				this.Logger.Debug($"Disposing of old thread: {oldThread.User.Name}");
				this.threads[oldThread].Item2.Cancel();
				this.threads[oldThread].Item1.Wait();
				this.threads[oldThread].Item2.Dispose();
				this.threads[oldThread].Item1.Dispose();
				this.threads.Remove(oldThread);
			}
			this.sessions.TryTake(out oldSession);
			await this.Events.RaiseAsync("clientReconnected", client, session, oldSession);
		}

		public async void Dropped([FromSource] Player player, string disconnectMessage, CallbackDelegate drop)
		{
			this.Logger.Debug("Dropped()");
			this.Logger.Debug($"Player Dropped: {player.Name} | Reason: {disconnectMessage}");
			var client = new Client(int.Parse(player.Handle));

			this.Disconnecting(client, disconnectMessage);
		}

		public async void Disconnecting(Client client, string disconnectMessage)
		{
			await this.Events.RaiseAsync("clientDisconnecting", client);

			using (var context = new StorageContext())
			{
				context.Configuration.LazyLoadingEnabled = false;

				var user = context.Users.SingleOrDefault(u => u.SteamId == client.SteamId);
				if (user == null) throw new Exception($"No user to end for disconnected client \"{client.SteamId}\""); // TODO: SessionException

				var session = context.Sessions.OrderBy(s => s.Connected).FirstOrDefault(s => s.User.Id == user.Id && s.Disconnected == null && s.DisconnectReason == null);
				if (session == null) throw new Exception($"No session to end for disconnected user \"{user.Id}\""); // TODO: SessionException

				session.Disconnected = DateTime.UtcNow;
				session.DisconnectReason = disconnectMessage;

				await context.SaveChangesAsync();

				await this.Events.RaiseAsync("clientDisconnected", client, session);

				this.Logger.Info($"[{session.Id}] Player \"{user.Name}\" disconnected: {session.DisconnectReason}");
			}
		}

		public async void Initialize(IRpcEvent e, string clientVersion)
		{
			await this.Events.RaiseAsync("clientInitializing", e.Client);

			e.Reply(e.User);
		}

		public void Initialized(IRpcEvent e)
		{
			this.Events.Raise("clientInitialized", e.Client);
		}

		public async Task MonitorSession(Session session, Client client)
		{
			while (session.IsConnected && !this.threads[session].Item2.Token.IsCancellationRequested)
			{
				await BaseScript.Delay(100);
				if (API.GetPlayerLastMsg(client.Handle.ToString()) <= this.Configuration.ConnectionTimeout) continue;
				await this.Events.RaiseAsync("sessionTimedOut", client, session);
				session.Disconnected = DateTime.UtcNow;
				this.Disconnecting(client, "Session Timed Out");
			}

			this.Logger.Debug("Starting reconnect grace checks");

			while (DateTime.UtcNow.Subtract(session.Disconnected ?? DateTime.UtcNow).TotalMilliseconds < this.Configuration.ReconnectGrace && !this.threads[session].Item2.Token.IsCancellationRequested)
			{
				await BaseScript.Delay(100);
			}

			this.sessions.TryTake(out session);
		}
	}
}
