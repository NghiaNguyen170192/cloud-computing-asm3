using NetCore.Donation.ServiceDefaults;
using NetCore.Donation.WebClient;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedConfiguration();

if (builder.Environment.IsDevelopment() &&
    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:6010", "https://localhost:6011");
}
else if (!builder.Environment.IsDevelopment())
{
    var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    if (string.IsNullOrWhiteSpace(urls) || urls.Contains("https://", StringComparison.OrdinalIgnoreCase))
    {
        builder.WebHost.UseUrls("http://127.0.0.1:5000");
    }
}

var apiBaseAddress = "http://localhost:6000/";
foreach (var candidate in new[]
         {
             Environment.GetEnvironmentVariable("ApiBaseAddress"),
             Environment.GetEnvironmentVariable("DonationApi__BaseUrl"),
             builder.Configuration["ApiBaseAddress"],
             builder.Configuration["DonationApi:BaseUrl"],
         })
{
    if (string.IsNullOrWhiteSpace(candidate) || candidate.Contains("#{", StringComparison.Ordinal))
    {
        continue;
    }

    apiBaseAddress = candidate.EndsWith('/') ? candidate : candidate + "/";
    break;
}

builder.Services.AddHttpClient<DonationApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseAddress);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();
