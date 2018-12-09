namespace NFive.SessionManager.Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Sessions",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        IpAddress = c.String(nullable: false, maxLength: 15, unicode: false),
                        Created = c.DateTime(nullable: false, precision: 0),
                        Connected = c.DateTime(precision: 0),
                        Disconnected = c.DateTime(precision: 0),
                        DisconnectReason = c.String(maxLength: 200, unicode: false),
                        UserId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Sessions", "UserId", "dbo.Users");
            DropIndex("dbo.Sessions", new[] { "UserId" });
            DropTable("dbo.Sessions");
        }
    }
}
