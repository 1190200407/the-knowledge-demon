using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class MimicryPower : KnowledgeDemonPowerModel
{
    private ModelId? _chosenCharacterId;
    private bool _grantsUpgradedReward;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("ChosenCharacter"), new StringVar("UpgradeText")];

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
        SetChosenCharacter(character, false);
    }

    public void SetChosenCharacter(CharacterModel character, bool grantsUpgradedReward)
    {
        ChosenCharacterId = character.Id;
        GrantsUpgradedReward = grantsUpgradedReward;
        SyncChosenCharacterVar();
    }

    public bool GrantsUpgradedReward
    {
        get => _grantsUpgradedReward;
        set
        {
            AssertMutable();
            _grantsUpgradedReward = value;
        }
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SyncChosenCharacterVar();
        return Task.CompletedTask;
    }

    private void SyncChosenCharacterVar()
    {
        if (_chosenCharacterId is null && Owner?.Player is { } player)
        {
            if (MimicryRewardSingleton.GetChosenCharacter(player) is { } savedCharacter)
            {
                _chosenCharacterId = savedCharacter.Id;
                _grantsUpgradedReward = MimicryRewardSingleton.GrantsUpgradedReward(player);
            }
        }

        if (_chosenCharacterId is null)
        {
            return;
        }

        var chosenCharacter = ModelDb.GetById<CharacterModel>(ChosenCharacterId);
        ((StringVar)DynamicVars["ChosenCharacter"]).StringValue = chosenCharacter.Title.GetFormattedText();
        ((StringVar)DynamicVars["UpgradeText"]).StringValue = GrantsUpgradedReward ? "升级过的" : string.Empty;
    }
}
