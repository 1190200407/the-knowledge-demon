using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class SalliSalli : KnowledgeDemonCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    private static bool HasDuplicateCardIdInHand(Player player)
    {
        var seen = new HashSet<ModelId>();
        foreach (var card in PileType.Hand.GetPile(player).Cards)
        {
            if (!seen.Add(card.Id))
            {
                return true;
            }
        }

        return false;
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Unique),
        CardKeyword.Exhaust
    ];

    protected override bool ShouldGlowRedInternal =>
        Owner is not null && HasDuplicateCardIdInHand(Owner);

    public SalliSalli()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner);

        while (hand.Cards.Count < CardPile.MaxCardsInHand)
        {
            if (HasDuplicateCardIdInHand(Owner))
            {
                break;
            }

            var drawn = await CardPileCmd.Draw(choiceContext, 1, Owner);
            if (!drawn.Any())
            {
                break;
            }

            if (HasDuplicateCardIdInHand(Owner))
            {
                break;
            }
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
