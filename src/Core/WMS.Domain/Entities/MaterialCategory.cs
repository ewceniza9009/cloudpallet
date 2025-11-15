using WMS.Domain.Shared;

namespace WMS.Domain.Entities;

public class MaterialCategory(Guid id, string name) : Entity<Guid>(id)
{
    public string Name { get; private set; } = name;
    public Guid? ParentId { get; private set; }

    public void SetParent(Guid? parentId)
    {
        ParentId = parentId;
    }

    private MaterialCategory() : this(Guid.Empty, string.Empty) { }
}