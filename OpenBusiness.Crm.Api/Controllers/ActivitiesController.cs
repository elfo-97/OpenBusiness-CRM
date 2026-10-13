using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenBusiness.Crm.Application.Activities;
using OpenBusiness.Crm.Api.Services.Activities;

namespace OpenBusiness.Crm.Api.Controllers;

[ApiController]
[Route("api/activities")]
[Authorize(Policy = "MemberOrAbove")]
public sealed class ActivitiesController : ControllerBase
{
    private readonly IActivityService _activityService;

    public ActivitiesController(IActivityService activityService)
    {
        _activityService = activityService;
    }

    [HttpPost]
    public async Task<ActionResult<ActivityResponse>> Create([FromBody] CreateActivityRequest request, CancellationToken ct)
    {
        var activity = await _activityService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = activity.Id }, activity);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ActivityResponse>> Get(Guid id, CancellationToken ct)
    {
        var activity = await _activityService.GetAsync(id, ct);
        return activity is null ? NotFound() : Ok(activity);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ActivityResponse>>> List(
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? leadId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var activities = await _activityService.ListAsync(customerId, leadId, page, pageSize, ct);
        return Ok(activities);
    }
}