using FluentMigrator;

namespace Migrations.Scripts
{
    [Migration(2)]
    public class CreateAuditLogOrderTable : Migration
    {
        public override void Up()
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS audit_log_order (
                    id BIGSERIAL NOT NULL PRIMARY KEY,
                    order_id BIGINT NOT NULL,
                    order_item_id BIGINT NOT NULL,
                    customer_id BIGINT NOT NULL,
                    order_status TEXT NOT NULL,
                    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                );

                CREATE INDEX IF NOT EXISTS idx_audit_log_order_id ON audit_log_order (order_id);
                CREATE INDEX IF NOT EXISTS idx_audit_log_order_item_id ON audit_log_order (order_item_id);
                CREATE INDEX IF NOT EXISTS idx_audit_log_customer_id ON audit_log_order (customer_id);
                CREATE INDEX IF NOT EXISTS idx_audit_log_order_status ON audit_log_order (order_status);
                CREATE INDEX IF NOT EXISTS idx_audit_log_created_at ON audit_log_order (created_at);
                CREATE INDEX IF NOT EXISTS idx_audit_log_customer_created ON audit_log_order (customer_id, created_at);
                CREATE INDEX IF NOT EXISTS idx_audit_log_order_created ON audit_log_order (order_id, created_at);
            ";

            Execute.Sql(sql);
        }

        public override void Down()
        {
            //оставляем пустым для безопасности данных
        }
    }
}