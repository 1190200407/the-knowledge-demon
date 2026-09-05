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
    internal const int InfiniteThreshold = 3;
    private const string TargetCardNameVarName = "TargetCard";

    [SavedProperty]
    public ModelId? TargetCardId { get; private set; }

    [SavedProperty]
    public bool TargetIsUpgraded { get; private set; }

    [SavedProperty]
    public int TransformCount { get; private set; }

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
        TransformCount = 0;
        SyncTargetCardName();
        InvokeDisplayAmountChanged();
    }

    internal TransformReplacement? CreateReplacement(CardModel original, bool countTransform)
    {
        if (TransformOptionUtility.IsInfinite(original))
        {
            return null;
        }

        if (TargetCardId is not { } targetCardId
            || ModelDb.GetById<CardModel>(targetCardId) is not { } targetTemplate)
        {
            return null;
        }

        if (countTransform)
        {
            TransformCount++;
        }

        var useInfinite = TransformCount >= InfiniteThreshold;
        var template = targetTemplate;
        if (useInfinite)
        {
            if (TransformOptionUtility.GetInfiniteCard() is { } infiniteTemplate)
            {
                template = infiniteTemplate;
            }
            else
            {
                useInfinite = false;
            }
        }

        var replacement = original.CardScope?.CreateCard(template, original.Owner)
            ?? original.Owner.RunState.CreateCard(template, original.Owner);

        if (useInfinite)
        {
            return new TransformReplacement(replacement, TransformCount, true);
        }

        if (TargetIsUpgraded)
        {
            CardCmd.Upgrade(replacement, CardPreviewStyle.None);
        }

        return new TransformReplacement(replacement, TransformCount, false);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;

        if (Owner.Player != cardPlay.Card.Owner)
        {
            return Task.CompletedTask;
        }

        TransformCount = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
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

    internal readonly record struct TransformReplacement(
        CardModel Replacement,
        int TransformCount,
        bool BecameInfinite);
}
