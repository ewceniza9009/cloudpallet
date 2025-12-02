using System.Text.Json.Serialization;
using WMS.Domain.Enums;
using WMS.Domain.Shared;
using WMS.Domain.Entities;
using System.Collections.Generic;

namespace WMS.Domain.Entities.Transaction;

public class Pallet : AuditableEntity<Guid>
{
    [JsonInclude]
    public Guid ReceivingId { get; private set; }
    [JsonInclude]
    public Guid PalletTypeId { get; private set; }
    [JsonInclude]
    public string PalletNumber { get; private set; }
    [JsonInclude]
    public decimal TotalWeight { get; private set; }
    [JsonInclude]
    public string Barcode { get; private set; }
    [JsonInclude]
    public PalletStatus Status { get; private set; }
    [JsonInclude]
    public Guid AccountId { get; private set; }
    [JsonInclude]
    public decimal TareWeight { get; private set; }
    [JsonInclude]
    public bool IsCrossDock { get; private set; }
    public Receiving Receiving { get; private set; } = null!;
    public PalletType PalletType { get; private set; } = null!;
    public Account Account { get; private set; } = null!;

    private readonly List<PalletLine> _lines = new();
    public IReadOnlyCollection<PalletLine> Lines => _lines.AsReadOnly();

    private readonly List<MaterialInventory> _inventory = new();
    public IReadOnlyCollection<MaterialInventory> Inventory => _inventory.AsReadOnly();

    [JsonConstructor]
    private Pallet() : base(Guid.Empty)
    {
        PalletNumber = null!;
        Barcode = null!;
    }

    public static Pallet Create(Guid receivingId, Guid palletTypeId, string palletNumber, decimal tareWeight, Guid accountId, bool isCrossDock = false) // <-- ADD isCrossDock parameter
    {
        var pallet = new Pallet(
            Guid.NewGuid(),
            receivingId,
            palletTypeId,
            palletNumber,
            $"TEMP-{Guid.NewGuid()}",
            tareWeight,
            accountId);
        pallet.IsCrossDock = isCrossDock; // <-- SET FLAG
        if (isCrossDock)
        {
            pallet.Status = PalletStatus.CrossDockPending; // <-- Optional: Set specific status
        }
        return pallet;
    }

    public PalletLine AddLine(Guid palletId, Guid materialId, decimal quantity, decimal netWeight, string batchNumber, DateTime dateOfManufacture, DateTime? expiryDate, Guid accountId)
    {
        var palletLine = PalletLine.Create(palletId, materialId, quantity, netWeight, batchNumber, dateOfManufacture, expiryDate, accountId);
        palletLine.SetPalletId(Id);
        _lines.Add(palletLine);
        TotalWeight = _lines.Sum(l => l.Weight);
        return palletLine;
    }


    public void SetBarcode(string barcode) { Barcode = barcode; }

    private Pallet(Guid id, Guid receivingId, Guid palletTypeId, string palletNumber, string barcode, decimal tareWeight, Guid accountId) : base(id)
    {
        ReceivingId = receivingId;
        PalletTypeId = palletTypeId;
        PalletNumber = palletNumber;
        Barcode = barcode;
        TareWeight = tareWeight;
        AccountId = accountId;
        Status = PalletStatus.Received; // Default status
        TotalWeight = 0;
        IsCrossDock = false; // Default
    }

    public void CompleteCrossDock()
    {
        if (!IsCrossDock || Status != PalletStatus.CrossDockPending)
            throw new InvalidOperationException("Pallet is not marked for cross-docking or not in the correct status.");
        Status = PalletStatus.Putaway; // Or another appropriate 'ready for shipping staging' status
    }
}