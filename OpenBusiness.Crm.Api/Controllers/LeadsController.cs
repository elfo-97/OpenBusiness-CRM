using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenBusiness.Crm.Application.Leads;
using OpenBusiness.Crm.Api.Services.Leads;

namespace OpenBusiness.Crm.Api.Controllers;

[ApiController]
[Route("api/leads")]
[Authorize(Policy = "MemberOrAbove")]
public sealed class LeadsController : ControllerBase
{
    private readonly ILeadService _leadService;

    public LeadsController(ILeadService leadService)
    {
        _leadService = leadService;
    }

    [HttpPost]
    public async Task<ActionResult<LeadResponse>> Create([FromBody] CreateLeadRequest request, CancellationToken ct)
    {
        var lead = await _leadService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = lead.Id }, lead);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeadResponse>> Get(Guid id, CancellationToken ct)
    {
        var lead = await _leadService.GetAsync(id, ct);
        return lead is null ? NotFound() : Ok(lead);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeadResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var leads = await _leadService.ListAsync(page, pageSize, ct);
        return Ok(leads);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LeadResponse>> Update(Guid id, [FromBody] UpdateLeadRequest request, CancellationToken ct)
    {
        var lead = await _leadService.UpdateAsync(id, request, ct);
        return lead is null ? NotFound() : Ok(lead);
    }

    [HttpPatch("{id:guid}/stage")]
    public async Task<ActionResult<LeadResponse>> ChangeStage(Guid id, [FromBody] ChangeLeadStageRequest request, CancellationToken ct)
    {
        var lead = await _leadService.ChangeStageAsync(id, request, ct);
        return lead is null ? NotFound() : Ok(lead);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _leadService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}