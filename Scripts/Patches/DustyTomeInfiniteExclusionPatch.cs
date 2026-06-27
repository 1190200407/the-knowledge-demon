using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

internal sealed class DustyTomeInfiniteExclusionPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_dusty_tome_exclude_infinite";
    public static string Description => "Exclude Infinite from Dusty Tome's ancient-card pool";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(DustyTome), nameof(DustyTome.SetupForPlayer), [typeof(Player)]),
    ];

    public static void Postfix(DustyTome __instance, Player player)
    {
        var filteredAncients = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(card => card.Rarity == CardRarity.Ancient)
            .Where(card => !TransformOptionUtility.IsInfinite(card))
            .ToList();

        if (filteredAncients.Count == 0)
        {
            return;
        }

        var chosen = player.PlayerRng.Rewards.NextItem(filteredAncients);
        if (chosen is null)
        {
            return;
        }

        __instance.AncientCard = chosen.Id;
    }
}
