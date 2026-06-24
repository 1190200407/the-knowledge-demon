using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace ComicChess.KnowledgeDemon;

[RegisterSingleton]
public sealed class MimicryRewardSingleton : HookedSingletonModel
{
    private static readonly Dictionary<ulong, ModelId> ChosenCharacterIds = [];

    public MimicryRewardSingleton()
        : base(HookType.Run)
    {
    }

    public static void SetChosenCharacter(Player player, CharacterModel character)
    {
        ChosenCharacterIds[player.NetId] = character.Id;
    }

    public static void Clear(Player player)
    {
        ChosenCharacterIds.Remove(player.NetId);
    }

    public static CharacterModel? GetChosenCharacter(Player player)
    {
        return ChosenCharacterIds.TryGetValue(player.NetId, out var id)
            ? ModelDb.GetById<CharacterModel>(id)
            : null;
    }

    public override Task BeforeCombatStart()
    {
        if (CurrentRunState is not RunState runState)
        {
            return Task.CompletedTask;
        }

        foreach (var player in runState.Players)
        {
            Clear(player);
        }

        return Task.CompletedTask;
    }

    public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)
    {
        if (options.Source != CardCreationSource.Encounter
            || options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications))
        {
            return options;
        }

        var chosenCharacter = GetChosenCharacter(player);
        if (chosenCharacter is null)
        {
            return options;
        }

        return options
            .WithCardPools([chosenCharacter.CardPool], options.CardPoolFilter)
            .WithFlags(CardCreationFlags.NoCardPoolModifications);
    }
}
