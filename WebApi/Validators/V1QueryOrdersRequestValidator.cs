using FluentValidation;
using Models.Dto.V1.Requests;

namespace WebApi.Validators
{
    public class V1QueryOrdersRequestValidator : AbstractValidator<V1QueryOrdersRequest>
    {
        public V1QueryOrdersRequestValidator()
        {
            RuleFor(x => x.Ids)
                .NotNull()
                .When(x => (x.CustomerIds == null || x.CustomerIds.Length == 0))
                .WithMessage("Either Ids or CustomerIds must be provided.");

            RuleFor(x => x.Page).GreaterThanOrEqualTo(0);
            RuleFor(x => x.PageSize).GreaterThanOrEqualTo(0);
        }
    }
}
