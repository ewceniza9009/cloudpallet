using WMS.Application.Abstractions.Integrations;

namespace WMS.Infrastructure.Integrations;

public class BarcodeGenerationService : IBarcodeGenerationService
{
    private const string ExtensionDigit = "0";
    private const string Gs1CompanyPrefix = "0123456";
    private long _serialCounter = DateTime.UtcNow.Ticks;

    public string GenerateSSCCBarcode(Guid uniqueId)
    {
        var serialReference = Interlocked.Increment(ref _serialCounter).ToString().PadLeft(9, '0');

        if (serialReference.Length > 9)
        {
            serialReference = serialReference[^9..];
        }

        var baseNumber = $"{ExtensionDigit}{Gs1CompanyPrefix}{serialReference}";

        var checkDigit = CalculateChecksum(baseNumber);

        return $"(00){baseNumber}{checkDigit}";
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