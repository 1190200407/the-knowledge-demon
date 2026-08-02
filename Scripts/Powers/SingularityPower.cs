using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class SingularityPower : KnowledgeDemonPowerModel
{
    private const string TargetCardNameVarName = "TargetCard";

    [SavedProperty]
    public ModelId? TargetCardId { get; private set; }

    [SavedProperty]
    public bool TargetIsUpgraded { get; private set; }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new StringVar(TargetCardNameVarName, "???")];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        GetTargetCard() is { } target
            ? [HoverTipFactory.FromCard(target, TargetIsUpgraded)]
            : [];

    public void SetTarget(CardModel target)
    {
        AssertMutable();
        TargetCardId = target.Id;
        TargetIsUpgraded = target.IsUpgraded;
        SyncTargetCardName();
        InvokeDisplayAmountChanged();
    }

    public CardModel? CreateReplacement(CardModel original)
    {
        if (TargetCardId is not { } targetCardId
            || ModelDb.GetById<CardModel>(targetCardId) is not { } targetTemplate)
        {
            return null;
        }

        var replacement = original.CardScope?.CreateCard(targetTemplate, original.Owner)
            ?? original.Owner.RunState.CreateCard(targetTemplate, original.Owner);

        if (TargetIsUpgraded)
        {
            CardCmd.Upgrade(replacement, CardPreviewStyle.None);
        }

        return replacement;
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        _ = cardSource;
        SyncTargetCardName();
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        _ = choiceContext;
        _ = participants;

        if (side == CombatSide.Player)
        {
            await PowerCmd.Remove(this);
        }
    }

    private CardModel? GetTargetCard() =>
        TargetCardId is { } targetCardId
            ? ModelDb.GetByIdOrNull<CardModel>(targetCardId)
            : null;

    private void SyncTargetCardName()
    {
        var target = GetTargetCard();
        if (target is null)
        {
            return;
        }

        ((StringVar)DynamicVars[TargetCardNameVarName]).StringValue =
            TargetIsUpgraded
                ? $"{target.Title}+"
                : target.Title;
    }
}
