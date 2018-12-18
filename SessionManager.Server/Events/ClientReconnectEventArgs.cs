using NFive.SDK.Core.Models.Player;
using NFive.SessionManager.Server.Models;

namespace NFive.SessionManager.Server.Events
{
	public class ClientReconnectEventArgs : ClientEventArgs
	{
		public Session OldSession { get; }

		public Session NewSession { get; }

		public ClientReconnectEventArgs(Client client, Session oldSession, Session newSession) : base(client)
		{
			this.OldSession = oldSession;
			this.NewSession = newSession;
		}
	}
}
