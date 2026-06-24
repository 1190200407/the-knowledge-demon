using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class CognitiveDistortion : KnowledgeDemonCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust,
        ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Record),
    ];

    public CognitiveDistortion()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var candidates = TransformOptionUtility.GetOtherCharacterPoolCards(Owner, Pool)
            .Where(card => card.CanBeGeneratedInCombat)
            .GroupBy(card => card.Id)
            .Select(group => group.First())
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        var offeredCards = candidates
            .StableShuffle(Owner.RunState.Rng.Shuffle)
            .Take(3)
            .Select(card => Owner.RunState.CreateCard(card, Owner))
            .ToList();
        if (offeredCards.Count == 0)
        {
            return;
        }

        var chosen = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            offeredCards,
            Owner,
            canSkip: false);
        if (chosen is null)
        {
            return;
        }

        await BookLibraryCmd.RecordToLibrary(choiceContext, Owner, chosen, 1);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
