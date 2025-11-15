// WMS.Application/Abstractions/Security/ICurrentUserService.cs
using WMS.Domain.Enums;

namespace WMS.Application.Abstractions.Security;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    UserRole? UserRole { get; }
}