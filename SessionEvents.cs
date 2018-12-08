namespace NFive.SessionManager
{
	public static class SessionEvents
	{
		public const string DisconnectPlayer = "nfive:sessionmanager:disconnectPlayer";
		public const string ClientConnecting = "nfive:sessionmanager:clientConnecting";
		public const string UserCreating = "nfive:sessionmanager:userCreating";
		public const string UserCreated = "nfive:sessionmanager:userCreated";
		public const string SessionCreating = "nfive:sessionmanager:sessionCreating";
		public const string SessionCreated = "nfive:sessionmanager:sessionCreated";
		public const string ClientConnected = "nfive:sessionmanager:clientConnected";
		public const string ClientReconnecting = "nfive:sessionmanager:clientReconnecting";
		public const string ClientReconnected = "nfive:sessionmanager:clientReconnected";
		public const string ClientDisconnecting = "nfive:sessionmanager:clientDisconnecting";
		public const string ClientDisconnected = "nfive:sessionmanager:clientDisconnected";
		public const string ClientInitializing = "nfive:sessionmanager:clientInitializing";
		public const string ClientInitialized = "nfive:sessionmanager:clientInitialized";

		public const string GetMaxPlayers = "nfive:sessionmanager:getMaxPlayers";
		public const string GetCurrentSessionsCount = "nfive:sessionmanager:getCurrentSessionsCount";
		public const string GetCurrentSessions = "nfive:sessionmanager:getCurrentSessions";

	}
}
