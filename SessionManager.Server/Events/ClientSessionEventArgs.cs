using NFive.SDK.Core.Models.Player;
using NFive.SessionManager.Server.Models;

namespace NFive.SessionManager.Server.Events
{
	public class ClientSessionEventArgs : ClientEventArgs
	{
		public Session Session { get; }

		public ClientSessionEventArgs(Client client, Session session) : base(client)
		{
			this.Session = session;
		}
	}
}
