namespace backend.Models;

public class CollectionField
{
    public Guid Id { get; set; }

    public Guid CollectionId { get; set; }

    public Collection Collection { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string FieldKey { get; set; } = string.Empty;

    public FieldType FieldType { get; set; }

    public bool IsRequired { get; set; }

    public int DisplayOrder { get; set; }
}
