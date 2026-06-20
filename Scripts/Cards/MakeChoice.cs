using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCharacterStarterCard(typeof(KnowledgeDemon), 1)]
[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class MakeAChoice : KnowledgeDemonCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Basic;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Choose)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [KnowledgeDemonKeywordHoverTips.FromChoose()];

    public MakeAChoice()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var libraryPile = BookLibraryUtility.TryGetLibraryPile(Owner);
        if (libraryPile is null || libraryPile.Cards.Count == 0)
        {
            return;
        }

        await BookLibraryCmd.PlayChooseStartPresentation(Owner);
        await BookLibraryCmd.ChooseFromLibraryAndAutoPlay(choiceContext, Owner, this);
        BookLibraryCmd.PlayChooseDonePresentation(Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
