using NFive.SessionManager.Server.Models;

namespace NFive.SessionManager.Server.Events
{
	public class ClientDeferralsEventArgs : ClientEventArgs
	{
		public Deferrals Deferrals { get; }

		public ClientDeferralsEventArgs(Client client, Deferrals deferrals) : base(client)
		{
			this.Deferrals = deferrals;
		}
	}
}
