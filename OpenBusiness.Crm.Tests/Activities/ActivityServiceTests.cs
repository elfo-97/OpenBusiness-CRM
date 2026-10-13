using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenBusiness.Crm.Api.Services.Activities;
using OpenBusiness.Crm.Application.Activities;
using OpenBusiness.Crm.Domain;
using OpenBusiness.Crm.Tests;

namespace OpenBusiness.Crm.Tests.Activities;

public sealed class ActivityServiceTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;
    private IActivityService _service = default!;

    public ActivityServiceTests()
    {
        _fixture = new TestFixture();
    }

    public Task InitializeAsync()
    {
        _fixture.InitializeAsync().GetAwaiter().GetResult();
        _service = _fixture.Services.GetRequiredService<IActivityService>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateActivity()
    {
        var request = new CreateActivityRequest(
            ActivityType.Call,
            "Follow up call",
            "Discussed pricing",
            null,
            null,
            DateTimeOffset.UtcNow.AddHours(-1));

        var result = await _service.CreateAsync(request);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Type.Should().Be(ActivityType.Call);
        result.Subject.Should().Be("Follow up call");
        result.Description.Should().Be("Discussed pricing");
        result.UserId.Should().Be(_fixture.UserId);
        result.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddHours(-1), TimeSpan.FromSeconds(5));
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetAsync_ShouldReturnActivity_WhenExists()
    {
        var created = await _service.CreateAsync(new CreateActivityRequest(ActivityType.Note, "Test Note"));

        var result = await _service.GetAsync(created.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Subject.Should().Be("Test Note");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenNotExists()
    {
        var result = await _service.GetAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_ShouldFilterByCustomerId()
    {
        await _service.CreateAsync(new CreateActivityRequest(ActivityType.Note, "Activity 1", null, Guid.NewGuid()));
        await _service.CreateAsync(new CreateActivityRequest(ActivityType.Call, "Activity 2", null, Guid.NewGuid()));
        await _service.CreateAsync(new CreateActivityRequest(ActivityType.Meeting, "Activity 3"));

        var customerId = Guid.NewGuid();
        await _service.CreateAsync(new CreateActivityRequest(ActivityType.Email, "Customer Activity", null, customerId));

        var results = await _service.ListAsync(customerId: customerId);

        results.Should().HaveCount(1);
        results.First().Subject.Should().Be("Customer Activity");
    }

    [Fact]
    public async Task ListAsync_ShouldFilterByLeadId()
    {
        var leadId = Guid.NewGuid();
        await _service.CreateAsync(new CreateActivityRequest(ActivityType.Note, "Activity 1", null, null, Guid.NewGuid()));
        await _service.CreateAsync(new CreateActivityRequest(ActivityType.Call, "Lead Activity", null, null, leadId));

        var results = await _service.ListAsync(leadId: leadId);

        results.Should().HaveCount(1);
        results.First().Subject.Should().Be("Lead Activity");
    }

    [Fact]
    public async Task ListAsync_ShouldReturnPagedActivities()
    {
        await _service.CreateAsync(new CreateActivityRequest(ActivityType.Note, "Activity 1"));
        await _service.CreateAsync(new CreateActivityRequest(ActivityType.Call, "Activity 2"));
        await _service.CreateAsync(new CreateActivityRequest(ActivityType.Meeting, "Activity 3"));

        var page1 = await _service.ListAsync(page: 1, pageSize: 2);
        var page2 = await _service.ListAsync(page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(1);
    }

    [Fact]
    public async Task TenantIsolation_ShouldNotLeakAcrossTenants()
    {
        var otherTenantFixture = new TestFixture();
        await otherTenantFixture.InitializeAsync();

        try
        {
            var otherService = otherTenantFixture.Services.GetRequiredService<IActivityService>();

            var created = await _service.CreateAsync(new CreateActivityRequest(ActivityType.Note, "Tenant A Activity"));
            var otherCreated = await otherService.CreateAsync(new CreateActivityRequest(ActivityType.Call, "Tenant B Activity"));

            var ourList = await _service.ListAsync();
            var otherList = await otherService.ListAsync();

            ourList.Should().ContainSingle(a => a.Id == created.Id);
            ourList.Should().NotContain(a => a.Id == otherCreated.Id);

            otherList.Should().ContainSingle(a => a.Id == otherCreated.Id);
            otherList.Should().NotContain(a => a.Id == created.Id);
        }
        finally
        {
            await otherTenantFixture.DisposeAsync();
        }
    }
}