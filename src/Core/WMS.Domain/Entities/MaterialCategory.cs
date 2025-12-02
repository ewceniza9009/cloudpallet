using System.Text.Json.Serialization;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities;

public class MaterialCategory(Guid id, string name) : Entity<Guid>(id)
{
    [JsonInclude]
    public string Name { get; private set; } = name;
    [JsonInclude]
    public Guid? ParentId { get; private set; }

    public void SetParent(Guid? parentId)
    {
        ParentId = parentId;
    }

    [JsonConstructor]
    private MaterialCategory() : this(Guid.Empty, string.Empty) { }
}