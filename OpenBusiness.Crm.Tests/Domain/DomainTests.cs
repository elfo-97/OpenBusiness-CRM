using FluentAssertions;
using OpenBusiness.Crm.Domain;

namespace OpenBusiness.Crm.Tests.Domain;

public sealed class TenantTests
{
    [Fact]
    public void Create_ShouldInitializeProperties()
    {
        var tenant = new Tenant("Acme Corp", "acme");

        tenant.Id.Should().NotBeEmpty();
        tenant.Name.Should().Be("Acme Corp");
        tenant.Slug.Should().Be("acme");
        tenant.IsActive.Should().BeTrue();
        tenant.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        var tenant = new Tenant("Test", "test");

        tenant.Deactivate();

        tenant.IsActive.Should().BeFalse();
    }
}

public sealed class UserTests
{
    [Fact]
    public void Create_ShouldInitializeProperties()
    {
        var tenantId = Guid.NewGuid();
        var user = new User(tenantId, "user@test.com", "Test User", "hashed_password");

        user.Id.Should().NotBeEmpty();
        user.TenantId.Should().Be(tenantId);
        user.Email.Should().Be("user@test.com");
        user.FullName.Should().Be("Test User");
        user.PasswordHash.Should().Be("hashed_password");
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        user.LastLoginAt.Should().BeNull();
    }

    [Fact]
    public void UpdateLastLogin_ShouldSetLastLoginAt()
    {
        var tenantId = Guid.NewGuid();
        var user = new User(tenantId, "user@test.com", "Test User", "hashed_password");

        user.UpdateLastLogin();

        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        var tenantId = Guid.NewGuid();
        var user = new User(tenantId, "user@test.com", "Test User", "hashed_password");

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }
}

public sealed class CustomerTests
{
    [Fact]
    public void Create_ShouldInitializeProperties()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Acme Corp", "contact@acme.com", "+123456789", "12345678900", "123 Main St");

        customer.Id.Should().NotBeEmpty();
        customer.TenantId.Should().Be(tenantId);
        customer.Name.Should().Be("Acme Corp");
        customer.Email.Should().Be("contact@acme.com");
        customer.Phone.Should().Be("+123456789");
        customer.Document.Should().Be("12345678900");
        customer.Address.Should().Be("123 Main St");
        customer.IsActive.Should().BeTrue();
        customer.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        customer.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithMinimalParams_ShouldWork()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Minimal Corp");

        customer.Name.Should().Be("Minimal Corp");
        customer.Email.Should().BeNull();
        customer.Phone.Should().BeNull();
        customer.Document.Should().BeNull();
        customer.Address.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldUpdatePropertiesAndSetUpdatedAt()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Original", "orig@test.com");

        customer.Update("Updated", "updated@test.com", "+999", "999", "New Address");

        customer.Name.Should().Be("Updated");
        customer.Email.Should().Be("updated@test.com");
        customer.Phone.Should().Be("+999");
        customer.Document.Should().Be("999");
        customer.Address.Should().Be("New Address");
        customer.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "To Deactivate");

        customer.Deactivate();

        customer.IsActive.Should().BeFalse();
    }
}

public sealed class ContactTests
{
    [Fact]
    public void Create_ShouldInitializeProperties()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var contact = new Contact(tenantId, customerId, "John Doe", "john@test.com", "+123", "Manager", true);

        contact.Id.Should().NotBeEmpty();
        contact.TenantId.Should().Be(tenantId);
        contact.CustomerId.Should().Be(customerId);
        contact.Name.Should().Be("John Doe");
        contact.Email.Should().Be("john@test.com");
        contact.Phone.Should().Be("+123");
        contact.Role.Should().Be("Manager");
        contact.IsPrimary.Should().BeTrue();
        contact.IsActive.Should().BeTrue();
        contact.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Update_ShouldUpdatePropertiesAndSetUpdatedAt()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var contact = new Contact(tenantId, customerId, "Original", "orig@test.com");

        contact.Update("Updated", "updated@test.com", "+999", "Director", false);

        contact.Name.Should().Be("Updated");
        contact.Email.Should().Be("updated@test.com");
        contact.Phone.Should().Be("+999");
        contact.Role.Should().Be("Director");
        contact.IsPrimary.Should().BeFalse();
        contact.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var contact = new Contact(tenantId, customerId, "To Deactivate");

        contact.Deactivate();

        contact.IsActive.Should().BeFalse();
    }
}

public sealed class LeadTests
{
    [Fact]
    public void Create_ShouldInitializeProperties()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var lead = new Lead(tenantId, "New Deal", "Description", customerId, 50000m, DateTimeOffset.UtcNow.AddDays(30), userId);

