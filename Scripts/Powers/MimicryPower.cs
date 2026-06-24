using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class MimicryPower : KnowledgeDemonPowerModel
{
    private ModelId? _chosenCharacterId;
    private bool _isArmed;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    [SavedProperty]
    public ModelId ChosenCharacterId
    {
        get => _chosenCharacterId
            ?? throw new System.InvalidOperationException($"{nameof(MimicryPower)} used without a chosen character.");
        set
        {
            AssertMutable();
            _chosenCharacterId = value;
        }
    }

    [SavedProperty]
    public bool IsArmed
    {
        get => _isArmed;
        set
        {
            AssertMutable();
            _isArmed = value;
        }
    }

    public void SetChosenCharacter(CharacterModel character)
    {
        ChosenCharacterId = character.Id;
    }

    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner))
        {
            IsArmed = true;
        }

        return Task.CompletedTask;
    }

    public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)
    {
        if (player != Owner.Player || !IsArmed || options.Source != CardCreationSource.Encounter)
        {
            return options;
        }

        var chosenCharacter = ModelDb.GetById<CharacterModel>(ChosenCharacterId);
        return options
            .WithCardPools([chosenCharacter.CardPool], options.CardPoolFilter)
            .WithFlags(CardCreationFlags.NoCardPoolModifications);
    }
}
