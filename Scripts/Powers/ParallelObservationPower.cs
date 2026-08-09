using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

public abstract class ParallelObservationPowerBase : KnowledgeDemonPowerModel
{
    private const string ChosenCharacterVarName = "ChosenCharacter";

    [SavedProperty]
    public ModelId? ChosenCharacterId { get; private set; }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => ParallelObservationPowerShared.Title;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new StringVar(ChosenCharacterVarName, "???")];

    public void SetChosenCharacter(CharacterModel character)
    {
        AssertMutable();
        ChosenCharacterId = character.Id;
        SyncChosenCharacterVar();
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        _ = cardSource;
        SyncChosenCharacterVar();
        return Task.CompletedTask;
    }

    private void SyncChosenCharacterVar()
    {
        if (ChosenCharacterId is not { } chosenCharacterId)
        {
            return;
        }

        ((StringVar)DynamicVars[ChosenCharacterVarName]).StringValue =
            ModelDb.GetById<CharacterModel>(chosenCharacterId).Title.GetFormattedText();
    }
}

[RegisterPower]
public sealed class ParallelObservationPower : ParallelObservationPowerBase;

[RegisterPower]
public sealed class ParallelObservationUpgradedPower : ParallelObservationPowerBase;

internal static class ParallelObservationPowerShared
{
    internal static readonly LocString Title =
        new("powers", "KNOWLEDGE_DEMON_POWER_PARALLEL_OBSERVATION_POWER.title");
}
