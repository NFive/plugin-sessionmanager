using System;
using CitizenFX.Core.Native;
using JetBrains.Annotations;
using NFive.SDK.Core.Controllers;

namespace NFive.SessionManager.Server
{
	[PublicAPI]
	public class Configuration : ControllerConfiguration
	{
		private readonly Lazy<ushort> maxClients = new Lazy<ushort>(() => (ushort)API.GetConvarInt("sv_maxclients", 32));

		public uint ConnectionTimeout { get; set; } = 60000;
		public uint ReconnectGrace { get; set; } = 120000;
		public ushort MaxClients => this.maxClients.Value;
	}
}
