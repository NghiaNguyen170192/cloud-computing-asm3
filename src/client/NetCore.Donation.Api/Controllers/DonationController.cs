using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetCore.Donation.Application.Donation.UserMakesDonation;
using System.Net;

namespace NetCore.Donation.Api.Controllers;

[Route("~/api/v1/donations")]
public class DonationController(IMediator mediator) : AuthorizedBaseController
{
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> Create([FromBody] UserMakesDonationCommand request)
    {
        var result = await mediator.Send(request);

        if (result.PaymentScheduleId is { } scheduleId)
        {
            return CreatedAtAction(
                nameof(PaymentScheduleController.GetPaymentSchedule),
                "PaymentSchedule",
                new { id = scheduleId },
                result);
        }

        return CreatedAtAction(
            nameof(TransactionController.GetTransaction),
            "Transaction",
            new { id = result.TransactionId },
            result);
    }
}
