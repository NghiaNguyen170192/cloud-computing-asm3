using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using NetCore.Donation.Api.OData;
using NetCore.Donation.Application.Donation.DTOs;
using NetCore.Donation.Application.Donation.QueryDonationFlows;
using System.Net;

namespace NetCore.Donation.Api.Controllers;

[Route("~/api/v1/donation-flows")]
public class DonationFlowController(IMediator mediator) : AuthorizedBaseController
{
    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetDonationFlows(ODataQueryOptions<QueryDonationFlowDto> options)
    {
        var response = await mediator.Send(new QueryDonationFlows());
        return ODataPageResult.Create(response.AsQueryable(), options);
    }
}
