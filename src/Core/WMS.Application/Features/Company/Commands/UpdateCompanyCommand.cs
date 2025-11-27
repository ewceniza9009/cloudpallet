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
    string Website,
    string Gs1CompanyPrefix,
    string DefaultBarcodeFormat,
    bool IsPickingWeightReadonly) : IRequest;

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

        if (company is null)
        {
            throw new KeyNotFoundException($"Company not found.");
        }

        if (request.Id != Guid.Empty && company.Id != request.Id)
        {
             throw new InvalidOperationException($"Company ID mismatch.");
        }

        UpdateEntityProperty(company, "Name", request.Name);
        UpdateEntityProperty(company, "TaxId", request.TaxId);
        UpdateEntityProperty(company, "Address", new Address(request.Address.Street, request.Address.City, request.Address.State, request.Address.PostalCode, request.Address.Country));
        UpdateEntityProperty(company, "PhoneNumber", request.PhoneNumber);
        UpdateEntityProperty(company, "Email", request.Email);
        UpdateEntityProperty(company, "Website", request.Website);

        // Use the explicit method for GS1 settings
        company.UpdateGs1Settings(request.Gs1CompanyPrefix, request.DefaultBarcodeFormat);
        company.UpdatePickingSettings(request.IsPickingWeightReadonly);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private void UpdateEntityProperty(object entity, string propertyName, object value)
    {
        var property = entity.GetType().GetProperty(propertyName);
        if (property != null && property.CanWrite)
        {
            property.SetValue(entity, value);
        }
        else
        {
            // Handle private setters
             var backingField = entity.GetType().GetField($"<{propertyName}>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
             if (backingField != null)
             {
                 backingField.SetValue(entity, value);
             }
             else
             {
                 // Fallback to property setter even if private
                 property?.SetValue(entity, value, null);
             }
        }
    }
}