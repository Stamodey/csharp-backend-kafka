using System.ComponentModel.DataAnnotations;

namespace Models.Dto.V1.Requests
{
    public class V1AuditLogOrderRequest
    {
        [Required]
        public LogOrder[] Orders { get; set; }

        public class LogOrder
        {
            [Required]
            public long OrderId { get; set; }

            [Required]
            public long OrderItemId { get; set; }

            [Required]
            public long CustomerId { get; set; }

            [Required]
            [StringLength(50)]
            public string OrderStatus { get; set; }
        }
    }
}