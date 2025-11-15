using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly WmsDbContext _context;

    public CompanyRepository(WmsDbContext context)
    {
        _context = context;
    }

    public async Task<Company?> GetCompanyAsync(CancellationToken cancellationToken)
    {
        return await _context.Companies.FirstOrDefaultAsync(cancellationToken);
    }

}