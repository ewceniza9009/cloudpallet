using WMS.Domain.Entities;

namespace WMS.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken);    
    Task AddAsync(User user, CancellationToken cancellationToken);
}