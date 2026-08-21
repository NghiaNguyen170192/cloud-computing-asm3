using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using NetCore.Donation.Api.OData;
using NetCore.Donation.Application.Country.Create;
using NetCore.Donation.Application.Country.DTOs;
using NetCore.Donation.Application.Country.QueryCountries;
using System.Net;

namespace NetCore.Donation.Api.Controllers;

[Route("~/api/v1/countries")]
public class CountryController(IMediator mediator) : AuthorizedBaseController
{
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    public async Task<ActionResult> Create([FromBody] CreateCountriesCommand request)
    {
        var ids = await mediator.Send(request);
        return Ok(ids);
    }

    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetCountries(ODataQueryOptions<QueryCountryDto> options)
    {
        var response = await mediator.Send(new QueryCountries());
        return ODataPageResult.Create(response, options);
    }
}