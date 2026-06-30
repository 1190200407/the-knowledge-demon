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
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace ComicChess.KnowledgeDemon;

[RegisterSingleton]
public sealed class MimicryRewardSingleton : HookedSingletonModel
{
    private readonly record struct MimicryRewardData(ModelId CharacterId, bool GrantsUpgradedReward);

    private static readonly Dictionary<ulong, MimicryRewardData> ChosenCharacterIds = [];

    public MimicryRewardSingleton()
        : base(HookType.Run)
    {
    }

    public static void SetChosenCharacter(Player player, CharacterModel character, bool grantsUpgradedReward)
    {
        ChosenCharacterIds[player.NetId] = new MimicryRewardData(character.Id, grantsUpgradedReward);
    }

    public static void Clear(Player player)
    {
        ChosenCharacterIds.Remove(player.NetId);
    }

    public static CharacterModel? GetChosenCharacter(Player player)
    {
        return ChosenCharacterIds.TryGetValue(player.NetId, out var data)
            ? ModelDb.GetById<CharacterModel>(data.CharacterId)
            : null;
    }

    public static bool GrantsUpgradedReward(Player player)
    {
        return ChosenCharacterIds.TryGetValue(player.NetId, out var data) && data.GrantsUpgradedReward;
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
