using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Companies.Queries;   
using WMS.Domain.ValueObjects;
namespace WMS.Application.Features.Companies.Commands;

public record UpdateCompanyCommand(
    Guid Id,
    string Name,
    string TaxId,
    AddressDto Address,
    string PhoneNumber,
    string Email,
    string Website) : IRequest;

public class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCompanyCommandHandler(ICompanyRepository companyRepository, IUnitOfWork unitOfWork)
    {
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
    }
    public async Task Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetCompanyAsync(cancellationToken);

        if (request.Id == Guid.Empty)
        {
            UpdateCompany(_companyRepository, _unitOfWork, request, cancellationToken);
            return;
        }
        
        if (company is null || company.Id != request.Id)       
        {
            throw new KeyNotFoundException($"Company with ID {request.Id} not found.");
        }

        UpdateCompany(_companyRepository, _unitOfWork, request, cancellationToken);
    }

    private async void UpdateCompany(ICompanyRepository company, IUnitOfWork unitOfWork, UpdateCompanyCommand request, CancellationToken cancellationToken) 
    {
        company.GetType().GetProperty("Name")?.SetValue(company, request.Name, null);
        company.GetType().GetProperty("TaxId")?.SetValue(company, request.TaxId, null);
        company.GetType().GetProperty("Address")?.SetValue(company, new Address(request.Address.Street, request.Address.City, request.Address.State, request.Address.PostalCode, request.Address.Country), null);
        company.GetType().GetProperty("PhoneNumber")?.SetValue(company, request.PhoneNumber, null);
        company.GetType().GetProperty("Email")?.SetValue(company, request.Email, null);
        company.GetType().GetProperty("Website")?.SetValue(company, request.Website, null);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}