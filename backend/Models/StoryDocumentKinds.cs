namespace backend.Models;

public static class StoryDocumentKinds
{
    public const string Manuscript = "manuscript";

    public const string Bible = "bible";

    public static readonly HashSet<string> All = [Manuscript, Bible];
}
