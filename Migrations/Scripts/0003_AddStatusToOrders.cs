using FluentMigrator;

namespace Migrations.Scripts
{
    [Migration(3)]
    public class AddStatusToOrders : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE orders 
                ADD COLUMN IF NOT EXISTS status TEXT NOT NULL DEFAULT 'created';
                
                CREATE INDEX IF NOT EXISTS idx_orders_status ON orders (status);
                CREATE INDEX IF NOT EXISTS idx_orders_customer_status ON orders (customer_id, status);
            ");
            
            Execute.Sql(@"
                DROP TYPE IF EXISTS v1_order;
                CREATE TYPE v1_order AS (
                    id bigint,
                    customer_id bigint,
                    delivery_address text,
                    total_price_cents bigint,
                    total_price_currency text,
                    status text,
                    created_at timestamp with time zone,
                    updated_at timestamp with time zone
                );
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE orders DROP COLUMN IF EXISTS status;
                DROP INDEX IF EXISTS idx_orders_status;
                DROP INDEX IF EXISTS idx_orders_customer_status;
                
                DROP TYPE IF EXISTS v1_order;
                CREATE TYPE v1_order AS (
                    id bigint,
                    customer_id bigint,
                    delivery_address text,
                    total_price_cents bigint,
                    total_price_currency text,
                    created_at timestamp with time zone,
                    updated_at timestamp with time zone
                );
            ");
        }
    }
}