using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using NetCore.Donation.Api.OData;
using NetCore.Donation.Application.PaymentSchedule.Create;
using NetCore.Donation.Application.PaymentSchedule.Delete;
using NetCore.Donation.Application.PaymentSchedule.DTOs;
using NetCore.Donation.Application.PaymentSchedule.GetPaymentSchedule;
using NetCore.Donation.Application.PaymentSchedule.QueryPaymentSchedules;
using NetCore.Donation.Application.PaymentSchedule.Update;
using System.Net;

namespace NetCore.Donation.Api.Controllers;

[Route("~/api/v1/payment-schedules")]
public class PaymentScheduleController(IMediator mediator) : AuthorizedBaseController
{
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> Create([FromBody] CreatePaymentScheduleCommand request)
    {
        var id = await mediator.Send(request);

        return CreatedAtAction(nameof(GetPaymentSchedule), new { id }, new { id });
    }

    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetPaymentSchedules(
        [FromQuery] Guid? contactId,
        ODataQueryOptions<QueryPaymentScheduleDto> options)
    {
        var response = await mediator.Send(new QueryPaymentSchedules(contactId));
        return ODataPageResult.Create(response, options);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<QueryPaymentScheduleDto>> GetPaymentSchedule(Guid id)
    {
        var response = await mediator.Send(new GetPaymentScheduleQuery(id));

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
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdatePaymentScheduleCommand request)
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
        var deleted = await mediator.Send(new DeletePaymentScheduleCommand(id));

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}