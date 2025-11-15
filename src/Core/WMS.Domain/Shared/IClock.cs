namespace WMS.Domain.Shared;

public interface IClock
{
 DateTime UtcNow { get; }
 DateTime Now { get; }
}
