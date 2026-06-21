using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

internal sealed class BookLibraryPileInjectPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_book_library_pile_inject";
    public static string Description => "Inject NBookLibraryPile into NCombatUi";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCombatUi), nameof(NCombatUi._Ready)),
    ];

    public static void Postfix(NCombatUi __instance)
    {
        if (__instance.GetChildren().OfType<NBookLibraryPile>().Any())
        {
            return;
        }

        if (!ResourceLoader.Exists(NBookLibraryPile.ScenePath))
        {
            Entry.Logger.Error($"[BookLibrary] Scene missing: {NBookLibraryPile.ScenePath}");
            return;
        }

        var watch = ResourceLoader.Load<PackedScene>(NBookLibraryPile.ScenePath)
            .Instantiate<NBookLibraryPile>(PackedScene.GenEditState.Disabled);
        __instance.AddChildSafely(watch);
        __instance.MoveChildSafely(watch, __instance.Hand.GetIndex());
        Entry.Logger.Info(
            $"[BookLibrary][Inject] handIndex={__instance.Hand.GetIndex()} pileIndex={watch.GetIndex()}");
    }
}

internal sealed class BookLibraryPileActivatePatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_book_library_pile_activate";
    public static string Description => "Initialize NBookLibraryPile on combat UI activate";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCombatUi), nameof(NCombatUi.Activate), [typeof(CombatState)]),
    ];

    public static void Postfix(NCombatUi __instance, CombatState state)
    {
        var me = LocalContext.GetMe(state);
        if (me == null)
        {
            return;
        }

        foreach (var watch in __instance.GetChildren().OfType<NBookLibraryPile>())
        {
            watch.Initialize(me);
        }
    }
}

/// <summary>
/// Headless 藏书库牌堆不走 RitsuLib <see cref="NModExtraHand" />，需自行解析 <see cref="NCard.FindOnTable" />。
/// </summary>
internal sealed class BookLibraryFindOnTablePatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_book_library_find_on_table";
    public static string Description => "Resolve NCard.FindOnTable for custom library pile visuals";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCard), nameof(NCard.FindOnTable)),
    ];

    public static void Postfix(CardModel card, ref NCard? __result)
    {
        if (__result != null)
        {
            return;
        }

        if (card.Pile is not { } pile || !BookLibraryUtility.IsBookLibraryPile(pile.Type))
        {
            return;
        }

        __result = NBookLibraryPile.Instance?.GetCard(card);
        if (__result != null && NBookLibraryPile.Instance?.TryGetHolder(card) is { } holder)
        {
            holder.ResetCardTransform();
        }
    }
}

/// <summary>
/// 藏书库牌堆：原版跑完后，再按 Hand/Play 规则重算一遍带全局 hook 的动态数值预览。
/// </summary>
internal sealed class BookLibraryDynamicVarPreviewPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_book_library_dynamic_var_preview";
    public static string Description => "Enable combat dynamic-var hooks for cards in the library pile";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CardModel), nameof(CardModel.UpdateDynamicVarPreview),
            [typeof(CardPreviewMode), typeof(Creature), typeof(DynamicVarSet)]),
    ];

    public static void Postfix(
        CardModel __instance,
        CardPreviewMode previewMode,
        Creature? target,
        DynamicVarSet dynamicVarSet)
    {
        if (__instance.Pile is not { } pile || !BookLibraryUtility.IsBookLibraryPile(pile.Type))
        {
            return;
        }

        if (__instance.RunState == null && __instance.CombatState == null)
        {
            return;
        }

        var runGlobalHooks = __instance.CombatState != null
            || __instance.UpgradePreviewType == CardUpgradePreviewType.Combat;
        if (!runGlobalHooks)
        {
            return;
        }

        foreach (var item in dynamicVarSet.Values.ToList())
        {
            item.UpdateCardPreview(__instance, previewMode, target, runGlobalHooks: true);
        }
    }
}
