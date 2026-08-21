using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using NetCore.Donation.Api.OData;
using NetCore.Donation.Application.Journal.Create;
using NetCore.Donation.Application.Journal.Delete;
using NetCore.Donation.Application.Journal.DTOs;
using NetCore.Donation.Application.Journal.GetJournal;
using NetCore.Donation.Application.Journal.QueryJournals;
using System.Net;

namespace NetCore.Donation.Api.Controllers;

[Route("~/api/v1/journals")]
public class JournalController(IMediator mediator) : AuthorizedBaseController
{
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> Create([FromBody] CreateJournalCommand request)
    {
        var id = await mediator.Send(request);

        return CreatedAtAction(nameof(GetJournal), new { id }, new { id });
    }

    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetJournals(ODataQueryOptions<QueryJournalDto> options)
    {
        var response = await mediator.Send(new QueryJournals());
        return ODataPageResult.Create(response, options);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<QueryJournalDto>> GetJournal(Guid id)
    {
        var response = await mediator.Send(new GetJournalQuery(id));

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await mediator.Send(new DeleteJournalCommand(id));

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
