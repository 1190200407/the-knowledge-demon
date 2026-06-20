using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

internal sealed class BigMushroomGrowScalePatch : IPatchMethod
{
	public static string PatchId => "knowledgedemon_big_mushroom_grow_scale";
	public static string Description => "Big Mushroom grow uses 0.6x scale for knowledge demon";
	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(BigMushroom), "Grow"),
	];

	public static bool Prefix(BigMushroom __instance)
	{
		if (__instance.Owner?.Character is not KnowledgeDemon)
		{
			return true;
		}

		NCombatRoom.Instance?.GetCreatureNode(__instance.Owner.Creature)?.ScaleTo(0.6f, 0f);
		return false;
	}
}

internal sealed class SurroundedPowerKnowledgeDemonFacingPatch : IPatchMethod
{
	public static string PatchId => "knowledgedemon_surrounded_flip_scale";
	public static string Description => "Invert Surrounded flip logic for knowledge demon facing";
	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(SurroundedPower), "FlipScale", new[] { typeof(Node2D) }),
	];

	public static bool Prefix(SurroundedPower __instance, Node2D? body, ref Task __result)
	{
		if (__instance.Owner?.Player?.Character is not KnowledgeDemon)
		{
			return true;
		}

		if (body == null)
		{
			__result = Task.CompletedTask;
			return false;
		}

		float x = body.Scale.X;
		SurroundedPower.Direction facing = __instance.Facing;
		bool shouldFlip = (facing == SurroundedPower.Direction.Right && x > 0f)
			|| (facing == SurroundedPower.Direction.Left && x < 0f);
		if (shouldFlip)
		{
			body.Scale = new Vector2(-body.Scale.X, body.Scale.Y);
		}

		__result = Task.CompletedTask;
		return false;
	}
}

/// <summary>
/// 加速模式下将 Attack / Cast 相关 Spine 动画提速，与 <see cref="MegaCrit.Sts2.Core.Commands.CreatureCmd.TriggerAnim" />
/// 缩短的等待时间更对齐（原版只缩 wait，不缩动画）。
/// </summary>
internal sealed class KnowledgeDemonFastModeAttackCastSpeedPatch : IPatchMethod
{
	public const float FastModeAttackCastTimeScale = 1.5f;

	public static string PatchId => "knowledgedemon_fast_mode_attack_cast_speed";
	public static string Description => "Double knowledge demon Attack/Cast spine speed in fast mode";
	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NCreature), nameof(NCreature.SetAnimationTrigger), [typeof(string)]),
	];

	public static void Postfix(NCreature __instance, string trigger)
	{
		if (__instance.Entity?.Player?.Character is not KnowledgeDemon)
			return;

		if (SaveManager.Instance.PrefsSave.FastMode != FastModeType.Fast)
			return;

		if (!IsAttackOrCastTrigger(trigger))
			return;

		if (!__instance.HasSpineAnimation)
			return;

		var track = __instance.SpineAnimation.GetCurrentTrack();
		if (track != null)
		{
			track.SetTimeScale(FastModeAttackCastTimeScale);
		}
	}

	private static bool IsAttackOrCastTrigger(string trigger) =>
		trigger is CreatureAnimator.attackTrigger
			or CreatureAnimator.castTrigger
			or "heavyAttack"
			or "superAttack";
}
