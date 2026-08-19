using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterPotion(typeof(KnowledgeDemonPotionPool))]
public sealed class StatusPotion : KnowledgeDemonPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        _ = target;
        if (Owner is null || Owner.Creature is null || Owner.Creature.CombatState is null)
            return;

        var statusPoolCards = TransformOptionUtility.GetKnowledgeDemonStatusPoolCards(Owner)
            .DistinctBy(card => card.Id)
            .OrderBy(_ => Owner.RunState.Rng.CombatCardGeneration.NextFloat())
            .Take(3)
            .ToList();
        var candidates = new List<CardModel>();
        for (int i = 0; i < statusPoolCards.Count; i++)
            candidates.Add(Owner.Creature.CombatState.CreateCard(statusPoolCards[i], Owner));

        if (candidates.Count == 0)
        {
            candidates.Add(Owner.RunState.CreateCard<Infinite>(Owner));
        }

        var chosen = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            candidates,
            Owner,
            canSkip: false);
        if (chosen is null)
            return;

        await CardPileCmd.AddGeneratedCardToCombat(chosen, PileType.Hand, Owner);
        chosen.RemoveKeyword(CardKeyword.Unplayable);
        BookLibraryUtility.RefreshHandCardVisual(chosen);
    }
}
