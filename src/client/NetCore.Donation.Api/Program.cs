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
using NetCore.Donation.Infrastructure.Database.Extensions;
using NetCore.Donation.Infrastructure.Database.Services;
using NetCore.Donation.Infrastructure.Database.Middleware;
using NetCore.Donation.Infrastructure.Storage;
using NetCore.Donation.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Override URLs if not set by Aspire
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:6000", "https://localhost:6001");
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

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Add correlation ID support
builder.Services.AddScoped<ICorrelationIdAccessor>(sp =>
    new CorrelationIdAccessor(sp.GetRequiredService<IHttpContextAccessor>()));
builder.Services.AddScoped<IIdempotencyKeyAccessor>(sp =>
    new IdempotencyKeyAccessor(sp.GetRequiredService<IHttpContextAccessor>()));
builder.Services.AddHostedService<OutboxProcessor>();

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
