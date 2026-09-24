namespace WebApi.DAL.Models
{
    public class QueryAuditLogOrderDalModel
    {
        public long OrderId { get; set; }
        public long OrderItemId { get; set; }
        public long CustomerId { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
    }
}