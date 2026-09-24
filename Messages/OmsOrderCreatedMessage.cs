using System;
using System.Collections.Generic;

namespace Messages
{
    public class OmsOrderCreatedMessage : BaseMessage
    {
        public long Id { get; set; }
        public long CustomerId { get; set; }
        public string DeliveryAddress { get; set; }
        public long TotalPriceCents { get; set; }
        public string TotalPriceCurrency { get; set; }
        public OrderStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public List<OrderItemDto> OrderItems { get; set; }
        
    }

    public class OrderItemDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public string ProductTitle { get; set; }
        public string ProductUrl { get; set; }
        public long PriceCents { get; set; }
        public string PriceCurrency { get; set; }
    }
}