namespace backend.Models;

public static class StoryDocumentKinds
{
    public const string Manuscript = "manuscript";

    public const string Character = BibleDocumentCategories.Character;

    public const string Location = BibleDocumentCategories.Location;

    public static readonly HashSet<string> ReferenceKinds = [Character, Location];

    public static readonly HashSet<string> All = [Manuscript, Character, Location];
}
