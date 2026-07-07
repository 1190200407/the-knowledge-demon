using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;
using STS2RitsuLib.RunData;

namespace ComicChess.KnowledgeDemon;

[RegisterSingleton]
public sealed class MimicryRewardSingleton : HookedSingletonModel
{
    private static readonly PlayerRunSavedData<MimicryRewardData> SavedData =
        RitsuLibFramework.GetRunSavedDataStore(Entry.ModId).RegisterPerPlayer(
            "mimicry_reward",
            () => new MimicryRewardData(),
            new RunSavedDataOptions { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault });

    public sealed class MimicryRewardData
    {
        public string? CharacterId { get; set; }
        public bool GrantsUpgradedReward { get; set; }
    }

    public MimicryRewardSingleton()
        : base(HookType.Run)
    {
    }

    public static void SetChosenCharacter(Player player, CharacterModel character, bool grantsUpgradedReward)
    {
        if (!TryGetRunState(player, out var runState))
        {
            return;
        }

        SavedData.Set(runState, player.NetId, new MimicryRewardData
        {
            CharacterId = character.Id.ToString(),
            GrantsUpgradedReward = grantsUpgradedReward,
        });
    }

    public static void Clear(Player player)
    {
        if (TryGetRunState(player, out var runState))
        {
            SavedData.Remove(runState, player.NetId);
        }
    }

    public static CharacterModel? GetChosenCharacter(Player player)
    {
        return TryGetData(player, out var data)
            ? ModelDb.GetByIdOrNull<CharacterModel>(ModelId.Deserialize(data.CharacterId!))
            : null;
    }

    public static bool GrantsUpgradedReward(Player player)
    {
        return TryGetData(player, out var data) && data.GrantsUpgradedReward;
    }

    private static bool TryGetData(Player player, out MimicryRewardData data)
    {
        if (!TryGetRunState(player, out var runState)
            || !SavedData.TryGet(runState, player.NetId, out data))
        {
            data = null!;
            return false;
        }

        return !string.IsNullOrWhiteSpace(data.CharacterId);
    }

    private static bool TryGetRunState(Player player, out RunState runState)
    {
        if (player.RunState is RunState concreteRunState)
        {
            runState = concreteRunState;
            return true;
        }

        runState = null!;
        Entry.Logger.Warn("[Mimicry] Player does not belong to a concrete RunState.");
        return false;
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

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> options, CardCreationOptions creationOptions)
    {
        if (creationOptions.Source != CardCreationSource.Encounter)
        {
            return false;
        }

        var chosenCharacter = GetChosenCharacter(player);
        if (chosenCharacter is null)
        {
            return false;
        }

        var pool = chosenCharacter.CardPool.GetUnlockedCards(
            player.UnlockState,
            player.RunState.CardMultiplayerConstraint);

        IEnumerable<CardModel> pickFrom = pool
            .Where(static card => card.CanBeGeneratedInCombat)
            .Where(static card => card.Rarity != CardRarity.Basic)
            .Where(static card => card.Rarity != CardRarity.Ancient)
            .Where(card => options.TrueForAll(option => option.originalCard.Id != card.Id));

        if (!pickFrom.Any())
        {
            pickFrom = pool
                .Where(static card => card.CanBeGeneratedInCombat)
                .Where(static card => card.Rarity != CardRarity.Basic)
                .Where(static card => card.Rarity != CardRarity.Ancient);
        }

        if (!pickFrom.Any())
        {
            return false;
        }

        var rollOptions = new CardCreationOptions(pickFrom, CardCreationSource.Other, creationOptions.RarityOdds)
            .WithFlags(CardCreationFlags.NoModifyHooks | CardCreationFlags.NoCardPoolModifications);
        var cardModel = CardFactory.CreateForReward(player, 1, rollOptions).FirstOrDefault()?.Card;
        if (cardModel is null)
        {
            return false;
        }

        if (GrantsUpgradedReward(player))
        {
            CardCmd.Upgrade(cardModel, CardPreviewStyle.None);
        }

        options.Add(new CardCreationResult(cardModel));
        Clear(player);
        return true;
    }
}
