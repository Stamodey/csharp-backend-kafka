using System;

namespace Messages
{
    public class OmsOrderStatusChangedMessage : BaseMessage
    {
        public long OrderId { get; set; }
        public OrderStatus Status { get; set; }

        public long CustomerId { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public DateTimeOffset ChangedAt { get; set; }
        
    }
}