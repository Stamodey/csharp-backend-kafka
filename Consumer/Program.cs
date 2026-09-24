using Consumer.Clients;
using Consumer.Config;
using Consumer.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(options =>
{
    options.ServicesStartConcurrently = true;
    options.ServicesStopConcurrently = true;
});

builder.Services.Configure<KafkaSettings>(
    builder.Configuration.GetSection(nameof(KafkaSettings)));

//builder.Services.AddHostedService<OmsOrderCreatedConsumer>();
//builder.Services.AddHostedService<OmsOrderStatusChangedConsumer>();

for (int i = 0; i < 5; i++)
{
    builder.Services.AddHostedService<BatchOmsOrderCreatedConsumer>();
    builder.Services.AddHostedService<BatchOmsOrderStatusChangedConsumer>();
}

var baseAddress = builder.Configuration["HttpClient:Oms:BaseAddress"];
if (string.IsNullOrEmpty(baseAddress))
{
    throw new Exception("HttpClient:Oms:BaseAddress is not configured!");
}
builder.Services.AddHttpClient<OmsClient>(c => c.BaseAddress = new Uri(baseAddress));

var app = builder.Build();

app.MapGet("/", () => "Consumer is running!");
app.MapGet("/health", () => new { status = "Healthy", timestamp = DateTime.UtcNow });

await app.RunAsync();