using MediatR;
using WMS.Application.Abstractions.Persistence;     
using AutoMapper;      
using WMS.Domain.Entities;     
using WMS.Domain.ValueObjects;    

namespace WMS.Application.Features.Companies.Queries;

public record AddressDto(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

public record CompanyDto(
    Guid Id,
    string Name,
    string TaxId,
    AddressDto Address,
    string PhoneNumber,
    string Email,
    string Website,
    string Status,  
    string SubscriptionPlan);  

public record GetCompanyDetailsQuery : IRequest<CompanyDto?>;

public class GetCompanyDetailsQueryHandler : IRequestHandler<GetCompanyDetailsQuery, CompanyDto?>
{
    private readonly ICompanyRepository _companyRepository;

    public GetCompanyDetailsQueryHandler(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }
    public async Task<CompanyDto?> Handle(GetCompanyDetailsQuery request, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetCompanyAsync(cancellationToken);
        if (company is null)
        {
            return null;
        }

        return new CompanyDto(
            company.Id,
            company.Name,
            company.TaxId,
            new AddressDto(
                company.Address.Street,
                company.Address.City,
                company.Address.State,
                company.Address.PostalCode,
                company.Address.Country),
            company.PhoneNumber,
            company.Email,
            company.Website,
            company.Status.ToString(),
            company.SubscriptionPlan
        );
    }
}