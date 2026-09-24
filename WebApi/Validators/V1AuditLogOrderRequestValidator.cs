using FluentValidation;
using Models.Dto.V1.Requests;

namespace WebApi.Validators
{
    public class V1AuditLogOrderRequestValidator : AbstractValidator<V1AuditLogOrderRequest>
    {
        public V1AuditLogOrderRequestValidator()
        {
            RuleFor(x => x.Orders)
                .NotEmpty().WithMessage("Orders array cannot be empty")
                .Must(orders => orders.Length <= 1000).WithMessage("Cannot process more than 1000 orders at once");

            RuleForEach(x => x.Orders).ChildRules(order =>
            {
                order.RuleFor(x => x.OrderId)
                    .GreaterThan(0).WithMessage("OrderId must be greater than 0");

                order.RuleFor(x => x.OrderItemId)
                    .GreaterThan(0).WithMessage("OrderItemId must be greater than 0");

                order.RuleFor(x => x.CustomerId)
                    .GreaterThan(0).WithMessage("CustomerId must be greater than 0");

                order.RuleFor(x => x.OrderStatus)
                    .NotEmpty().WithMessage("OrderStatus is required")
                    .MaximumLength(50).WithMessage("OrderStatus cannot exceed 50 characters")
                    .Must(BeValidStatus).WithMessage("Invalid order status");
            });
        }

        private bool BeValidStatus(string status)
        {
            var validStatuses = new[] { "created", "processing", "completed", "cancelled", "refunded", "pending", "shipped", "delivered" };
            return validStatuses.Contains(status?.ToLower());
        }
    }
}