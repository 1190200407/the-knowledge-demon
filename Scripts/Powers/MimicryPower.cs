using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class MimicryPower : KnowledgeDemonPowerModel
{
    private ModelId? _chosenCharacterId;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("ChosenCharacter")];

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

    public void SetChosenCharacter(CharacterModel character)
    {
        ChosenCharacterId = character.Id;
        SyncChosenCharacterVar();
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SyncChosenCharacterVar();
        return Task.CompletedTask;
    }

    private void SyncChosenCharacterVar()
    {
        if (_chosenCharacterId is null)
        {
            return;
        }

        var chosenCharacter = ModelDb.GetById<CharacterModel>(ChosenCharacterId);
        ((StringVar)DynamicVars["ChosenCharacter"]).StringValue = chosenCharacter.Title.GetFormattedText();
    }
}
