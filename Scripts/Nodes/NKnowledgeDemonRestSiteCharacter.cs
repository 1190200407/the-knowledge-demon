using Godot;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace ComicChess.KnowledgeDemon;

[GlobalClass]
public partial class NKnowledgeDemonRestSiteCharacter : NRestSiteCharacter
{
    private const string LightMaskPath = "res://KnowledgeDemon/images/charui/knowledge_demon_restsite_lightmask.png";
    private const float BodyLightAlpha = 0.5f;

    private Sprite2D? _bodyFireLight;

    public override void _Ready()
    {
        base._Ready();

        _bodyFireLight = GetNode<Sprite2D>("%BodyFireLight");
        RefreshBodyFireLightMask();
        ApplyActLighting();
    }

    public void HideKnowledgeDemonFlameGlow()
    {
        if (_bodyFireLight != null && GodotObject.IsInstanceValid(_bodyFireLight))
        {
            _bodyFireLight.Visible = false;
        }
    }

    private void RefreshBodyFireLightMask()
    {
        if (_bodyFireLight == null)
        {
            return;
        }

        Texture2D? mask = ResourceLoader.Load<Texture2D>(LightMaskPath, null, ResourceLoader.CacheMode.Ignore);
        if (mask != null)
        {
            _bodyFireLight.Texture = mask;
        }
    }

    private void ApplyActLighting()
    {
        if (Player?.RunState == null)
        {
            return;
        }

        var bodyTint = GetBodyTint(Player.RunState.CurrentActIndex);
        _bodyFireLight!.SelfModulate = new Color(bodyTint.R, bodyTint.G, bodyTint.B, BodyLightAlpha);
    }

    private static Color GetBodyTint(int actIndex) =>
        actIndex switch
        {
            0 => new Color(0.71f, 0.90f, 0.36f),
            1 => new Color(1.00f, 0.74f, 0.20f),
            2 => new Color(0.33f, 0.82f, 1.00f),
            _ => new Color(1.00f, 1.00f, 1.00f),
        };
}
