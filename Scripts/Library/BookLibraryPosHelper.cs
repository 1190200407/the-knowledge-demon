using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace ComicChess.KnowledgeDemon;

/// <summary>藏书库摆位与手牌共用 <see cref="HandPosHelper" />（按当前张数居中展开）。</summary>
public static class BookLibraryPosHelper
{
    private const int HandLayoutMax = 10;
    private const float BaseYOffset = -50f;

    public static Vector2 GetPosition(int cardCount, int pileIndex)
    {
        if (cardCount <= HandLayoutMax)
        {
            return HandPosHelper.GetPosition(cardCount, pileIndex);
        }

        var left = HandPosHelper.GetPosition(HandLayoutMax, 0).X;
        var right = HandPosHelper.GetPosition(HandLayoutMax, HandLayoutMax - 1).X;
        var t = cardCount <= 1 ? 0.5f : pileIndex / (float)(cardCount - 1);
        return new Vector2(Mathf.Lerp(left, right, t), BaseYOffset);
    }

    public static float GetAngle(int cardCount, int pileIndex)
    {
        if (cardCount <= HandLayoutMax)
        {
            return HandPosHelper.GetAngle(cardCount, pileIndex);
        }

        var maxAngle = HandPosHelper.GetAngle(HandLayoutMax, HandLayoutMax - 1);
        var t = cardCount <= 1 ? 0f : (pileIndex / (float)(cardCount - 1) - 0.5f) * 2f;
        return maxAngle * t;
    }

    public static Vector2 GetScale(int cardCount)
    {
        var scale = HandPosHelper.GetScale(Math.Min(cardCount, HandLayoutMax));
        return cardCount > HandLayoutMax ? scale * 0.85f : scale;
    }

    public static Vector2 GetHoverSpreadOffset(int focusedPileIndex, int pileIndex) =>
        GetHoverSpreadOffset(focusedPileIndex, pileIndex, 100f, 4f);

    public static Vector2 GetHoverSpreadOffset(int focusedPileIndex, int pileIndex, float maxSpread, float falloffCards)
    {
        if (focusedPileIndex < 0 || pileIndex == focusedPileIndex)
        {
            return Vector2.Zero;
        }

        var spread = Mathf.Lerp(maxSpread, 0f, Mathf.Min(1f, Mathf.Abs(focusedPileIndex - pileIndex) / falloffCards));
        return Vector2.Left * Mathf.Sign(focusedPileIndex - pileIndex) * spread;
    }
}
