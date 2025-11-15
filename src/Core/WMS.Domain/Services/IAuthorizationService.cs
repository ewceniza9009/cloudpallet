using WMS.Domain.Entities;

namespace WMS.Domain.Services;

public interface IAuthorizationService
{
    Task<bool> CanExecute(User user, string action, Guid? resourceId = null, CancellationToken cancellationToken = default);
}