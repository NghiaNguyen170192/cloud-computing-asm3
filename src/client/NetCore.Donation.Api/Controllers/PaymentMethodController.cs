using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using NetCore.Donation.Api.OData;
using NetCore.Donation.Application.PaymentMethod.Create;
using NetCore.Donation.Application.PaymentMethod.Delete;
using NetCore.Donation.Application.PaymentMethod.DTOs;
using NetCore.Donation.Application.PaymentMethod.GetPaymentMethod;
using NetCore.Donation.Application.PaymentMethod.QueryPaymentMethods;
using NetCore.Donation.Application.PaymentMethod.Update;
using System.Net;

namespace NetCore.Donation.Api.Controllers;

[Route("~/api/v1/payment-methods")]
public class PaymentMethodController(IMediator mediator) : AuthorizedBaseController
{
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> Create([FromBody] CreatePaymentMethodCommand request)
    {
        var id = await mediator.Send(request);

        return CreatedAtAction(nameof(GetPaymentMethod), new { id }, new { id });
    }

    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetPaymentMethods(
        [FromQuery] Guid? contactId,
        ODataQueryOptions<QueryPaymentMethodDto> options)
    {
        var response = await mediator.Send(new QueryPaymentMethods(contactId));
        return ODataPageResult.Create(response, options);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<QueryPaymentMethodDto>> GetPaymentMethod(Guid id)
    {
        var response = await mediator.Send(new GetPaymentMethodQuery(id));

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
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdatePaymentMethodCommand request)
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
        var deleted = await mediator.Send(new DeletePaymentMethodCommand(id));

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}