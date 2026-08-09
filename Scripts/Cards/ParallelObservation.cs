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
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class ParallelObservation : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = 1;
    private const CardType TypeValue = CardType.Power;
    private const CardRarity RarityValue = CardRarity.Uncommon;
    private const TargetType TargetTypeValue = TargetType.Self;
    private const bool ShouldShowInCardLibraryValue = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Unique),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Transform)];

    public ParallelObservation()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var candidates = Owner.UnlockState.Characters
            .Where(character => character.IsPlayable && character.Id != Owner.Character.Id)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        var offeredCharacters = candidates
            .StableShuffle(Owner.RunState.Rng.Shuffle)
            .Take(3)
            .ToList();

        var optionCards = offeredCharacters
            .Select(CreateCharacterOptionCard)
            .Where(static card => card is not null)
            .Cast<CardModel>()
            .ToList();
        if (optionCards.Count == 0)
        {
            return;
        }

        var chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, optionCards, Owner, canSkip: false);
        if (chosen is not ParallelObservationCharacterOption chosenOption || chosenOption.ChosenCharacterId is not { } chosenCharacterId)
        {
            return;
        }

        var chosenCharacter = ModelDb.GetById<CharacterModel>(chosenCharacterId);
        ParallelObservationPowerBase? existingPower = Owner.Creature.GetPower<ParallelObservationPower>();
        existingPower ??= Owner.Creature.GetPower<ParallelObservationUpgradedPower>();
        if (existingPower is not null)
        {
            await PowerCmd.Remove(existingPower);
        }

        if (IsUpgraded)
        {
            var upgradedPower = (ParallelObservationUpgradedPower)ModelDb.Power<ParallelObservationUpgradedPower>().ToMutable();
            upgradedPower.SetChosenCharacter(chosenCharacter);
            await PowerCmd.Apply(choiceContext, upgradedPower, Owner.Creature, 1m, Owner.Creature, this);
            return;
        }

        var power = (ParallelObservationPower)ModelDb.Power<ParallelObservationPower>().ToMutable();
        power.SetChosenCharacter(chosenCharacter);
        await PowerCmd.Apply(choiceContext, power, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private CardModel? CreateCharacterOptionCard(CharacterModel character)
    {
        CardModel? previewSource = character.Id == ModelDb.Character<Ironclad>().Id
            ? ModelDb.Card<ShrugItOff>()
            : null;

        previewSource ??= character.StartingDeck.FirstOrDefault(card => card.Tags.Contains(CardTag.Defend))
            ?? character.StartingDeck.FirstOrDefault();
        if (previewSource is null)
        {
            return null;
        }

        var option = Owner.RunState.CreateCard<ParallelObservationCharacterOption>(Owner);
        option.SetChosenCharacter(character, previewSource);
        return option;
    }
}
