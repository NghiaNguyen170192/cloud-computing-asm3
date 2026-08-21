using NetCore.Donation.ServiceDefaults;
using NetCore.Donation.WebClient;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedConfiguration();

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:6010", "https://localhost:6011");
}

var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? "http://localhost:6000";
builder.Services.AddHttpClient<DonationApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseAddress.EndsWith('/') ? apiBaseAddress : apiBaseAddress + "/");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();
