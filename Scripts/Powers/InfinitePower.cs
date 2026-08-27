using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class InfinitePower : KnowledgeDemonPowerModel
{
    private const string InfiniteCardsVarName = "InfiniteCards";

    private readonly HashSet<string> _infiniteCardIds = [];

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [KnowledgeDemonKeywordHoverTips.FromInfinite()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new StringVar(InfiniteCardsVarName, "无")];

    public void AddInfiniteTarget(CardModel card)
    {
        if (_infiniteCardIds.Add(card.Id.Entry))
        {
            ApplyInfiniteToCurrentCards();
            SyncInfiniteCardsVar();
            Flash();
        }
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        _ = cardSource;

        ApplyInfiniteToCurrentCards();
        SyncInfiniteCardsVar();
        return Task.CompletedTask;
    }

    public override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        _ = oldPileType;
        _ = clonedBy;

        if (Owner.Player is not { } player || card.Owner != player)
        {
            return Task.CompletedTask;
        }

        if (TryApplyInfiniteKeyword(card))
        {
            SyncInfiniteCardsVar();
        }
        else if (card.HasModKeyword(ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Infinite))
                 || card is Infinite)
        {
            SyncInfiniteCardsVar();
        }

        return Task.CompletedTask;
    }

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (Owner.Player is not { } player
            || creator != player
            || card.Owner != player)
        {
            return Task.CompletedTask;
        }

        if (TryApplyInfiniteKeyword(card))
        {
            SyncInfiniteCardsVar();
        }

        return Task.CompletedTask;
    }

    private void ApplyInfiniteToCurrentCards()
    {
        if (Owner.Player is not { } player || player.PlayerCombatState is null)
        {
            return;
        }

        foreach (var card in player.PlayerCombatState.AllCards)
        {
            if (card.Owner != player || card is Infinite)
            {
                continue;
            }

            if (_infiniteCardIds.Contains(card.Id.Entry))
            {
                TryApplyInfiniteKeyword(card);
            }
        }
    }

    private bool TryApplyInfiniteKeyword(CardModel card)
    {
        if (card is Infinite)
        {
            return false;
        }

        var infiniteKeyword = ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Infinite);
        if (card.HasModKeyword(infiniteKeyword))
        {
            return false;
        }

        if (!_infiniteCardIds.Contains(card.Id.Entry))
        {
            return false;
        }

        card.AddKeyword(infiniteKeyword);
        return true;
    }

    private void SyncInfiniteCardsVar()
    {
        if (Owner.Player is not { } player || player.PlayerCombatState is null)
        {
            return;
        }

        var names = player.PlayerCombatState.AllCards
            .Where(card => card.Owner == player && card.HasModKeyword(ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Infinite)) && card is not Infinite)
            .GroupBy(card => card.Id.Entry)
            .Select(group => group.First().Title)
            .ToList();

        ((StringVar)DynamicVars[InfiniteCardsVarName]).StringValue =
            names.Count == 0 ? "无" : string.Join("、", names.Select(name => $"[gold]{name}[/gold]"));
    }
}
