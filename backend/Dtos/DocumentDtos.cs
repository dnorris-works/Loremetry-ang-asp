namespace backend.Dtos;

public record UpdateDocumentTextRequest
{
    public string TextContent { get; init; } = "";
}
