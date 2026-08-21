using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using NetCore.Donation.Api.OData;
using NetCore.Donation.Application.Transaction.Create;
using NetCore.Donation.Application.Transaction.Delete;
using NetCore.Donation.Application.Transaction.DTOs;
using NetCore.Donation.Application.Transaction.GetTransaction;
using NetCore.Donation.Application.Transaction.QueryTransactions;
using NetCore.Donation.Application.Transaction.Update;
using System.Net;

namespace NetCore.Donation.Api.Controllers;

[Route("~/api/v1/transactions")]
public class TransactionController(IMediator mediator) : AuthorizedBaseController
{
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> Create([FromBody] CreateTransactionCommand request)
    {
        var id = await mediator.Send(request);

        return CreatedAtAction(nameof(GetTransaction), new { id }, new { id });
    }

    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] Guid? contactId,
        [FromQuery] Guid? paymentScheduleId,
        ODataQueryOptions<QueryTransactionDto> options)
    {
        var response = await mediator.Send(new QueryTransactions(contactId, paymentScheduleId));
        return ODataPageResult.Create(response, options);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<QueryTransactionDto>> GetTransaction(Guid id)
    {
        var response = await mediator.Send(new GetTransactionQuery(id));

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateTransactionCommand request)
    {
        if (id != request.Id)
        {
            return BadRequest("The identifier in the route does not match the identifier in the payload.");
        }

        var updated = await mediator.Send(request);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await mediator.Send(new DeleteTransactionCommand(id));

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}