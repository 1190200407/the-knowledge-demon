using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

/// <summary>集中注册本 mod 全部 Ritsu <see cref="IPatchMethod"/>。</summary>
internal sealed class KnowledgeDemonModPatches : IModPatches
{
	public static void AddTo(ModPatcher patcher)
	{
		patcher.RegisterPatch<LibraryPileButtonInitializePatch>();
		patcher.RegisterPatch<LibraryPileButtonAnimInPatch>();
		patcher.RegisterPatch<LibraryPileButtonAnimOutPatch>();
		patcher.RegisterPatch<LibraryPileButtonEnablePatch>();
		patcher.RegisterPatch<LibraryPileButtonDisablePatch>();
		patcher.RegisterPatch<BookLibraryPileInitializePatch>();
		patcher.RegisterPatch<BookLibraryFindOnTablePatch>();
		patcher.RegisterPatch<BookLibraryDynamicVarPreviewPatch>();
		patcher.RegisterPatch<BookLibrarySharedHandSimpleSelectPatch>();
		patcher.RegisterPatch<BookLibrarySharedHandRevalidatePatch>();
		patcher.RegisterPatch<BookLibrarySharedHandGetCardHolderPatch>();
		patcher.RegisterPatch<BookLibraryChooseScreenReadyPatch>();
		patcher.RegisterPatch<KnowledgeDemonUniqueTransformOptionsPatch>();
		patcher.RegisterPatch<SingularityTransformPatch>();
		patcher.RegisterPatch<DustyTomeInfiniteAncientCardSetterPatch>();
		patcher.RegisterPatch<TouchOfOrobasStarterRelicHoverTipPatch>();
		patcher.RegisterPatch<BigMushroomGrowScalePatch>();
		patcher.RegisterPatch<SurroundedPowerKnowledgeDemonFacingPatch>();
		patcher.RegisterPatch<KnowledgeDemonFastModeAttackCastSpeedPatch>();
		patcher.RegisterPatch<KnowledgeDemonRestSiteHideFlameGlowPatch>();
	}
}
