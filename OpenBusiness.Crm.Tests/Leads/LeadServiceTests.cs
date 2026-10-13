using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenBusiness.Crm.Api.Services.Leads;
using OpenBusiness.Crm.Application.Leads;
using OpenBusiness.Crm.Domain;
using OpenBusiness.Crm.Tests;

namespace OpenBusiness.Crm.Tests.Leads;

public sealed class LeadServiceTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;
    private ILeadService _service = default!;

    public LeadServiceTests()
    {
        _fixture = new TestFixture();
    }

    public Task InitializeAsync()
    {
        _fixture.InitializeAsync().GetAwaiter().GetResult();
        _service = _fixture.Services.GetRequiredService<ILeadService>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateLead()
    {
        var request = new CreateLeadRequest(
            "New Deal",
            "Description here",
            null,
            50000m,
            DateTimeOffset.UtcNow.AddDays(30),
            _fixture.UserId);

        var result = await _service.CreateAsync(request);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Title.Should().Be("New Deal");
        result.Description.Should().Be("Description here");
        result.Stage.Should().Be(LeadStage.New);
        result.EstimatedValue.Should().Be(50000m);
        result.AssignedToUserId.Should().Be(_fixture.UserId);
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetAsync_ShouldReturnLead_WhenExists()
    {
        var created = await _service.CreateAsync(new CreateLeadRequest("Test Lead"));

        var result = await _service.GetAsync(created.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Title.Should().Be("Test Lead");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenNotExists()
    {
        var result = await _service.GetAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_ShouldReturnPagedLeads()
    {
        await _service.CreateAsync(new CreateLeadRequest("Lead 1"));
        await _service.CreateAsync(new CreateLeadRequest("Lead 2"));
        await _service.CreateAsync(new CreateLeadRequest("Lead 3"));

        var page1 = await _service.ListAsync(page: 1, pageSize: 2);
        var page2 = await _service.ListAsync(page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateLead_WhenExists()
    {
        var created = await _service.CreateAsync(new CreateLeadRequest("Original", null, null, 1000m));

        var updated = await _service.UpdateAsync(created.Id, new UpdateLeadRequest("Updated", "New desc", null, 2000m, DateTimeOffset.UtcNow.AddDays(15), _fixture.UserId));

        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Updated");
        updated.Description.Should().Be("New desc");
        updated.EstimatedValue.Should().Be(2000m);
        updated.AssignedToUserId.Should().Be(_fixture.UserId);
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenNotExists()
    {
        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateLeadRequest("Ghost"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task ChangeStageAsync_ShouldChangeStage()
    {
        var created = await _service.CreateAsync(new CreateLeadRequest("Lead to Stage"));

        var updated = await _service.ChangeStageAsync(created.Id, new ChangeLeadStageRequest(LeadStage.Proposal));

        updated.Should().NotBeNull();
        updated!.Stage.Should().Be(LeadStage.Proposal);
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeStageAsync_ShouldSetClosedAt_WhenWon()
    {
        var created = await _service.CreateAsync(new CreateLeadRequest("Lead to Win"));

        var updated = await _service.ChangeStageAsync(created.Id, new ChangeLeadStageRequest(LeadStage.Won));

        updated.Should().NotBeNull();
        updated!.Stage.Should().Be(LeadStage.Won);
        updated.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeStageAsync_ShouldSetClosedAt_WhenLost()
    {
        var created = await _service.CreateAsync(new CreateLeadRequest("Lead to Lose"));

        var updated = await _service.ChangeStageAsync(created.Id, new ChangeLeadStageRequest(LeadStage.Lost));

        updated.Should().NotBeNull();
        updated!.Stage.Should().Be(LeadStage.Lost);
        updated.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeactivateLead_WhenExists()
    {
        var created = await _service.CreateAsync(new CreateLeadRequest("To Delete"));

        var deleted = await _service.DeleteAsync(created.Id);

        deleted.Should().BeTrue();

        var result = await _service.GetAsync(created.Id);
        result!.Stage.Should().Be(LeadStage.Lost);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
    {
        var result = await _service.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TenantIsolation_ShouldNotLeakAcrossTenants()
    {
        var otherTenantFixture = new TestFixture();
        await otherTenantFixture.InitializeAsync();

        try
        {
            var otherService = otherTenantFixture.Services.GetRequiredService<ILeadService>();

            var created = await _service.CreateAsync(new CreateLeadRequest("Tenant A Lead"));
            var otherCreated = await otherService.CreateAsync(new CreateLeadRequest("Tenant B Lead"));

            var ourList = await _service.ListAsync();
            var otherList = await otherService.ListAsync();

            ourList.Should().ContainSingle(l => l.Id == created.Id);
            ourList.Should().NotContain(l => l.Id == otherCreated.Id);

            otherList.Should().ContainSingle(l => l.Id == otherCreated.Id);
            otherList.Should().NotContain(l => l.Id == created.Id);
        }
        finally
        {
            await otherTenantFixture.DisposeAsync();
        }
    }
}