using Microsoft.AspNetCore.Mvc;
using OpenBusiness.Crm.Application.Contacts;
using OpenBusiness.Crm.Api.Services.Contacts;

namespace OpenBusiness.Crm.Api.Controllers;

[ApiController]
[Route("api/customers/{customerId:guid}/contacts")]
public sealed class ContactsController : ControllerBase
{
    private readonly IContactService _contactService;

    public ContactsController(IContactService contactService)
    {
        _contactService = contactService;
    }

    [HttpPost]
    public async Task<ActionResult<ContactResponse>> Create(Guid customerId, [FromBody] CreateContactRequest request, CancellationToken ct)
    {
        if (request.CustomerId != customerId)
            return BadRequest("CustomerId in route must match body");

        var contact = await _contactService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { customerId, id = contact.Id }, contact);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContactResponse>> Get(Guid customerId, Guid id, CancellationToken ct)
    {
        var contact = await _contactService.GetAsync(id, ct);
        return contact is null ? NotFound() : Ok(contact);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContactResponse>>> List(
        Guid customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var contacts = await _contactService.ListAsync(customerId, page, pageSize, ct);
        return Ok(contacts);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ContactResponse>> Update(Guid customerId, Guid id, [FromBody] UpdateContactRequest request, CancellationToken ct)
    {
        var contact = await _contactService.UpdateAsync(id, request, ct);
        return contact is null ? NotFound() : Ok(contact);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid customerId, Guid id, CancellationToken ct)
    {
        var deleted = await _contactService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}