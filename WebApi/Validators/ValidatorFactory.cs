using FluentValidation;

namespace WebApi.Validators
{
    public class ValidatorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidatorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IValidator<T> GetValidator<T>()
        {
            // Используем DI для получения валидаторов
            return _serviceProvider.GetRequiredService<IValidator<T>>();
        }
    }
}