namespace WMS.Domain.Enums;

public enum CompanyStatus { Active, Suspended, Canceled }

// Role-Based Access Control
public enum UserRole { Admin, Operator, Finance }

// Dock & Yard Management
public enum DockType { Inbound, Outbound, Both }
public enum YardSpotStatus { Available, Occupied, Reserved }
public enum AppointmentStatus { Scheduled, InProgress, Unloading, Completed, Cancelled }
public enum AppointmentType { Receiving, Shipping }

public enum MaterialType
{
    Normal,
    Kit,
    Repack
}

// Location & Inventory
public enum LocationType { Picking, Storage, Staging }
public enum InventoryStatus { Available, Reserved, Quarantined, AwaitingLabeling }
public enum PalletLineStatus { Pending, Processed }

// Transactions
public enum PickReason { Order, Adjustment }
public enum PickStatus { Planned, Confirmed, Short }
public enum AdjustmentReason { Damage, Count, Expiry, Correction }
public enum TransactionStatus { Planned, Completed }
public enum ReceivingStatus { Pending, InProgress, Completed }
public enum PalletStatus { Received, Labeled, Putaway, CrossDockPending }
public enum ShipmentStatus { Packed, Shipped }

// Billing & Invoicing
public enum AccountType { ThreePL, Direct, Vendor }
public enum InvoiceStatus { Draft, Issued, Paid, Overdue }

public enum ServiceType
{
    Storage, // General category
    Handling,
    VAS,

    // Specific Storage Types
    Staging,
    Chilling,
    FrozenStorage,
    CoolStorage,
    DeepFrozenStorage,
    ULTStorage,
    // CryogenicStorage (if needed)

    // Specific VAS Types
    Blasting,
    Repack,
    Split,
    Labeling,
    CrossDock,
    Fumigation,
    Surcharge,
    CycleCount,
    Kitting
}

public enum RateUom { Pallet, Kg, Day, Cycle, Each, Hour, Shipment, Percent }

public enum TempZone
{
    Chilling,
    FrozenStorage,
    CoolStorage,
    DeepFrozenStorage,
    ULTStorage
}

public enum BarcodeFormat { GS1_128, UPC }
public enum ComplianceLabelType { None, Export, Allergen, ForeignLanguage }

public enum LedgerTransactionType
{
    Receiving,
    Picking,
    Adjustment,
    ItemTransferOut, // Item moved *from* an inventory record
    ItemTransferIn,  // Item moved *to* a new inventory record (often via VAS or direct transfer command)
    VAS_Consumption, // Raw material used in VAS
    VAS_Production   // Finished good created by VAS
    // Note: Pallet-level Putaway/Transfer moves often don't need ledger entries
    // unless you specifically track location changes in the ledger itself. Consider if needed later.
}

public enum AmendmentType
{
    LineAmendment,    // Amendment to specific line values (quantity, weight)
    TransactionVoid   // Full transaction void with inventory reversal
}