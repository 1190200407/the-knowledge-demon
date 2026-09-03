using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class LocustIncursion : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = 1;
    private const CardType TypeValue = CardType.Attack;
    private const CardRarity RarityValue = CardRarity.Rare;
    private const TargetType TargetTypeValue = TargetType.AllEnemies;
    private const bool ShouldShowInCardLibraryValue = true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Transform)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8m, ValueProp.Move)];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Sly];

    protected override bool ShouldGlowGoldInternal => ShouldRecordAgainThisPlay();

    public LocustIncursion()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await KnowledgeDemon.WithKnowledgeDemonAttackAnim(
            DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(CombatState!),
            Owner.Character)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);

        if (ShouldRecordAgainThisPlay())
        {
            await BookLibraryCmd.RecordToLibrary(choiceContext, Owner, this, 1);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }

    private bool ShouldRecordAgainThisPlay()
    {
        if (Owner?.Creature.CombatState is not { } combatState)
        {
            return false;
        }

        var threshold = IsUpgraded ? 5 : 3;
        return CountPlayedCardsThisTurn(combatState) < threshold;
    }

    private int CountPlayedCardsThisTurn(ICombatState combatState)
    {
        var history = CombatManager.Instance?.History;
        if (history is null)
        {
            return 0;
        }

        return history.CardPlaysFinished.Count(entry =>
            entry.HappenedThisTurn(combatState)
            && entry.CardPlay.Card.Owner == Owner);
    }
}
