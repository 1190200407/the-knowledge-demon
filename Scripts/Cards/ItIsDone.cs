using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class ItIsDone : KnowledgeDemonCardModel
{
    private const int energyCost = 0;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    private const int ChooseCount = 3;

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

        await BookLibraryCmd.PlayChooseStartPresentation(Owner);

        for (var i = 0; i < ChooseCount; i++)
        {
            if (CombatManager.Instance.IsOverOrEnding || libraryPile.Cards.Count == 0)
            {
                break;
            }

            var result = await BookLibraryCmd.ChooseFromLibrary(choiceContext, Owner, this);
            if (!result.HasCandidates)
            {
                break;
            }

            await ApplyItIsDoneChooseResult(choiceContext, Owner, result);
        }

        BookLibraryCmd.PlayChooseDonePresentation(Owner);
    }

    private static async Task ApplyItIsDoneChooseResult(
        PlayerChoiceContext choiceContext,
        Player player,
        BookLibraryChooseResult result)
    {
        if (result.Chosen != null)
        {
            KnowledgeDemonChooseContext.ClearChoosePreviewFlag(result.Chosen);
            await BookLibraryCmd.PlayChosenCard(choiceContext, player, result.Chosen, null);
        }

        foreach (var card in result.Unchosen)
        {
            KnowledgeDemonChooseContext.ClearChoosePreviewFlag(card);
            await CardPileCmd.Add(card, BookLibraryUtility.PileType);
            await Cmd.CustomScaledWait(0.5f, 1f);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
