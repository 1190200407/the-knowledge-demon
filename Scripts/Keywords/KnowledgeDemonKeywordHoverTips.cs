using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

public static class KnowledgeDemonKeywordHoverTips
{
    public static IHoverTip FromMaterialize(DynamicVarSet dynamicVars) =>
        Create(KnowledgeDemonKeyword.Materialize, dynamicVars[MaterializeVar.DefaultName]);

    public static IHoverTip FromMaterializeKeyword() =>
        CreateStatic(KnowledgeDemonKeyword.Materialize);

    public static IHoverTip FromRecord() =>
        HoverTipFactory.FromKeyword(ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Record));

    public static IHoverTip FromChoose() =>
        CreateStatic(KnowledgeDemonKeyword.Choose);

    public static IHoverTip FromKnowledgeOverload() =>
        CreateStatic(KnowledgeDemonKeyword.KnowledgeOverload);

    private static IHoverTip Create(string keywordId, DynamicVar amountVar)
    {
        var definition = ModKeywordRegistry.Get(keywordId);
        var title = new LocString(definition.TitleTable, definition.TitleKey);
        var description = new LocString(definition.DescriptionTable, definition.DescriptionKey.Replace(".description", ".smartDescription"));
        title.Add(amountVar);
        description.Add(amountVar);
        return new HoverTip(title, description);
    }

    private static IHoverTip CreateStatic(string keywordId)
    {
        var definition = ModKeywordRegistry.Get(keywordId);
        var title = new LocString(definition.TitleTable, definition.TitleKey);
        var description = new LocString(definition.DescriptionTable, definition.DescriptionKey);
        return new HoverTip(title, description);
    }
}
