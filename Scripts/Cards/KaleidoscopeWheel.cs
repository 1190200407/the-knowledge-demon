using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class KaleidoscopeWheel : KnowledgeDemonCardModel
{
    private const int energyCost = 2;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AllEnemies;
    private const bool shouldShowInCardLibrary = true;

    private HashSet<ModelId> _seen = new();

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(17m, ValueProp.Move)];

    protected override bool IsPlayable => HandAllCardsHaveDistinctIds(Owner);

    protected override bool ShouldGlowGoldInternal => IsPlayable;

    public KaleidoscopeWheel()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    /// <summary>手牌中任意两张是否同名（与独一相同：<see cref="SharesUniqueName" /> / <see cref="ModelId" />）。</summary>
    public bool HandAllCardsHaveDistinctIds(Player player)
    {
        _seen.Clear();
        foreach (var card in PileType.Hand.GetPile(player).Cards)
        {
            if (!_seen.Add(card.Id))
            {
                return false;
            }
        }

        return true;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await KnowledgeDemon.WithKnowledgeDemonAttackAnim(
            DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .TargetingAllOpponents(CombatState!),
            Owner.Character)
            .WithHitFx("vfx/vfx_attack_blunt")
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8m);
    }
}
