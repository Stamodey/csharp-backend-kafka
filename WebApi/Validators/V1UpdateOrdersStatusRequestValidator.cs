using FluentValidation;
using Models.Dto.V1.Requests;
using WebApi.BLL.Services;

namespace WebApi.Validators
{
    public class V1UpdateOrdersStatusRequestValidator : AbstractValidator<V1UpdateOrdersStatusRequest>
    {
        public V1UpdateOrdersStatusRequestValidator()
        {
            RuleFor(x => x.OrderIds)
                .NotNull().WithMessage("OrderIds cannot be null")
                .NotEmpty().WithMessage("OrderIds cannot be empty")
                .Must(ids => ids.Length > 0).WithMessage("OrderIds must contain at least one order id")
                .Must(ids => ids.All(id => id > 0)).WithMessage("All order ids must be greater than 0")
                .Must(ids => ids.Length <= 1000).WithMessage("Cannot update more than 1000 orders at once");
            
            RuleFor(x => x.NewStatus)
                .NotNull().WithMessage("NewStatus cannot be null")
                .NotEmpty().WithMessage("NewStatus cannot be empty")
                .Must(OrderStatusService.IsValidStatus)
                .WithMessage("Invalid status. Allowed statuses: " + 
                             string.Join(", ", OrderStatusService.GetAllStatuses()));
        }
    }
}