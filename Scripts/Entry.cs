using System.Reflection;
using Godot.Bridge;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Combat.Rewards;
using STS2RitsuLib.Cards.Transforms;
using STS2RitsuLib.Patching.Core;
using Godot;

namespace ComicChess.KnowledgeDemon;

/// <summary>Mod 入口：ContentPack、图鉴筛选、类型发现与 Harmony 补丁。</summary>
[ModInitializer("Init")]
public class Entry
{
	public const string ModId = "KnowledgeDemon";
	public static readonly MegaCrit.Sts2.Core.Logging.Logger Logger = RitsuLibFramework.CreateLogger(ModId);

	public static void Init()
	{
		var assembly = Assembly.GetExecutingAssembly();
		RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
		ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);


        var registry = ModCardPileRegistry.For(ModId);
		BookLibraryUtility.PileType = registry.RegisterOwned(
			BookLibraryUtility.LocalStem,
			new ModCardPileSpec
			{
				Scope = ModCardPileScope.CombatOnly,
				Style = ModCardPileUiStyle.Headless,
				CardShouldBeVisible = true,
				VisibleWhen = static ctx => BookLibraryUtility.PlayerHasBookLibraryRelic(ctx.Player),
				FlightTargetPositionResolver = static _ =>
					NBookLibraryPile.Instance?.GetSlotGlobalPosition(0),
				FlightStartPositionResolver = static ctx =>
				{
					if (ctx.CardModel is { } card)
					{
						return NBookLibraryPile.Instance?.GetSlotGlobalPositionForCard(card);
					}

					return NBookLibraryPile.Instance?.GetSlotGlobalPosition(0);
				},
			}
		).PileType;

		var keywordRegistry = ModKeywordRegistry.For(ModId);
		keywordRegistry.RegisterCardKeywordOwnedByLocNamespace(
			"materialize",
			iconPath: null,
			cardDescriptionPlacement: ModKeywordCardDescriptionPlacement.None,
			includeInCardHoverTip: false);
		keywordRegistry.RegisterCardKeywordOwnedByLocNamespace("record");
		keywordRegistry.RegisterCardKeywordOwnedByLocNamespace(
			"choose",
			iconPath: null,
			cardDescriptionPlacement: ModKeywordCardDescriptionPlacement.None,
			includeInCardHoverTip: true);
		keywordRegistry.RegisterCardKeywordOwnedByLocNamespace("knowledge_overload");
		keywordRegistry.RegisterCardKeywordOwnedByLocNamespace(
			"unique",
			iconPath: null,
			cardDescriptionPlacement: ModKeywordCardDescriptionPlacement.AfterCardDescription,
			includeInCardHoverTip: true);

		EnlightenmentAttainedRewardRegistration.Register();
		RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<MakeAChoice, ItIsDone>(ModId);
		RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<CognitionVesselRelic, KnowledgeHostRelic>(ModId);
		ModCardTransformRegistry.For(ModId).Register(
			"knowledge_demon_transform_events",
			KnowledgeDemonHook.AfterCardTransformed);

		var patcher = RitsuLibFramework.CreatePatcher(ModId, "main", "knowledge-demon");
		patcher.RegisterPatches<KnowledgeDemonModPatches>();
		RitsuLibFramework.ApplyRequiredPatcher(patcher, DisableMod);
	}

	private static void DisableMod()
	{
		Logger.Error("Required patches failed to apply; Knowledge Demon mod is disabled for this session.");
	}
}
