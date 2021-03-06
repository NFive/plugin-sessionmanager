﻿using CitizenFX.Core;
using CitizenFX.Core.Native;
using NFive.SDK.Server.Rpc;
using System;
using System.Globalization;

namespace NFive.SessionManager.Server.Models
{
	public class Client : IClient
	{
		public int Handle { get; }

		public string Name { get; }

		public string License { get; }

		public long? SteamId { get; }

		public string EndPoint { get; }

		public int Ping
		{
			get
			{
				if (this.Handle > ushort.MaxValue) return -1;
				return API.GetPlayerPing(this.Handle.ToString());
			}
		}

		public Client(int handle)
		{
			this.Handle = handle;

			var player = new PlayerList()[this.Handle];

			this.Name = player.Name;
			this.License = player.Identifiers["license"];
			this.SteamId = !string.IsNullOrEmpty(player.Identifiers["steam"]) ? long.Parse(player.Identifiers["steam"], NumberStyles.HexNumber) : default(long?);
			this.EndPoint = player.EndPoint;
		}

		public IRpcTrigger Event(string @event) { throw new NotImplementedException(); } // TODO
	}
}
