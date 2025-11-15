// WMS.Application/Abstractions/Persistence/IUnitOfWork.cs
namespace WMS.Application.Abstractions.Persistence;

/// <summary>
/// Represents the Unit of Work pattern, ensuring that a series of operations
/// are committed to the database as a single atomic transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}