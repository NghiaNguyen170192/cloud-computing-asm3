using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;

namespace NetCore.Donation.ServiceDefaults;

public static partial class Extensions
{
    public static IHostApplicationBuilder AddDefaultOpenApi(
        this IHostApplicationBuilder builder,
        Action<ODataConventionModelBuilder>? configureODataModel = null)
    {
        var modelBuilder = new ODataConventionModelBuilder();
        configureODataModel?.Invoke(modelBuilder);

        // Use default OpenAPI/Swagger configuration. No JWT security defined by default.
        builder.Services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen()
            .AddRouting(options => options.LowercaseUrls = true)
            .AddControllers()
            .AddOData(options =>
            {
                options.Filter().Expand()
                    .Select().OrderBy().Count().SetMaxTop(1000).SkipToken()
                    .AddRouteComponents("odata", modelBuilder.GetEdmModel());
                options.EnableNoDollarQueryOptions = true;
            });

        return builder;
    }

    public static IApplicationBuilder UseDefaultOpenApi(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        return app;
    }
}