using System;

namespace Models.Dto.V1.Responses
{
    public class V1AuditLogOrderResponse
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public long OrderItemId { get; set; }
        public long CustomerId { get; set; }
        public string OrderStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}