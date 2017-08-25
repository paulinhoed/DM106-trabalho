namespace PauloSouzaTrabalhoFinalDM106.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Orders",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        email = c.String(),
                        orderDate = c.DateTime(nullable: false),
                        deliveryDate = c.DateTime(nullable: false),
                        status = c.String(),
                        orderTotalPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                        orderTotalWeight = c.Decimal(nullable: false, precision: 18, scale: 2),
                        shippingPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.OrderItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ProductId = c.Int(nullable: false),
                        OrderId = c.Int(nullable: false),
                        quantity = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .ForeignKey("dbo.Orders", t => t.OrderId, cascadeDelete: true)
                .Index(t => t.ProductId)
                .Index(t => t.OrderId);
            
            CreateTable(
                "dbo.Products",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        name = c.String(nullable: false),
                        description = c.String(),
                        color = c.String(),
                        model = c.String(nullable: false),
                        code = c.String(nullable: false),
                        price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        weight = c.Decimal(nullable: false, precision: 18, scale: 2),
                        height = c.Decimal(nullable: false, precision: 18, scale: 2),
                        width = c.Decimal(nullable: false, precision: 18, scale: 2),
                        length = c.Decimal(nullable: false, precision: 18, scale: 2),
                        diameter = c.Decimal(nullable: false, precision: 18, scale: 2),
                        imageUrl = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.OrderItems", "OrderId", "dbo.Orders");
            DropForeignKey("dbo.OrderItems", "ProductId", "dbo.Products");
            DropIndex("dbo.OrderItems", new[] { "OrderId" });
            DropIndex("dbo.OrderItems", new[] { "ProductId" });
            DropTable("dbo.Products");
            DropTable("dbo.OrderItems");
            DropTable("dbo.Orders");
        }
    }
}
