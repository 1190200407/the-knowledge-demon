using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

/// <summary>集中注册本 mod 全部 Ritsu <see cref="IPatchMethod"/>。</summary>
internal sealed class KnowledgeDemonModPatches : IModPatches
{
	public static void AddTo(ModPatcher patcher)
	{
		patcher.RegisterPatch<BookLibraryPileInjectPatch>();
		patcher.RegisterPatch<BookLibraryPileActivatePatch>();
		patcher.RegisterPatch<BookLibraryFindOnTablePatch>();
		patcher.RegisterPatch<BookLibraryDynamicVarPreviewPatch>();
		patcher.RegisterPatch<BookLibraryChooseScreenReadyPatch>();
		patcher.RegisterPatch<ParallelObservationTransformOptionsPatch>();
		patcher.RegisterPatch<KnowledgeDemonUniqueTransformOptionsPatch>();
		patcher.RegisterPatch<BigMushroomGrowScalePatch>();
		patcher.RegisterPatch<SurroundedPowerKnowledgeDemonFacingPatch>();
		patcher.RegisterPatch<KnowledgeDemonFastModeAttackCastSpeedPatch>();
	}
}
