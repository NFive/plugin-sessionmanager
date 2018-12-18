using NFive.SDK.Core.Models.Player;
using NFive.SessionManager.Server.Models;

namespace NFive.SessionManager.Server.Events
{
	public class ClientSessionDeferralsEventArgs : ClientSessionEventArgs
	{
		public Deferrals Deferrals { get; }

		public ClientSessionDeferralsEventArgs(Client client, Session session, Deferrals deferrals) : base(client, session)
		{
			this.Deferrals = deferrals;
		}
	}
}
