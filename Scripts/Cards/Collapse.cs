using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Collapse : KnowledgeDemonCardModel
{
    public override int MaxUpgradeLevel => 0;

    private const int EnergyCostValue = 0;
    private const CardType TypeValue = CardType.Status;
    private const CardRarity RarityValue = CardRarity.Token;
    private const TargetType TargetTypeValue = TargetType.None;
    private const bool ShouldShowInCardLibraryValue = false;

    public override bool CanBeGeneratedInCombat => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Unique),
    ];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<DisintegrationPower>(12m),
    ];

    public Collapse()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState is null)
        {
            return;
        }

        await PowerCmd.Apply<DisintegrationPower>(
            choiceContext,
            CombatState.HittableEnemies,
            DynamicVars["DisintegrationPower"].BaseValue,
            Owner.Creature,
            this);
    }
}
