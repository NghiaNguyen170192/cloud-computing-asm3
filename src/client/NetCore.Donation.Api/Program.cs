using NetCore.Donation.Application.Contact.DTOs;
using NetCore.Donation.Application.Country.DTOs;
using NetCore.Donation.Application.Donation.DTOs;
using NetCore.Donation.Application.Extensions;
using NetCore.Donation.Application.Journal.DTOs;
using NetCore.Donation.Application.Outbox.DTOs;
using NetCore.Donation.Application.PaymentMethod.DTOs;
using NetCore.Donation.Application.PaymentSchedule.DTOs;
using NetCore.Donation.Application.Receipt.DTOs;
using NetCore.Donation.Application.Transaction.DTOs;
using NetCore.Donation.Api;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Infrastructure.Database;
using NetCore.Donation.Infrastructure.Database.Extensions;
using NetCore.Donation.Infrastructure.Database.Services;
using NetCore.Donation.Infrastructure.Database.Middleware;
using NetCore.Donation.Infrastructure.Storage;
using NetCore.Donation.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

var onLambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));

// Local Kestrel only. On Lambda, the Hosting package owns the server.
if (!onLambda && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:6000", "https://localhost:6001");
}

if (onLambda)
{
    builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
}

builder.AddServiceDefaults();
builder.AddDefaultOpenApi(model =>
{
    model.EntitySet<QueryCountryDto>("Countries");
    model.EntitySet<QueryContactDto>("Contacts");
    model.EntitySet<QueryPaymentMethodDto>("PaymentMethods");
    model.EntitySet<QueryPaymentScheduleDto>("PaymentSchedules");
    model.EntitySet<QueryTransactionDto>("Transactions");
    model.EntitySet<QueryJournalDto>("Journals");
    model.EntitySet<QueryReceiptDto>("Receipts");
    model.EntitySet<QueryOutboxMessageDto>("OutboxMessages");
    model.EntitySet<QueryDonationFlowDto>("DonationFlows");
});

// Prefer Aspire-injected connection strings over local appsettings defaults.
var applicationConnectionString = builder.Configuration.GetConnectionString("netcore-donation-db");
if (!string.IsNullOrWhiteSpace(applicationConnectionString))
{
    builder.Configuration["Database:ApplicationConnectionString"] = applicationConnectionString;
}

var redisConnectionString = builder.Configuration.GetConnectionString("redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Configuration["Database:RedisConnectionString"] = redisConnectionString;
}

// ServiceDefaults embedded Production placeholders ("#{...}") are loaded after env vars and
// would otherwise win. Re-apply real AWS env values last.
foreach (var (envKey, configKey) in new (string, string)[]
         {
             ("Database__ApplicationConnectionString", "Database:ApplicationConnectionString"),
             ("Database__Provider", "Database:Provider"),
             ("Database__MigrationsAssembly", "Database:MigrationsAssembly"),
             ("Database__RedisConnectionString", "Database:RedisConnectionString"),
             ("ObjectStorage__BucketName", "ObjectStorage:BucketName"),
             ("ObjectStorage__Region", "ObjectStorage:Region"),
             ("ObjectStorage__ForcePathStyle", "ObjectStorage:ForcePathStyle"),
             ("ObjectStorage__CreateBucketIfNotExists", "ObjectStorage:CreateBucketIfNotExists"),
         })
{
    var value = Environment.GetEnvironmentVariable(envKey);
    if (!string.IsNullOrWhiteSpace(value) && !value.Contains("#{", StringComparison.Ordinal))
    {
        builder.Configuration[configKey] = value;
    }
}

var rdsConnectionString = RdsConnection.TryFromEnvironment();
if (!string.IsNullOrWhiteSpace(rdsConnectionString) &&
    RdsConnection.IsMissing(builder.Configuration["Database:ApplicationConnectionString"]))
{
    builder.Configuration["Database:ApplicationConnectionString"] = rdsConnectionString;
}

// Drop MinIO-only settings on Lambda so the SDK uses IAM + regional S3.
if (onLambda)
{
    builder.Configuration["ObjectStorage:ServiceUrl"] = string.Empty;
    builder.Configuration["ObjectStorage:AccessKey"] = string.Empty;
    builder.Configuration["ObjectStorage:SecretKey"] = string.Empty;
}

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Add correlation ID support
builder.Services.AddScoped<ICorrelationIdAccessor>(sp =>
    new CorrelationIdAccessor(sp.GetRequiredService<IHttpContextAccessor>()));

// Background outbox polling belongs on aws-asm3-outbox-worker, not the request Lambda.
if (!onLambda &&
    !string.Equals(Environment.GetEnvironmentVariable("DISABLE_OUTBOX_PROCESSOR"), "true", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHostedService<OutboxProcessor>();
}

// Dependency Injections
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddObjectStorage(builder.Configuration);

builder.Host.AddLogger("netcore-api");

var app = builder.Build();

app.UseExceptionHandler();
app.UseDefaultOpenApi();

// Use correlation ID middleware early in pipeline
app.UseCorrelationId();

app.MapDefaultEndpoints();

await app.RunAsync();
