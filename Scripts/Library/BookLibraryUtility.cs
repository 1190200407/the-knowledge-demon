using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.CardPiles;

namespace ComicChess.KnowledgeDemon;

/// <summary>藏书库牌堆注册与 UI 刷新的共用工具。</summary>
public static class BookLibraryUtility
{
    public const string LocalStem = "library";

    /// <summary>藏书库复制品：略透明、带淡绿色。</summary>
    // #rgb(198, 255, 220)
    public static readonly Color LibraryCardModulate = new(198f / 255f, 255f / 255f, 226f / 255f, 0.9f);

    /// <summary>在 <see cref="Entry.Init" /> 中由 <see cref="ModCardPileRegistry.RegisterOwned" /> 赋值。</summary>
    public static PileType PileType;

    public static bool PlayerHasBookLibraryRelic(Player? player) =>
        player?.GetRelic<CognitionVesselRelic>() is not null
        || player?.GetRelic<KnowledgeHostRelic>() is not null;

    public static bool PlayerHasSharedStarterRelicEffect(Player? player) =>
        player?.Creature.GetPower<TelepathyPower>() is not null;

    public static bool PlayerHasBookLibraryAccess(Player? player) =>
        PlayerHasBookLibraryRelic(player)
        || PlayerHasSharedStarterRelicEffect(player);

    public static bool PlayerHasKnowledgeHostRelic(Player? player) =>
        player?.GetRelic<KnowledgeHostRelic>() is not null;

    public static CardPile? TryGetLibraryPile(Player? player) =>
        player is null ? null : PileType.GetPile(player);

    public static bool PlayerHasBookLibrary(Player? player) =>
        TryGetLibraryPile(player) is not null;

    public static bool IsBookLibraryPile(PileType pileType) =>
        pileType == PileType;

    public static void RefreshLocalBookLibraryUi(Player? player)
    {
        if (player is null)
        {
            return;
        }

        if (NBookLibraryPile.Instance?.Player == player)
        {
            NBookLibraryPile.Instance.RefreshAvailability();
        }

        if (NLibraryPileButton.Instance?.Player == player)
        {
            NLibraryPileButton.Instance.RefreshAvailability();
        }
    }

    public static void RefreshCardVisual(CardModel card)
    {
        if (card.Pile is not { } pile || !IsBookLibraryPile(pile.Type))
        {
            return;
        }

        var ncard = NBookLibraryPile.Instance?.TryGetHolder(card)?.CardNode;
        if (ncard != null)
        {
            ApplyLibraryCardPreviewVisuals(ncard);
        }

        NBookLibraryPile.Instance?.TryGetHolder(card)?.UpdateCard();
    }

    public static void RefreshHandCardVisual(CardModel card)
    {
        if (card.Pile?.Type != PileType.Hand)
        {
            return;
        }

        var ncard = NCard.FindOnTable(card);
        if (ncard != null)
        {
            ApplyRealHandTableVisuals(ncard);
            return;
        }

        Callable.From(() =>
        {
            if (card.Pile?.Type != PileType.Hand)
            {
                return;
            }

            var deferredCard = NCard.FindOnTable(card);
            if (deferredCard != null)
            {
                ApplyRealHandTableVisuals(deferredCard);
            }
        }).CallDeferred();
    }

    public static void ApplyLibraryCardPreviewVisuals(NCard ncard, bool applyTint = true)
    {
        if (!GodotObject.IsInstanceValid(ncard))
        {
            return;
        }

        ncard.SetForceUnpoweredPreview(false);
        ncard.SetPretendCardCanBePlayed(true);
        ncard.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
        if (applyTint)
        {
            ApplyLibraryCardTint(ncard);
        }
    }

    public static void ApplyHandCardPreviewVisuals(NCard ncard)
    {
        if (!GodotObject.IsInstanceValid(ncard))
        {
            return;
        }

        ncard.SetForceUnpoweredPreview(false);
        ncard.SetPretendCardCanBePlayed(false);
        ncard.Modulate = Colors.White;
        ncard.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
    }

    public static void ApplyHandTableVisuals(NCard ncard)
    {
        if (!GodotObject.IsInstanceValid(ncard))
        {
            return;
        }

        if (ncard.IsNodeReady())
        {
            ApplyLibraryCardPreviewVisuals(ncard);
            return;
        }

        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(ncard) || !ncard.IsNodeReady())
            {
                return;
            }

            ApplyLibraryCardPreviewVisuals(ncard);
        }).CallDeferred();
    }

    public static void ApplyRealHandTableVisuals(NCard ncard)
    {
        if (!GodotObject.IsInstanceValid(ncard))
        {
            return;
        }

        if (ncard.IsNodeReady())
        {
            ApplyHandCardPreviewVisuals(ncard);
            return;
        }

        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(ncard) || !ncard.IsNodeReady())
            {
                return;
            }

            ApplyHandCardPreviewVisuals(ncard);
        }).CallDeferred();
    }

    public static void ApplyLibraryCardTint(NCard ncard)
    {
        if (!GodotObject.IsInstanceValid(ncard))
        {
            return;
        }

        ncard.Modulate = LibraryCardModulate;
    }

    public static void ResetCardTint(CardModel card)
    {
        var ncard = NBookLibraryPile.Instance?.TryGetHolder(card)?.CardNode;
        if (ncard == null || !GodotObject.IsInstanceValid(ncard))
        {
            return;
        }

        ncard.Modulate = Colors.White;
    }

    /// <summary>藏书库内原位变化动画；其余牌堆走原版中央预览。</summary>
    public static async Task<CardPileAddResult?> TransformCard(
        CardModel original,
        CardModel replacement)
    {
        var pileUi = NBookLibraryPile.Instance;
        var inLibrary = original.Pile is { } originalPile && IsBookLibraryPile(originalPile.Type);
        NBookLibraryCardHolder? holder = null;
        var retainHolder = inLibrary
            && pileUi is not null
            && pileUi.TryTakeHolderForTransform(original, out holder);

        try
        {
            var style = retainHolder ? CardPreviewStyle.None : CardPreviewStyle.HorizontalLayout;
            var result = await CardCmd.Transform(original, replacement, style);

            if (retainHolder && holder is not null && result is { cardAdded: not null } addResult)
            {
                await holder.PlayTransformAnim(addResult.cardAdded);
            }

            return result;
        }
        finally
        {
            if (retainHolder)
            {
                pileUi?.ClearPendingTransformHolder();
            }
        }
    }

    public static async Task<CardPileAddResult> TransformToRandom(CardModel original, Rng rng)
    {
        if (original.Pile is { } pile && IsBookLibraryPile(pile.Type))
        {
            var replacement = new CardTransformation(original).GetReplacement(rng);
            if (replacement is null)
            {
                throw new InvalidOperationException($"Cannot transform un-transformable card {original}.");
            }

            return await TransformCard(original, replacement)
                ?? throw new InvalidOperationException($"Transform failed for {original}.");
        }

        return await CardCmd.TransformToRandom(original, rng, CardPreviewStyle.HorizontalLayout);
    }
}
