using System;
using System.Collections.Generic;

namespace Messages
{
    public class OrderCreatedMessage : BaseMessage
    {
        public long Id { get; set; }
        public long CustomerId { get; set; }
        public string DeliveryAddress { get; set; }
        public long TotalPriceCents { get; set; }
        public string TotalPriceCurrency { get; set; }
        public OrderStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public OrderItemUnit[] OrderItems { get; set; }

    }
    
    public enum OrderStatus
    {
        Created,
        Processing,
        Completed,
        Cancelled
    }

    public class OrderItemUnit
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public long PricePerUnitCents { get; set; }
        public string PriceCurrency { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}