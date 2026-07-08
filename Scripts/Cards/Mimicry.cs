using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Mimicry : KnowledgeDemonCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public Mimicry()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
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

        var chosen = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            optionCards,
            Owner,
            canSkip: false);
        if (chosen is not MimicryCharacterOption chosenOption || chosenOption.ChosenCharacterId is not { } chosenCharacterId)
        {
            return;
        }

        var chosenCharacter = ModelDb.GetById<CharacterModel>(chosenCharacterId);

        MimicryRewardSingleton.SetChosenCharacter(Owner, chosenCharacter, IsUpgraded);
        await PowerCmd.Remove(Owner.Creature.GetPower<MimicryPower>());

        var power = (MimicryPower)ModelDb.Power<MimicryPower>().ToMutable();
        power.SetChosenCharacter(chosenCharacter, IsUpgraded);
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

        var option = Owner.RunState.CreateCard<MimicryCharacterOption>(Owner);
        option.SetChosenCharacter(character, previewSource);
        if (base.IsUpgraded)
            CardCmd.Upgrade(option);
        return option;
    }
}
