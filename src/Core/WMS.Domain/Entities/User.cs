using Microsoft.AspNetCore.Identity;
using WMS.Domain.Enums;
using WMS.Domain.Events;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public Guid CompanyId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();


    private User()     
    {
        FirstName = null!;
        LastName = null!;
    }

    public User(string userName, string email, string firstName, string lastName, UserRole role) : base(userName)
    {
        Id = Guid.NewGuid();
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
        IsActive = true;
        CompanyId = Guid.Parse("10000000-0000-0000-0000-000000000001");     
    }

    public void ChangeRole(UserRole newRole)
    {
        if (newRole == Role) return;

        Role = newRole;
        AddDomainEvent(new UserRoleChangedEvent(Id, newRole));
    }
}