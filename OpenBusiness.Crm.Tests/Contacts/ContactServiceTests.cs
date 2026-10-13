using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenBusiness.Crm.Api.Services.Contacts;
using OpenBusiness.Crm.Application.Contacts;
using OpenBusiness.Crm.Tests;

namespace OpenBusiness.Crm.Tests.Contacts;

public sealed class ContactServiceTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;
    private IContactService _service = default!;

    public ContactServiceTests()
    {
        _fixture = new TestFixture();
    }

    public Task InitializeAsync()
    {
        _fixture.InitializeAsync().GetAwaiter().GetResult();
        _service = _fixture.Services.GetRequiredService<IContactService>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateContact()
    {
        var customerId = Guid.NewGuid();
        var request = new CreateContactRequest(customerId, "John Doe", "john@test.com", "+123", "Manager", true);

        var result = await _service.CreateAsync(request);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.CustomerId.Should().Be(customerId);
        result.Name.Should().Be("John Doe");
        result.Email.Should().Be("john@test.com");
        result.Phone.Should().Be("+123");
        result.Role.Should().Be("Manager");
        result.IsPrimary.Should().BeTrue();
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetAsync_ShouldReturnContact_WhenExists()
    {
        var customerId = Guid.NewGuid();
        var created = await _service.CreateAsync(new CreateContactRequest(customerId, "Test Contact", "test@corp.com"));

        var result = await _service.GetAsync(created.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Name.Should().Be("Test Contact");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenNotExists()
    {
        var result = await _service.GetAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_ShouldReturnPagedContacts()
    {
        var customerId = Guid.NewGuid();
        await _service.CreateAsync(new CreateContactRequest(customerId, "Contact 1"));
        await _service.CreateAsync(new CreateContactRequest(customerId, "Contact 2"));
        await _service.CreateAsync(new CreateContactRequest(customerId, "Contact 3"));

        var page1 = await _service.ListAsync(customerId, page: 1, pageSize: 2);
        var page2 = await _service.ListAsync(customerId, page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(1);
        page1.Select(c => c.Id).Should().NotIntersectWith(page2.Select(c => c.Id));
    }

    [Fact]
    public async Task ListAsync_ShouldFilterByCustomerId()
    {
        var customer1 = Guid.NewGuid();
        var customer2 = Guid.NewGuid();
        await _service.CreateAsync(new CreateContactRequest(customer1, "Contact 1"));
        await _service.CreateAsync(new CreateContactRequest(customer2, "Contact 2"));

        var results = await _service.ListAsync(customer1);

        results.Should().HaveCount(1);
        results.First().Name.Should().Be("Contact 1");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateContact_WhenExists()
    {
        var customerId = Guid.NewGuid();
        var created = await _service.CreateAsync(new CreateContactRequest(customerId, "Original", "orig@test.com"));

        var updated = await _service.UpdateAsync(created.Id, new UpdateContactRequest("Updated", "updated@test.com", "+999", "Director", false));

        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated");
        updated.Email.Should().Be("updated@test.com");
        updated.Phone.Should().Be("+999");
        updated.Role.Should().Be("Director");
        updated.IsPrimary.Should().BeFalse();
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenNotExists()
    {
        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateContactRequest("Ghost"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeactivateContact_WhenExists()
    {
        var customerId = Guid.NewGuid();
        var created = await _service.CreateAsync(new CreateContactRequest(customerId, "To Delete"));

        var deleted = await _service.DeleteAsync(created.Id);

        deleted.Should().BeTrue();

        var result = await _service.GetAsync(created.Id);
        result!.IsActive.Should().BeFalse();
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
            var otherService = otherTenantFixture.Services.GetRequiredService<IContactService>();

            var customerId = Guid.NewGuid();
            var otherCustomerId = Guid.NewGuid();

            var created = await _service.CreateAsync(new CreateContactRequest(customerId, "Tenant A Contact"));
            var otherCreated = await otherService.CreateAsync(new CreateContactRequest(otherCustomerId, "Tenant B Contact"));

            var ourList = await _service.ListAsync(customerId);
            var otherList = await otherService.ListAsync(otherCustomerId);

            ourList.Should().ContainSingle(c => c.Id == created.Id);
            ourList.Should().NotContain(c => c.Id == otherCreated.Id);

            otherList.Should().ContainSingle(c => c.Id == otherCreated.Id);
            otherList.Should().NotContain(c => c.Id == created.Id);
        }
        finally
        {
            await otherTenantFixture.DisposeAsync();
        }
    }
}