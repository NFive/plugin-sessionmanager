using System;
using CitizenFX.Core.Native;
using JetBrains.Annotations;
using NFive.SDK.Core.Controllers;

namespace NFive.SessionManager
{
	[PublicAPI]
	public class Configuration : IControllerConfiguration
	{
		private Lazy<int> maxClients = new Lazy<int>(() => API.GetConvarInt("sv_maxclients", 32));

		public uint ConnectionTimeout { get; set; } = 60000;
		public uint ReconnectGrace { get; set; } = 120000;
		public int MaxClients => this.maxClients.Value;
	}
}
