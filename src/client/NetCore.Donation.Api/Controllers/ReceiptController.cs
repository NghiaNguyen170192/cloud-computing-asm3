using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Net.Http.Headers;
using NetCore.Donation.Api.OData;
using NetCore.Donation.Application.Receipt.Create;
using NetCore.Donation.Application.Receipt.Delete;
using NetCore.Donation.Application.Receipt.DTOs;
using NetCore.Donation.Application.Receipt.GetReceipt;
using NetCore.Donation.Application.Receipt.GetReceiptDocument;
using NetCore.Donation.Application.Receipt.QueryReceipts;
using NetCore.Donation.Application.Receipt.Update;
using System.Net;

namespace NetCore.Donation.Api.Controllers;

[Route("~/api/v1/receipts")]
public class ReceiptController(IMediator mediator) : AuthorizedBaseController
{
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> Create([FromBody] CreateReceiptCommand request)
    {
        var id = await mediator.Send(request);

        return CreatedAtAction(nameof(GetReceipt), new { id }, new { id });
    }

    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetReceipts(
        [FromQuery] Guid? contactId,
        ODataQueryOptions<QueryReceiptDto> options)
    {
        var response = await mediator.Send(new QueryReceipts(contactId));
        return ODataPageResult.Create(response, options);
    }

    [HttpGet("{id:guid}")]
    [Produces("application/json", "application/pdf")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
    public async Task<IActionResult> GetReceipt(Guid id)
    {
        var format = ResolveReceiptResponseFormat(Request.GetTypedHeaders().Accept);
        if (format == ReceiptResponseFormat.NotAcceptable)
        {
            return StatusCode(StatusCodes.Status406NotAcceptable);
        }

        if (format == ReceiptResponseFormat.Pdf)
        {
            var document = await mediator.Send(new GetReceiptDocumentQuery(id));
            if (document is null)
            {
                return NotFound();
            }

            return File(document.Content, document.ContentType, document.FileName);
        }

        var response = await mediator.Send(new GetReceiptQuery(id));
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
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateReceiptCommand request)
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
        var deleted = await mediator.Send(new DeleteReceiptCommand(id));

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    private static ReceiptResponseFormat ResolveReceiptResponseFormat(IList<MediaTypeHeaderValue>? accept)
    {
        if (accept is null || accept.Count == 0)
        {
            return ReceiptResponseFormat.Json;
        }

        var pdfQuality = accept
            .Where(header => header.MediaType.HasValue &&
                header.MediaType.Value.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            .Select(header => header.Quality ?? 1.0)
            .DefaultIfEmpty(0)
            .Max();

        var jsonQuality = accept
            .Where(header => header.MediaType.HasValue &&
                (header.MediaType.Value.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
                 header.MediaType.Value.Equals("*/*", StringComparison.OrdinalIgnoreCase) ||
                 header.MediaType.Value.Equals("application/*", StringComparison.OrdinalIgnoreCase)))
            .Select(header => header.Quality ?? 1.0)
            .DefaultIfEmpty(0)
            .Max();

        if (pdfQuality <= 0 && jsonQuality <= 0)
        {
            return ReceiptResponseFormat.NotAcceptable;
        }

        if (pdfQuality > jsonQuality)
        {
            return ReceiptResponseFormat.Pdf;
        }

        return ReceiptResponseFormat.Json;
    }

    private enum ReceiptResponseFormat
    {
        Json,
        Pdf,
        NotAcceptable,
    }
}