        lead.Id.Should().NotBeEmpty();
        lead.TenantId.Should().Be(tenantId);
        lead.CustomerId.Should().Be(customerId);
        lead.Title.Should().Be("New Deal");
        lead.Description.Should().Be("Description");
        lead.Stage.Should().Be(LeadStage.New);
        lead.EstimatedValue.Should().Be(50000m);
        lead.ExpectedCloseDate.Should().NotBeNull();
        lead.AssignedToUserId.Should().Be(userId);
        lead.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        lead.UpdatedAt.Should().BeNull();
        lead.ClosedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithMinimalParams_ShouldWork()
    {
        var tenantId = Guid.NewGuid();
        var lead = new Lead(tenantId, "Minimal Lead");

        lead.Title.Should().Be("Minimal Lead");
        lead.Description.Should().BeNull();
        lead.CustomerId.Should().BeNull();
        lead.EstimatedValue.Should().BeNull();
        lead.ExpectedCloseDate.Should().BeNull();
        lead.AssignedToUserId.Should().BeNull();
        lead.Stage.Should().Be(LeadStage.New);
    }

    [Fact]
    public void Update_ShouldUpdatePropertiesAndSetUpdatedAt()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var lead = new Lead(tenantId, "Original", null, customerId, 1000m);

        lead.Update("Updated", "New desc", customerId, 2000m, DateTimeOffset.UtcNow.AddDays(15), userId);

        lead.Title.Should().Be("Updated");
        lead.Description.Should().Be("New desc");
        lead.EstimatedValue.Should().Be(2000m);
        lead.AssignedToUserId.Should().Be(userId);
        lead.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ChangeStage_ShouldChangeStageAndSetUpdatedAt()
    {
        var tenantId = Guid.NewGuid();
        var lead = new Lead(tenantId, "Lead to Stage");

        lead.ChangeStage(LeadStage.Proposal);

        lead.Stage.Should().Be(LeadStage.Proposal);
        lead.UpdatedAt.Should().NotBeNull();
        lead.ClosedAt.Should().BeNull();
    }

    [Fact]
    public void ChangeStage_ToWon_ShouldSetClosedAt()
    {
        var tenantId = Guid.NewGuid();
        var lead = new Lead(tenantId, "Lead to Win");

        lead.ChangeStage(LeadStage.Won);

        lead.Stage.Should().Be(LeadStage.Won);
        lead.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void ChangeStage_ToLost_ShouldSetClosedAt()
    {
        var tenantId = Guid.NewGuid();
        var lead = new Lead(tenantId, "Lead to Lose");

        lead.ChangeStage(LeadStage.Lost);

        lead.Stage.Should().Be(LeadStage.Lost);
        lead.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_ShouldSetStageToLost()
    {
        var tenantId = Guid.NewGuid();
        var lead = new Lead(tenantId, "Active Lead");

        lead.Deactivate();

        lead.Stage.Should().Be(LeadStage.Lost);
        lead.ClosedAt.Should().NotBeNull();
    }
}

public sealed class ActivityTests
{
    [Fact]
    public void Create_ShouldInitializeProperties()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var leadId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow.AddHours(-1);
        var activity = new Activity(tenantId, userId, ActivityType.Call, "Follow up", "Discussed pricing", customerId, leadId, occurredAt);

        activity.Id.Should().NotBeEmpty();
        activity.TenantId.Should().Be(tenantId);
        activity.UserId.Should().Be(userId);
        activity.Type.Should().Be(ActivityType.Call);
        activity.Subject.Should().Be("Follow up");
        activity.Description.Should().Be("Discussed pricing");
        activity.CustomerId.Should().Be(customerId);
        activity.LeadId.Should().Be(leadId);
        activity.OccurredAt.Should().Be(occurredAt);
        activity.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithMinimalParams_ShouldUseNowForOccurredAt()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var activity = new Activity(tenantId, userId, ActivityType.Note, "Test");

        activity.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}

public sealed class LeadStageTests
{
    [Fact]
    public void Values_ShouldMatchExpectedOrder()
    {
        ((int)LeadStage.New).Should().Be(1);
        ((int)LeadStage.Qualified).Should().Be(2);
        ((int)LeadStage.Proposal).Should().Be(3);
        ((int)LeadStage.Negotiation).Should().Be(4);
        ((int)LeadStage.Won).Should().Be(5);
        ((int)LeadStage.Lost).Should().Be(6);
    }
}

public sealed class ActivityTypeTests
{
    [Fact]
    public void Values_ShouldMatchExpectedOrder()
    {
        ((int)ActivityType.Note).Should().Be(1);
        ((int)ActivityType.Call).Should().Be(2);
        ((int)ActivityType.Meeting).Should().Be(3);
        ((int)ActivityType.Task).Should().Be(4);
        ((int)ActivityType.Email).Should().Be(5);
    }
}