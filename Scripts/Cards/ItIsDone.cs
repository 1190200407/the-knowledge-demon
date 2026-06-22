using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class ItIsDone : KnowledgeDemonCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    private const int MaxLibrarySelectCount = 3;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Choose)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [KnowledgeDemonKeywordHoverTips.FromChoose()];

    public ItIsDone()
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

        var maxSelect = Math.Min(MaxLibrarySelectCount, libraryPile.Cards.Count);
        var selected = (await KnowledgeDemonCardSelectCmd.FromBookLibrary(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, maxSelect),
            filter: null,
            source: this)).ToList();

        if (selected.Count == 0)
        {
            return;
        }

        await BookLibraryCmd.PlayChooseStartPresentation(Owner);
        await BookLibraryCmd.ChooseFromCandidatesAndAutoPlay(choiceContext, Owner, selected, this);
        BookLibraryCmd.PlayChooseDonePresentation(Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
