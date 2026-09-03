using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Slap : KnowledgeDemonCardModel
{
    private const int energyCost = 2;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [KnowledgeDemonKeywordHoverTips.FromRecord()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        ModCardVars.Computed("Repeat", 1m, card => GetHitCount(card)),
    ];

    public Slap()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await KnowledgeDemon.WithKnowledgeDemonAttackAnim(
            DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount((int)GetHitCount(this))
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target),
            Owner.Character,
            onlyPlayAnimOnce: true)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    private static decimal GetHitCount(CardModel? source)
    {
        if (source is not { IsMutable: true })
        {
            return 1;
        }

        if (source is not Slap slap)
        {
            return 1;
        }

        var owner = slap.Owner;

        var history = CombatManager.Instance?.History;
        if (history is null)
        {
            return 1;
        }

        return 1 + history.Entries
            .OfType<KnowledgeDemonRecordedEntry>()
            .Where(entry => entry.Player == owner && entry.RecordedCardId == slap.Id)
            .Sum(entry => entry.Count);
    }
}
