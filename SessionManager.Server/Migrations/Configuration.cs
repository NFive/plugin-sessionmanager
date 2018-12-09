using JetBrains.Annotations;
using NFive.SDK.Server.Migrations;
using NFive.SessionManager.Server.Storage;

namespace NFive.SessionManager.Server.Migrations
{
	[UsedImplicitly]
	public sealed class Configuration : MigrationConfiguration<StorageContext> { }
}
