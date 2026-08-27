using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Infinite : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = 0;
    private const CardType TypeValue = CardType.Status;
    private const CardRarity RarityValue = CardRarity.Ancient;
    private const TargetType TargetTypeValue = TargetType.Self;
    private const bool ShouldShowInCardLibraryValue = false;

    public override bool CanBeGeneratedInCombat => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust,
        ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Infinite),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        KnowledgeDemonKeywordHoverTips.FromInfinite(),
        KnowledgeDemonKeywordHoverTips.FromUnique(),
    ];

    public Infinite()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        var candidates = KnowledgeDemonUniqueUtility.IterateCombatUniqueScope(Owner)
            .Where(KnowledgeDemonUniqueUtility.IsUnique)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        var selected = (await CardSelectCmd.FromSimpleGridForRewards(
            choiceContext,
            candidates.Select(card => new CardCreationResult(card)).ToList(),
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        if (selected is null)
        {
            return;
        }

        var power = Owner.Creature.GetPower<InfinitePower>();
        if (power is null)
        {
            power = (InfinitePower)ModelDb.Power<InfinitePower>().ToMutable();
            await PowerCmd.Apply(choiceContext, power, Owner.Creature, 1m, Owner.Creature, this);
            power = Owner.Creature.GetPower<InfinitePower>() ?? power;
        }

        power.AddInfiniteTarget(selected);
    }
}
