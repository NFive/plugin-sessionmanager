using System;
using NFive.SessionManager.Server.Models;

namespace NFive.SessionManager.Server.Events
{
	public class ClientEventArgs : EventArgs
	{
		public Client Client { get; }

		public ClientEventArgs(Client client)
		{
			this.Client = client;
		}
	}
}
