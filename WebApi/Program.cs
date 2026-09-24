using Dapper;
using FluentValidation;
using Models.Dto.V1.Requests;
using System.Text.Json;
using WebApi.BLL.Services;
using WebApi.Config;
using WebApi.DAL;
using WebApi.DAL.Interfaces;
using WebApi.DAL.Repositories;
using WebApi.Validators;
using WebApi.Jobs;

var builder = WebApplication.CreateBuilder(args);

DefaultTypeMap.MatchNamesWithUnderscores = true;
builder.Services.AddScoped<UnitOfWork>();

// Конфигурация базы данных
builder.Services.Configure<DbSettings>(builder.Configuration.GetSection(nameof(DbSettings)));

builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection(nameof(KafkaSettings)));

// Регистрация валидаторов
builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));
builder.Services.AddScoped<ValidatorFactory>();

// Явная регистрация валидаторов для обеспечения DI
builder.Services.AddScoped<IValidator<V1AuditLogOrderRequest>, V1AuditLogOrderRequestValidator>();
builder.Services.AddScoped<IValidator<V1UpdateOrdersStatusRequest>, V1UpdateOrdersStatusRequestValidator>(); // Добавляем новый валидатор

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

// Регистрация репозиториев
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<IAuditLogOrderRepository, AuditLogOrderRepository>(); 

builder.Services.AddScoped<OrderService>();
builder.Services.AddSingleton<KafkaProducer>();
builder.Services.AddScoped<IAuditLogOrderService, AuditLogOrderService>(); 

builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<OrderGenerator>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

// Запускаем миграции
Migrations.Program.Main(Array.Empty<string>());

app.Run();