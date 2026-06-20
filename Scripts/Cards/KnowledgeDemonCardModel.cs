using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Scaffolding.Content;

namespace ComicChess.KnowledgeDemon;

public abstract class KnowledgeDemonCardModel : ModCardTemplate
{
    private const string PlaceholderPortraitPath = "res://KnowledgeDemon/images/card_portraits/card.png";

    public override string PortraitPath => PlaceholderPortraitPath;

    protected KnowledgeDemonCardModel(
        int energyCost,
        CardType type,
        CardRarity rarity,
        TargetType targetType,
        bool shouldShowInCardLibrary)
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }
}
