using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

internal sealed class DustyTomeInfiniteAncientCardSetterPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_dusty_tome_replace_infinite_ancient_card";
    public static string Description => "Replace Infinite if Dusty Tome tries to use it as its ancient card";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(DustyTome), nameof(DustyTome.AncientCard), MethodType.Setter),
    ];

    public static void Prefix(DustyTome __instance, ref ModelId? value)
    {
        if (value is not { } candidateId)
        {
            return;
        }

        var candidate = ModelDb.GetByIdOrNull<CardModel>(candidateId);
        if (candidate is null || !TransformOptionUtility.IsInfinite(candidate))
        {
            return;
        }

        if (__instance.Owner is not Player player)
        {
            return;
        }

        var transcendenceCardIds = ArchaicTooth.TranscendenceCards
            .Select(static card => card.Id)
            .ToHashSet();
        var replacements = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Concat(player.Character.CardPool.AllCards)
            .Where(static card => card.Rarity == CardRarity.Ancient)
            .Where(card => !transcendenceCardIds.Contains(card.Id))
            .Where(card => !TransformOptionUtility.IsInfinite(card))
            .GroupBy(static card => card.Id)
            .Select(static group => group.First())
            .ToList();

        if (replacements.Count == 0)
        {
            Entry.Logger.Warn("[DustyTome] Infinite was selected but no replacement Ancient card was available.");
            return;
        }

        var replacement = player.PlayerRng.Rewards.NextItem(replacements);
        if (replacement is null)
        {
            return;
        }

        Entry.Logger.Info($"[DustyTome] Replaced Infinite with {replacement.Id}.");
        value = replacement.Id;
    }
}
