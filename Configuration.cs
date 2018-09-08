using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using JetBrains.Annotations;
using NFive.SDK.Core.Controllers;

namespace NFive.SessionManager
{
	[PublicAPI]
	public class Configuration : IControllerConfiguration
	{
		public uint ConnectionTimeout { get; set; } = 60000;
		public uint ReconnectGrace { get; set; } = 120000;
		public int MaxClients => this.maxClients.Value;

		private Lazy<int> maxClients = new Lazy<int>(() => API.GetConvarInt("sv_maxclients", 32));
	}
}

