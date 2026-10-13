using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenBusiness.Crm.Application.Customers;
using OpenBusiness.Crm.Api.Services.Customers;

namespace OpenBusiness.Crm.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Policy = "MemberOrAbove")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var customer = await _customerService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, customer);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> Get(Guid id, CancellationToken ct)
    {
        var customer = await _customerService.GetAsync(id, ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var customers = await _customerService.ListAsync(page, pageSize, ct);
        return Ok(customers);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        var customer = await _customerService.UpdateAsync(id, request, ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _customerService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}