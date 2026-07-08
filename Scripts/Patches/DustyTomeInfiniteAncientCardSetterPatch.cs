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
        new(typeof(DustyTome), nameof(DustyTome.SetupForPlayer), [typeof(Player)]),
    ];

    public static void Postfix(DustyTome __instance)
    {
        if (__instance.AncientCard is not { } candidateId)
        {
            return;
        }

        var candidate = ModelDb.GetByIdOrNull<CardModel>(candidateId);
        if (candidate is null || !TransformOptionUtility.IsInfinite(candidate))
        {
            return;
        }

        __instance.AncientCard = ModelDb.Card<EnlightenmentAttained>().Id;
        Entry.Logger.Info("[DustyTome] Replaced Infinite with ItIsDone.");
    }
}
