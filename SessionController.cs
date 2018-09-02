using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
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
	public class SessionController : Controller
	{
		private readonly List<Action> sessionCallbacks = new List<Action>();
		//private List<Session> sessions = new List<Session>();
		private ConcurrentBag<Session> sessions = new ConcurrentBag<Session>();

		public Player CurrentHost { get; private set; }

		public SessionController(ILogger logger, IEventManager events, IRpcHandler rpc) : base(logger, events, rpc)
		{
			API.EnableEnhancedHostSupport(true);

			this.Rpc.Event("hostingSession").OnRaw(new Action<Player>(OnHostingSession));
			this.Rpc.Event("HostedSession").OnRaw(new Action<Player>(OnHostedSession));

			this.Rpc.Event("playerConnecting").OnRaw(new Action<Player, string, CallbackDelegate, ExpandoObject>(Connecting));
			this.Rpc.Event("playerDropped").OnRaw(new Action<Player, string, CallbackDelegate>(Dropped));
			this.Rpc.Event("clientInitialize").On<string>(Initialize);
			this.Rpc.Event("clientInitialized").On(Initialized);
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
			var deferals = new Deferals(callbacks, drop);

			await this.Events.RaiseAsync("clientConnecting", client, deferals); // TODO
		
			using (var context = new StorageContext())
			using (var transaction = context.Database.BeginTransaction())
			{
				context.Configuration.ProxyCreationEnabled = false;
				context.Configuration.LazyLoadingEnabled = false;

				try
				{
					var user = context.Users.SingleOrDefault(u => u.SteamId == client.SteamId);

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
					var session = new Session
					{
						Id = GuidGenerator.GenerateTimeBasedGuid(),
						User = user,
						IpAddress = client.EndPoint
					};

					context.Sessions.Add(session);

					// Save changes
					await context.SaveChangesAsync();
					transaction.Commit();

					this.sessions.Add(session);

					await this.Events.RaiseAsync("sessionCreated", user, session);

					await this.Events.RaiseAsync("clientConnected", client, session, deferals);

					this.Logger.Info($"[{session.Id}] Player \"{user.Name}\" connected from {session.IpAddress}");
				}
				catch (Exception ex)
				{
					transaction.Rollback();

					this.Logger.Error(ex);
				}
			}
		}

		public async void Dropped([FromSource] Player player, string disconnectMessage, CallbackDelegate drop)
		{
			var client = new Client(int.Parse(player.Handle));

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

				this.sessions.TryTake(out session);

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
	}
}
