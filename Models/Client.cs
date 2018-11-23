using System.Globalization;
using System.Linq;
using CitizenFX.Core;
using NFive.SDK.Server.Rpc;

namespace NFive.SessionManager.Models
{
	public class Client : IClient
	{
		public int Handle { get; }

		public string Name { get; }

		public string License { get; }

		public long? SteamId { get; }

		public string EndPoint { get; }

		public int Ping { get; }

		public Client(int handle)
		{
			this.Handle = handle;

			var player = new PlayerList()[this.Handle];

			this.Name = player.Name;
			this.License = player.Identifiers["license"];
			this.SteamId = !string.IsNullOrEmpty(player.Identifiers["steam"]) ? long.Parse(player.Identifiers["steam"], NumberStyles.HexNumber) : default(long?);
			this.EndPoint = player.EndPoint;
			this.Ping = player.Ping;
		}

		public IRpcTrigger Event(string @event) { throw new System.NotImplementedException(); }
	}
}
