using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Builder;

public static class BlazorUiHostingExtensions
{
    public static WebApplicationBuilder AddBlazorUiCompression(this WebApplicationBuilder builder)
    {
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            [
                "application/javascript",
                "application/json",
                "image/svg+xml",
            ]);
        });
        builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });
        builder.Services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        return builder;
    }

    public static WebApplication UseBlazorUiStaticFiles(this WebApplication app)
    {
        app.UseResponseCompression();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context =>
            {
                if (app.Environment.IsDevelopment())
                {
                    context.Context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
                    return;
                }

                var path = context.Context.Request.Path.Value ?? string.Empty;
                var maxAge = path.Contains("/_content/", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("/_framework/", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)
                    ? TimeSpan.FromDays(7)
                    : TimeSpan.FromDays(1);
                context.Context.Response.Headers[HeaderNames.CacheControl] =
                    $"public,max-age={(int)maxAge.TotalSeconds}";
            },
        });

        return app;
    }
}
