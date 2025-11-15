using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Repositories;

public class UserRepository(WmsDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Users.FindAsync([id], cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Users
            .AsNoTracking()
            .OrderBy(u => u.LastName)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        context.Users.Add(user);
        return Task.CompletedTask;
    }
}