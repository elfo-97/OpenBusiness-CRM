using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenBusiness.Crm.Api.Services.Customers;
using OpenBusiness.Crm.Application.Customers;
using OpenBusiness.Crm.Tests;

namespace OpenBusiness.Crm.Tests.Customers;

public sealed class CustomerServiceTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;
    private ICustomerService _service = default!;

    public CustomerServiceTests()
    {
        _fixture = new TestFixture();
    }

    public Task InitializeAsync()
    {
        _fixture.InitializeAsync().GetAwaiter().GetResult();
        _service = _fixture.Services.GetRequiredService<ICustomerService>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateCustomer()
    {
        var request = new CreateCustomerRequest("Acme Corp", "contact@acme.com", "+123****6789", "12345678900", "123 Main St");

        var result = await _service.CreateAsync(request);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Acme Corp");
        result.Email.Should().Be("contact@acme.com");
        result.Phone.Should().Be("+123****6789");
        result.Document.Should().Be("12345678900");
        result.Address.Should().Be("123 Main St");
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetAsync_ShouldReturnCustomer_WhenExists()
    {
        var created = await _service.CreateAsync(new CreateCustomerRequest("Test Corp", "test@corp.com"));

        var result = await _service.GetAsync(created.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Name.Should().Be("Test Corp");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenNotExists()
    {
        var result = await _service.GetAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_ShouldReturnPagedCustomers()
    {
        await _service.CreateAsync(new CreateCustomerRequest("Customer 1"));
        await _service.CreateAsync(new CreateCustomerRequest("Customer 2"));
        await _service.CreateAsync(new CreateCustomerRequest("Customer 3"));

        var page1 = await _service.ListAsync(page: 1, pageSize: 2);
        var page2 = await _service.ListAsync(page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(1);
        page1.Select(c => c.Id).Should().NotIntersectWith(page2.Select(c => c.Id));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCustomer_WhenExists()
    {
        var created = await _service.CreateAsync(new CreateCustomerRequest("Original", "orig@test.com"));

        var updated = await _service.UpdateAsync(created.Id, new UpdateCustomerRequest("Updated", "updated@test.com", "+999", "999", "New Address"));

        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated");
        updated.Email.Should().Be("updated@test.com");
        updated.Phone.Should().Be("+999");
        updated.Document.Should().Be("999");
        updated.Address.Should().Be("New Address");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenNotExists()
    {
        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateCustomerRequest("Ghost"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeactivateCustomer_WhenExists()
    {
        var created = await _service.CreateAsync(new CreateCustomerRequest("To Delete"));

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
            var otherService = otherTenantFixture.Services.GetRequiredService<ICustomerService>();

            var created = await _service.CreateAsync(new CreateCustomerRequest("Tenant A Customer"));
            var otherCreated = await otherService.CreateAsync(new CreateCustomerRequest("Tenant B Customer"));

            var ourList = await _service.ListAsync();
            var otherList = await otherService.ListAsync();

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