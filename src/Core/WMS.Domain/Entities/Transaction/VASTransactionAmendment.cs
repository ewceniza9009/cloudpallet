using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities.Transaction;

/// <summary>
/// Represents an auditable record of an amendment made to a VAS transaction.
/// This includes both individual line amendments and full transaction voids.
/// </summary>
public class VASTransactionAmendment : AuditableEntity<Guid>
{
    /// <summary>
    /// The VAS transaction that was amended or voided.
    /// </summary>
    public Guid OriginalTransactionId { get; private set; }

    /// <summary>
    /// The user who performed the amendment.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The time the amendment was recorded.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// The reason for the amendment.
    /// </summary>
    public string Reason { get; private set; }

    /// <summary>
    /// JSON-serialized details of the amendment (what changed, old/new values).
    /// </summary>
    public string AmendmentDetails { get; private set; }

    /// <summary>
    /// Type of amendment: LineAmendment or TransactionVoid.
    /// </summary>
    public AmendmentType AmendmentType { get; private set; }

    // --- Navigation Properties ---
    public VASTransaction OriginalTransaction { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private VASTransactionAmendment() : base(Guid.Empty)
    {
        Reason = null!;
        AmendmentDetails = null!;
    }

    public static VASTransactionAmendment CreateLineAmendment(
        Guid originalTransactionId,
        Guid userId,
        string reason,
        string amendmentDetails)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Amendment reason is required.", nameof(reason));

        return new VASTransactionAmendment
        {
            Id = Guid.NewGuid(),
            OriginalTransactionId = originalTransactionId,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            Reason = reason,
            AmendmentDetails = amendmentDetails,
            AmendmentType = AmendmentType.LineAmendment
        };
    }

    public static VASTransactionAmendment CreateVoidAmendment(
        Guid originalTransactionId,
        Guid userId,
        string reason,
        string amendmentDetails)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Void reason is required.", nameof(reason));

        return new VASTransactionAmendment
        {
            Id = Guid.NewGuid(),
            OriginalTransactionId = originalTransactionId,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            Reason = reason,
            AmendmentDetails = amendmentDetails,
            AmendmentType = AmendmentType.TransactionVoid
        };
    }
}
