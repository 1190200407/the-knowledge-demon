using STS2RitsuLib.Content;

namespace ComicChess.KnowledgeDemon;

public static class KnowledgeDemonKeyword
{
    public const string LibraryIconPath = "res://KnowledgeDemon/images/charui/energy_icon.png";

    public static readonly string Library =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, "library");

    public static readonly string Materialize =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, "materialize");

    public static readonly string Record =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, "record");

    public static readonly string Choose =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, "choose");
    public static readonly string ChooseOmega =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, "choose_omega");

    public static readonly string KnowledgeOverload =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, "knowledge_overload");
    public static readonly string KnowledgeOverloadOmega =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, "knowledge_overload_omega");

    public static readonly string Unique =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, "unique");

    public static readonly string Infinite =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, "infinite");
}
