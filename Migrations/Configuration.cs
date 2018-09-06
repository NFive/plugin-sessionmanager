using JetBrains.Annotations;
using NFive.SDK.Server.Migrations;
using NFive.SessionManager.Storage;

namespace NFive.SessionManager.Migrations
{
	[UsedImplicitly]
	public sealed class Configuration : MigrationConfiguration<StorageContext> { }
}
