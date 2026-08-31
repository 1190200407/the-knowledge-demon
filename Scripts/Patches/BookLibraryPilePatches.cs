using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

internal sealed class LibraryPileButtonInitializePatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_library_pile_button_initialize";
    public static string Description => "Initialize attached NLibraryPileButton on combat pile container initialize";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCombatPilesContainer), nameof(NCombatPilesContainer.Initialize), [typeof(Player)]),
    ];

    public static void Postfix(NCombatPilesContainer __instance, Player player)
    {
        if (!ModNodeAttachmentRegistry.For(Entry.ModId)
                .TryGetAttached<NCombatPilesContainer, NLibraryPileButton>(
                    __instance,
                    NLibraryPileButton.NodeAttachmentLocalId,
                    out var pileButton)
            || pileButton == null)
        {
            return;
        }

        pileButton.Initialize(player);
    }
}

internal sealed class LibraryPileButtonAnimInPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_library_pile_button_anim_in";
    public static string Description => "Animate attached NLibraryPileButton into combat UI";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCombatPilesContainer), nameof(NCombatPilesContainer.AnimIn)),
    ];

    public static void Postfix(NCombatPilesContainer __instance)
    {
        if (ModNodeAttachmentRegistry.For(Entry.ModId)
                .TryGetAttached<NCombatPilesContainer, NLibraryPileButton>(
                    __instance,
                    NLibraryPileButton.NodeAttachmentLocalId,
                    out var pileButton)
            && pileButton != null)
        {
            pileButton.AnimIn();
        }
    }
}

internal sealed class LibraryPileButtonAnimOutPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_library_pile_button_anim_out";
    public static string Description => "Animate attached NLibraryPileButton out of combat UI";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCombatPilesContainer), nameof(NCombatPilesContainer.AnimOut)),
    ];

    public static void Postfix(NCombatPilesContainer __instance)
    {
        if (ModNodeAttachmentRegistry.For(Entry.ModId)
                .TryGetAttached<NCombatPilesContainer, NLibraryPileButton>(
                    __instance,
                    NLibraryPileButton.NodeAttachmentLocalId,
                    out var pileButton)
            && pileButton != null)
        {
            pileButton.AnimOut();
        }
    }
}

internal sealed class LibraryPileButtonEnablePatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_library_pile_button_enable";
    public static string Description => "Enable attached NLibraryPileButton with combat pile UI";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCombatPilesContainer), nameof(NCombatPilesContainer.Enable)),
    ];

    public static void Postfix(NCombatPilesContainer __instance)
    {
        if (ModNodeAttachmentRegistry.For(Entry.ModId)
                .TryGetAttached<NCombatPilesContainer, NLibraryPileButton>(
                    __instance,
                    NLibraryPileButton.NodeAttachmentLocalId,
                    out var pileButton)
            && pileButton != null)
        {
            pileButton.Enable();
        }
    }
}

internal sealed class LibraryPileButtonDisablePatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_library_pile_button_disable";
    public static string Description => "Disable attached NLibraryPileButton with combat pile UI";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCombatPilesContainer), nameof(NCombatPilesContainer.Disable)),
    ];

    public static void Postfix(NCombatPilesContainer __instance)
    {
        if (ModNodeAttachmentRegistry.For(Entry.ModId)
                .TryGetAttached<NCombatPilesContainer, NLibraryPileButton>(
                    __instance,
                    NLibraryPileButton.NodeAttachmentLocalId,
                    out var pileButton)
            && pileButton != null)
        {
            pileButton.Disable();
        }
    }
}

internal sealed class BookLibraryPileInitializePatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_book_library_pile_initialize";
    public static string Description => "Initialize attached NBookLibraryPile on combat UI activate";
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

        if (!ModNodeAttachmentRegistry.For(Entry.ModId)
                .TryGetAttached<NCombatUi, NBookLibraryPile>(
                    __instance,
                    NBookLibraryPile.NodeAttachmentLocalId,
                    out var pile)
            || pile == null)
        {
            return;
        }

        pile.Initialize(me);
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
        if (__instance.Pile is { } pile && BookLibraryUtility.IsBookLibraryPile(pile.Type))
        {
            // 藏书库堆内牌
        }
        else if (!KnowledgeDemonChooseContext.IsChooseCandidate(__instance))
        {
            return;
        }

        if (__instance.RunState == null && __instance.CombatState == null)
        {
            return;
        }

        foreach (var item in dynamicVarSet.Values.ToList())
        {
            item.UpdateCardPreview(__instance, previewMode, target, runGlobalHooks: true);
        }
    }
}

internal sealed class BookLibrarySharedHandSimpleSelectPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_book_library_shared_hand_simple_select";
    public static string Description => "Route mixed hand/library selection through the shared library selected-card container";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NPlayerHand), "SelectCardInSimpleMode", [typeof(NHandCardHolder)]),
    ];

    public static bool Prefix(NPlayerHand __instance, NHandCardHolder holder)
    {
        var session = KnowledgeDemonCardSelectSession.ActiveSession;
        if (session is not { IsActive: true })
        {
            return true;
        }

        if (!session.IncludeHand)
        {
            if (__instance.PeekButton.IsPeeking)
            {
                __instance.PeekButton.Wiggle();
            }

            return false;
        }

        session.SelectHandCard(__instance, holder);
        return false;
    }
}

internal sealed class BookLibrarySharedHandRevalidatePatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_book_library_shared_hand_revalidate";
    public static string Description => "Revalidate mixed hand/library selection against the shared selected-card container";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NPlayerHand), "RevalidateSelectionAfterStateChange"),
    ];

    public static bool Prefix(NPlayerHand __instance)
    {
        var session = KnowledgeDemonCardSelectSession.ActiveSession;
        if (session is not { IsActive: true, IncludeHand: true })
        {
            return true;
        }

        session.RevalidateSelectionAfterStateChange(__instance);
        return false;
    }
}

internal sealed class BookLibrarySharedHandGetCardHolderPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_book_library_shared_hand_get_card_holder";
    public static string Description => "Allow hand lookups to find cards living in the shared library selected-card container";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NPlayerHand), nameof(NPlayerHand.GetCardHolder), [typeof(CardModel)]),
    ];

    public static void Postfix(CardModel card, ref NCardHolder? __result)
    {
        if (__result != null)
        {
            return;
        }

        var session = KnowledgeDemonCardSelectSession.ActiveSession;
        if (session is not { IsActive: true, IncludeHand: true })
        {
            return;
        }

        __result = NBookLibraryPile.Instance?
            .GetSelectedCardHolders()
            .FirstOrDefault(holder => holder.CardNode?.Model == card);
    }
}
