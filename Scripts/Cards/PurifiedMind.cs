using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class PurifiedMind : KnowledgeDemonCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Unique),
    ];

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, ValueProp.Move)];

    public PurifiedMind()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = (await KnowledgeDemonCardSelectCmd.FromBookLibraryAndHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            null,
            this)).FirstOrDefault();

        if (selected is null)
        {
            return;
        }

        var combatState = Owner.PlayerCombatState;
        if (combatState is null)
        {
            return;
        }

        var sameNameCards = combatState.AllCards
            .Where(c =>
                c.Owner == Owner
                && c.Id == selected.Id
                && c.Pile is { } pile
                && pile.Type != PileType.Exhaust)
            .ToList();

        foreach (var card in sameNameCards)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        if (sameNameCards.Count > 0)
        {
            for (var i = 0; i < sameNameCards.Count; i++)
            {   
                await CreatureCmd.GainBlock(
                        Owner.Creature,
                        DynamicVars.Block,
                        cardPlay);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
