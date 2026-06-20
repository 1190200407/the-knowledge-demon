using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class HiveConsciousness : KnowledgeDemonCardModel, IKnowledgeDemonEventListener
{
    private const int energyCost = 0;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Sly];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    public HiveConsciousness()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public Task<CardModel> ModifyRecordCardLate(Player player, CardModel sourceCard, CardModel recordTemplate)
    {
        if (Pile is not { } listenerPile || !BookLibraryUtility.IsBookLibraryPile(listenerPile.Type))
        {
            return Task.FromResult(recordTemplate);
        }

        if (sourceCard.CardScope is null)
        {
            return Task.FromResult(recordTemplate);
        }

        if (recordTemplate is HiveConsciousness)
        {
            return Task.FromResult(recordTemplate);
        }

        var libraryPile = BookLibraryUtility.PileType.GetPile(player);
        if (libraryPile is null || !libraryPile.Cards.Any(static c => c is HiveConsciousness))
        {
            return Task.FromResult(recordTemplate);
        }

        var hive = sourceCard.CardScope.CreateCard<HiveConsciousness>(player);
        if (recordTemplate.IsUpgraded && !hive.IsUpgraded)
        {
            CardCmd.Upgrade(hive, CardPreviewStyle.None);
        }

        return Task.FromResult((CardModel)hive);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CreatureCmd.TriggerAnim(Owner.Creature, "Attack", Owner.Character.AttackAnimDelay);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
