using Homework2.Application.Abstractions;
using Homework2.Application.Exceptions;
using Homework2.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Homework2.Api.Controllers;

[ApiController]
[Route("api/contacts")]
[Produces("application/json")]
public sealed class ContactsController : ControllerBase
{
    private readonly IContactService _contactService;

    public ContactsController(IContactService contactService)
    {
        _contactService = contactService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ContactDto>>> GetContacts(
        [FromQuery] ContactQuery query,
        CancellationToken ct)
    {
        return Ok(await _contactService.GetPagedAsync(query, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContactDto>> GetContact(Guid id, CancellationToken ct)
    {
        return Ok(await _contactService.GetByIdAsync(id, ct));
    }

    [HttpPost]
    public async Task<ActionResult<ContactDto>> CreateContact(
        [FromBody] CreateContactRequest request,
        CancellationToken ct)
    {
        var contact = await _contactService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetContact), new { id = contact.Id }, contact);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ContactDto>> UpdateContact(
        Guid id,
        [FromBody] UpdateContactRequest request,
        CancellationToken ct)
    {
        return Ok(await _contactService.UpdateAsync(id, request, ct));
    }

    [HttpPatch("{id:guid}/favorite")]
    public async Task<ActionResult<ContactDto>> SetFavorite(
        Guid id,
        [FromBody] SetFavoriteRequest request,
        CancellationToken ct)
    {
        return Ok(await _contactService.SetFavoriteAsync(id, request.IsFavorite, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteContact(Guid id, CancellationToken ct)
    {
        await _contactService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/methods")]
    public async Task<ActionResult<ContactDto>> AddMethod(
        Guid id,
        [FromBody] ContactMethodInput input,
        CancellationToken ct)
    {
        return Ok(await _contactService.AddMethodAsync(id, input, ct));
    }

    [HttpPut("{id:guid}/methods/{methodId:guid}")]
    public async Task<ActionResult<ContactDto>> UpdateMethod(
        Guid id,
        Guid methodId,
        [FromBody] ContactMethodInput input,
        CancellationToken ct)
    {
        return Ok(await _contactService.UpdateMethodAsync(id, methodId, input, ct));
    }

    [HttpDelete("{id:guid}/methods/{methodId:guid}")]
    public async Task<IActionResult> RemoveMethod(Guid id, Guid methodId, CancellationToken ct)
    {
        await _contactService.RemoveMethodAsync(id, methodId, ct);
        return NoContent();
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] ContactQuery query, CancellationToken ct)
    {
        var content = await _contactService.ExportAsync(query, ct);
        var fileName = $"contacts-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx";
        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ImportResult>> Import(
        IFormFile file,
        [FromQuery] bool skipDuplicates = true,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            throw new AppValidationException("An Excel file is required.");
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppValidationException("Only .xlsx files are supported.");
        }

        await using var stream = file.OpenReadStream();
        return Ok(await _contactService.ImportAsync(stream, skipDuplicates, ct));
    }
}