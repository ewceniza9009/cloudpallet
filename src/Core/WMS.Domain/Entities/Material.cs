// ---- File: src/Core/WMS.Domain/Entities/Material.cs ----

using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities;

public class Material : AggregateRoot<Guid>
{
    public string Name { get; set; }
    public string Sku { get; set; }
    public string Description { get; set; }
    public Guid CategoryId { get; set; }
    public Guid UomId { get; set; }
    public bool Perishable { get; set; } // Already exists
    public TempZone RequiredTempZone { get; set; }
    public BarcodeFormat DefaultBarcodeFormat { get; set; }
    public decimal BaseWeight { get; set; }
    public decimal DimensionsLength { get; set; }
    public decimal DimensionsWidth { get; set; }
    public decimal DimensionsHeight { get; set; }
    public decimal CostPerUnit { get; set; }
    public int MinStockLevel { get; set; }
    public int MaxStockLevel { get; set; }
    public int ShelfLifeDays { get; set; } // Already exists
    public bool IsHazardous { get; set; } // Already exists
    public string Gs1BarcodePrefix { get; set; } // Already exists
    public bool IsActive { get; set; } // Already exists
    public decimal PackageTareWeightPerUom { get; set; }

    public MaterialType MaterialType { get; set; }
    
    public Material() : base(Guid.Empty)
    {
        Name = null!;
        Sku = null!;
        Description = null!;
        Gs1BarcodePrefix = null!;
    }

    public static Material Create(
        string name,
        string sku,
        string description,
        Guid categoryId,
        Guid uomId,
        TempZone requiredTempZone,
        decimal baseWeight,
        decimal costPerUnit,
        MaterialType materialType)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Material name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("Material SKU cannot be empty.", nameof(sku));
        if (categoryId == Guid.Empty) throw new ArgumentException("A material category must be specified.", nameof(categoryId));
        if (uomId == Guid.Empty) throw new ArgumentException("A unit of measure must be specified.", nameof(uomId));
        if (baseWeight < 0) throw new ArgumentException("Base weight cannot be negative.", nameof(baseWeight));
        if (costPerUnit < 0) throw new ArgumentException("Cost per unit cannot be negative.", nameof(costPerUnit));

        return new Material(
            Guid.NewGuid(),
            name,
            sku,
            description,
            categoryId,
            uomId,
            requiredTempZone,
            baseWeight,
            costPerUnit,
            materialType);
    }

    private Material(
        Guid id,
        string name,
        string sku,
        string description,
        Guid categoryId,
        Guid uomId,
        TempZone requiredTempZone,
        decimal baseWeight,
        decimal costPerUnit,
        MaterialType materialType) : base(id)
    {
        Name = name;
        Sku = sku;
        Description = description;
        CategoryId = categoryId;
        UomId = uomId;
        RequiredTempZone = requiredTempZone;
        BaseWeight = baseWeight;
        CostPerUnit = costPerUnit;
        MaterialType = materialType;
        IsActive = true;
        Perishable = false; // Default
        ShelfLifeDays = 0; // Default
        IsHazardous = false; // Default
        DefaultBarcodeFormat = BarcodeFormat.GS1_128; // Default
        Gs1BarcodePrefix = string.Empty; // Default
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        // Consider adding a domain event if needed
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        // Consider adding a domain event if needed
    }

    public void UpdateBasicInfo(string name, string description, decimal costPerUnit, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Material name cannot be empty.", nameof(name));
        if (costPerUnit < 0) throw new ArgumentException("Cost per unit cannot be negative.", nameof(costPerUnit));
        if (categoryId == Guid.Empty) throw new ArgumentException("A material category must be specified.", nameof(categoryId));

        Name = name;
        Description = description;
        CostPerUnit = costPerUnit;
        CategoryId = categoryId;
    }

    public void UpdateLogistics(
        TempZone tempZone,
        decimal baseWeight,
        MaterialType materialType) // MaterialType is generally set at creation, but allow update if needed
    {
        if (baseWeight < 0)
        {
            throw new ArgumentException("Base weight cannot be negative.");
        }
        BaseWeight = baseWeight;
        RequiredTempZone = tempZone;
        MaterialType = materialType;
    }

    public void UpdateDimensions(decimal length, decimal width, decimal height, decimal packageTareWeight)
    {
        if (length < 0 || width < 0 || height < 0 || packageTareWeight < 0)
        {
            throw new ArgumentException("Dimensions and weights cannot be negative.");
        }
        DimensionsLength = length;
        DimensionsWidth = width;
        DimensionsHeight = height;
        PackageTareWeightPerUom = packageTareWeight;
    }

    // This method already covers Perishable, ShelfLifeDays, IsHazardous
    public void UpdateHandlingRules(bool isPerishable, int shelfLifeDays, bool isHazardous)
    {
        if (isPerishable && shelfLifeDays <= 0)
        {
            throw new InvalidOperationException("A perishable material must have a shelf life greater than zero days.");
        }

        Perishable = isPerishable;
        ShelfLifeDays = isPerishable ? shelfLifeDays : 0; // Reset shelf life if not perishable
        IsHazardous = isHazardous;
    }

    // --- ADDED: Method to update GS1 Prefix ---
    public void UpdateGs1Prefix(string prefix)
    {
        // Add any validation for the prefix format if needed
        Gs1BarcodePrefix = prefix ?? string.Empty;
    }
    // --- END ADDED ---

    public void UpdateStockLevels(int min, int max)
    {
        if (min < 0 || max < 0) throw new ArgumentException("Stock levels cannot be negative.");
        if (min > max) throw new InvalidOperationException("Minimum stock level cannot be greater than maximum stock level.");

        MinStockLevel = min;
        MaxStockLevel = max;
    }
}