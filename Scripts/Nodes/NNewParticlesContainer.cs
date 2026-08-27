using System.Reflection;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ComicChess.KnowledgeDemon;

/// <summary>
[GlobalClass]
public partial class NNewParticlesContainer : NParticlesContainer
{
    private static readonly FieldInfo BaseParticlesField =
        typeof(NParticlesContainer).GetField("_particles", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Export]
    private Array<GpuParticles2D>? _particles;

    public override void _EnterTree()
    {
        SyncParticlesToBase();
        base._EnterTree();
    }

    public override void _Ready()
    {
        SyncParticlesToBase();
        base._Ready();
    }

    private void SyncParticlesToBase()
    {
        Array<GpuParticles2D> particles = _particles ?? [];
        if (particles.Count == 0)
        {
            particles = [];
            CollectGpuParticles(this, particles);
            _particles = particles;
        }

        BaseParticlesField.SetValue(this, particles);
    }

    private static void CollectGpuParticles(Node node, Array<GpuParticles2D> particles)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is GpuParticles2D gpuParticle)
            {
                particles.Add(gpuParticle);
            }

            CollectGpuParticles(child, particles);
        }
    }
}
