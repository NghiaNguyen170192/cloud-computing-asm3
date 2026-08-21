using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using NetCore.Donation.Api.OData;
using NetCore.Donation.Application.Outbox.DTOs;
using NetCore.Donation.Application.Outbox.QueryOutboxMessages;
using System.Net;

namespace NetCore.Donation.Api.Controllers;

[Route("~/api/v1/outbox-messages")]
public class OutboxMessageController(IMediator mediator) : AuthorizedBaseController
{
    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetOutboxMessages(
        [FromQuery] string? correlationId,
        [FromQuery] string? idempotencyKey,
        ODataQueryOptions<QueryOutboxMessageDto> options)
    {
        var response = await mediator.Send(new QueryOutboxMessages(correlationId, idempotencyKey));
        return ODataPageResult.Create(response, options);
    }
}
