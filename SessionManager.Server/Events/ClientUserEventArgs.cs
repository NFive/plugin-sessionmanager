using NFive.SDK.Core.Models.Player;
using NFive.SessionManager.Server.Models;

namespace NFive.SessionManager.Server.Events
{
	public class ClientUserEventArgs : ClientEventArgs
	{
		public User User { get; }

		public ClientUserEventArgs(Client client, User user) : base(client)
		{
			this.User = user;
		}
	}
}
