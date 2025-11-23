using WMS.Application.Abstractions.Integrations;
using Microsoft.Extensions.DependencyInjection;
using WMS.Infrastructure.Persistence;
using System.Linq;

namespace WMS.Infrastructure.Integrations;

public class BarcodeGenerationService : IBarcodeGenerationService
{
    private const string ExtensionDigit = "00";
    private readonly IServiceScopeFactory _scopeFactory;
    private long _serialCounter = DateTime.UtcNow.Ticks;

    public BarcodeGenerationService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public string GenerateSSCCBarcode(Guid uniqueId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WmsDbContext>();

        var company = context.Companies.FirstOrDefault();
        var gs1Prefix = company?.Gs1CompanyPrefix ?? "0000000";

        var serialReference = Interlocked.Increment(ref _serialCounter).ToString().PadLeft(9, '0');

        if (serialReference.Length > 9)
        {
            serialReference = serialReference[^9..];
        }

        var baseNumber = $"({ExtensionDigit}-{gs1Prefix}){serialReference}";

        var checkDigit = CalculateChecksum(baseNumber);

        return $"{baseNumber}{checkDigit}";
    }

    public string GenerateItemBarcode(Guid materialId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WmsDbContext>();

        var material = context.Materials.FirstOrDefault(m => m.Id == materialId);
        string prefix;

        if (material != null && !string.IsNullOrWhiteSpace(material.Gs1BarcodePrefix))
        {
            prefix = material.Gs1BarcodePrefix;
        }
        else
        {
            var company = context.Companies.FirstOrDefault();
            prefix = company?.Gs1CompanyPrefix ?? "0000000";
        }

        var serialReference = Interlocked.Increment(ref _serialCounter).ToString().PadLeft(5, '0'); 
        
        var baseNumber = $"({prefix}){serialReference}";
        
        var checkDigit = CalculateChecksum(baseNumber);
        
        return $"{baseNumber}{checkDigit}";
    }

    private static int CalculateChecksum(string data)
    {
        int sum = 0;
        for (int i = 0; i < data.Length; i++)
        {
            int digit = data[i] - '0';
            sum += (i % 2 == 0) ? digit * 3 : digit;
        }

        int remainder = sum % 10;
        return (remainder == 0) ? 0 : 10 - remainder;
    }
}