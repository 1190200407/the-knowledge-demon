using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.Transforms;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class LightTrapPower : KnowledgeDemonPowerModel, IKnowledgeDemonEventListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterCardTransformed(Player player, ModCardTransformContext context)
    {
        if (Owner.Player != player || Amount <= 0 || CombatState is null)
        {
            return;
        }

        IReadOnlyList<Creature> targets = CombatState.HittableEnemies;
        if (targets.Count == 0)
        {
            return;
        }

        Creature? target = player.RunState.Rng.CombatTargets.NextItem(targets);
        if (target is null)
        {
            return;
        }

        Flash();
        VfxCmd.PlayOnCreatureCenter(target, "vfx/vfx_attack_blunt");
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            target,
            Amount,
            ValueProp.Unpowered,
            Owner,
            null,
            null);
    }
}
