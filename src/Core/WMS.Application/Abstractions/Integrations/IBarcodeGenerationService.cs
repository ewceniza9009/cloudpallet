namespace WMS.Application.Abstractions.Integrations;

public interface IBarcodeGenerationService
{
    string GenerateSSCCBarcode(Guid uniqueId);
    string GenerateItemBarcode(Guid materialId);
}