namespace WMS.Domain.Services;

public interface IPutawaySuggestionService
{
    Task<Guid> SuggestLocationAsync(Guid materialId, CancellationToken cancellationToken);
}