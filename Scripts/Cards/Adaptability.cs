using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Adaptability : KnowledgeDemonCardModel, IKnowledgeDemonEventListener
{
    private const int EnergyCostValue = 1;
    private const CardType TypeValue = CardType.Skill;
    private const CardRarity RarityValue = CardRarity.Common;
    private const TargetType TargetTypeValue = TargetType.Self;
    private const bool ShouldShowInCardLibraryValue = true;

    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.SingleplayerOnly;

    private static readonly LocString DiscardSelectionPrompt =
        new("cards", "KNOWLEDGE_DEMON_CARD_ADAPTABILITY.discardSelectionScreenPrompt");

    private static readonly LocString RecordSelectionPrompt =
        new("cards", "KNOWLEDGE_DEMON_CARD_ADAPTABILITY.recordSelectionScreenPrompt");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [KnowledgeDemonKeywordHoverTips.FromRecord()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
    ];

    public Adaptability()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    public async Task AfterRecordedToLibrary(
        PlayerChoiceContext? choiceContext,
        Player player,
        CardModel sourceCard,
        IReadOnlyList<CardModel> recordedCopies)
    {
        if (!ReferenceEquals(sourceCard, this) || recordedCopies.Count == 0 || choiceContext is null)
        {
            return;
        }

        await Cmd.CustomScaledWait(0.5f, 1f);
        var selected = (await CardSelectCmd.FromHand(
            choiceContext,
            player,
            new CardSelectorPrefs(DiscardSelectionPrompt, 0, 1),
            static card => card is not Adaptability,
            this)).ToList();

        if (selected.Count == 0)
        {
            return;
        }

        await CardCmd.Discard(choiceContext, selected);
    }

    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (!ReferenceEquals(card, this))
        {
            return;
        }

        await Cmd.CustomScaledWait(0.5f, 1f);
        var selection = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(RecordSelectionPrompt, 1),
            null,
            this)).FirstOrDefault();

        if (selection is null)
        {
            return;
        }

        await BookLibraryCmd.RecordToLibrary(choiceContext, Owner, selection, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
