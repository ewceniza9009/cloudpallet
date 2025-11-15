using WMS.Domain.Entities;

namespace WMS.Application.Abstractions.Persistence;

public interface ICompanyRepository
{
    Task<Company?> GetCompanyAsync(CancellationToken cancellationToken);

}