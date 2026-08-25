using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;

namespace NetCore.Donation.Api.OData;

public static class ODataPageResult
{
    public static IActionResult Create<T>(IQueryable<T> query, ODataQueryOptions<T> options)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate(new ODataValidationSettings
        {
            AllowedQueryOptions = AllowedQueryOptions.Filter
                | AllowedQueryOptions.OrderBy
                | AllowedQueryOptions.Skip
                | AllowedQueryOptions.Top
                | AllowedQueryOptions.Select
                | AllowedQueryOptions.Count
                | AllowedQueryOptions.SkipToken,
            MaxTop = 1000,
        });

        var settings = new ODataQuerySettings
        {
            EnsureStableOrdering = true,
            HandleNullPropagation = HandleNullPropagationOption.Default,
        };

        var filtered = options.Filter is null
            ? query
            : options.Filter.ApplyTo(query, settings) as IQueryable<T> ?? query;

        var count = filtered.Count();

        IQueryable results = options.OrderBy is null
            ? filtered
            : options.OrderBy.ApplyTo(filtered, settings);

        if (options.Skip is not null)
        {
            results = options.Skip.ApplyTo(results, settings);
        }

        if (options.Top is not null)
        {
            results = options.Top.ApplyTo(results, settings);
        }

        if (options.SelectExpand is not null)
        {
            results = options.SelectExpand.ApplyTo(results, settings);
        }

        var value = new List<object>();
        foreach (var item in results)
        {
            if (item is not null)
            {
                value.Add(item);
            }
        }

        return new OkObjectResult(new Dictionary<string, object?>
        {
            ["@odata.count"] = count,
            ["value"] = value,
        });
    }
}