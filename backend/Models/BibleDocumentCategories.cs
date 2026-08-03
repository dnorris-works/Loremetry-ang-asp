namespace backend.Models;

public static class BibleDocumentCategories
{
    public const string Character = "character";

    public const string Location = "location";

    public static readonly HashSet<string> All = [Character, Location];
}
