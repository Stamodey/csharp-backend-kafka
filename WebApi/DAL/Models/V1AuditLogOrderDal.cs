using NpgsqlTypes;

namespace WebApi.DAL.Models
{
    public class V1AuditLogOrderDal
    {
        [PgName("id")]
        public long Id { get; set; }

        [PgName("order_id")]
        public long OrderId { get; set; }

        [PgName("order_item_id")]
        public long OrderItemId { get; set; }

        [PgName("customer_id")]
        public long CustomerId { get; set; }

        [PgName("order_status")]
        public string OrderStatus { get; set; }

        [PgName("created_at")]
        public DateTime CreatedAt { get; set; }

        [PgName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}