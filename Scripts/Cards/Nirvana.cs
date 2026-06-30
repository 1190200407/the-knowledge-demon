using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Nirvana : KnowledgeDemonCardModel, IKnowledgeDemonEventListener
{
    private const int energyCost = 6;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(36m, ValueProp.Move)];

    public Nirvana()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (!ReferenceEquals(card, this) || IsClone)
        {
            return Task.CompletedTask;
        }

        var chooseCount = CountChooseHistory();
        if (chooseCount <= 0)
        {
            return Task.CompletedTask;
        }

        EnergyCost.AddThisCombat(-chooseCount);
        InvokeEnergyCostChanged();
        return Task.CompletedTask;
    }

    public Task AfterChooseFromLibrary(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel? chooseSource,
        CardModel? chosen,
        IReadOnlyList<CardModel> candidates)
    {
        _ = choiceContext;
        _ = chooseSource;
        _ = chosen;
        _ = candidates;

        if (player != Owner)
        {
            return Task.CompletedTask;
        }

        EnergyCost.AddThisCombat(-1);
        InvokeEnergyCostChanged();
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .WithAttackerAnim(
                KnowledgeDemon.GetSuperAnimIfApplicable(Owner.Character),
                KnowledgeDemon.GetSuperAttackDelayIfApplicable(Owner.Character))
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(9m);
    }

    private int CountChooseHistory()
    {
        if (Owner is null)
        {
            return 0;
        }

        return CombatManager.Instance.History.CardPlaysFinished.Count(playFinished =>
            playFinished.CardPlay.Card.Owner == Owner
            && playFinished.CardPlay.Card is TouchGold or Practice or Embodiment or MakeAChoice or ItIsDone);
    }
}
