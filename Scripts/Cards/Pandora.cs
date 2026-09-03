using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Pandora : KnowledgeDemonCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    private decimal _extraDamage;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(3m, ValueProp.Move),
        new RepeatVar(2),
    ];

    private decimal ExtraDamage
    {
        get => _extraDamage;
        set
        {
            AssertMutable();
            _extraDamage = value;
        }
    }

    public Pandora()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Pile is not { } listenerPile || !BookLibraryUtility.IsBookLibraryPile(listenerPile.Type))
        {
            return;
        }

        var played = cardPlay.Card;
        if (played.Owner != Owner || played.Type != CardType.Attack)
        {
            return;
        }

        var damage = GetAttackCardDamage(played, Owner, cardPlay.Target, cardPlay);
        DynamicVars.Damage.BaseValue += damage;
        ExtraDamage += damage;
        BookLibraryUtility.RefreshCardVisual(this);

        await CardCmd.Exhaust(choiceContext, played);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await KnowledgeDemon.WithKnowledgeDemonAttackAnim(
            DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(DynamicVars.Repeat.IntValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target),
            Owner.Character,
            onlyPlayAnimOnce: true)
            .WithHitFx("vfx/vfx_thrash")
            .Execute(choiceContext);
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue += ExtraDamage;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(1m);
    }

    private static decimal GetAttackCardDamage(
        CardModel attackCard,
        Player owner,
        Creature? target,
        CardPlay cardPlay)
    {
        decimal damage = default;
        if (attackCard.DynamicVars.ContainsKey("CalculatedDamage"))
        {
            damage = attackCard.DynamicVars.CalculatedDamage.Calculate(target);
        }
        else if (attackCard.DynamicVars.ContainsKey("Damage"))
        {
            damage = attackCard.DynamicVars.Damage.BaseValue;
        }
        else if (attackCard.DynamicVars.ContainsKey("OstyDamage"))
        {
            damage = attackCard.DynamicVars.OstyDamage.BaseValue;
        }
        else
        {
            Log.Warn(
                $"{nameof(Pandora)} exhausted attack {attackCard.Id.Entry} without a damage var.");
            return 0m;
        }

        return Hook.ModifyDamage(
            owner.RunState,
            owner.Creature.CombatState,
            null,
            owner.Creature,
            damage,
            ValueProp.Move,
            attackCard,
            cardPlay,
            ModifyDamageHookType.All,
            CardPreviewMode.None,
            out _);
    }
}
