using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ComicChess.KnowledgeDemon;

public static class KnowledgeDemonTelemetryEvents
{
    public static void CaptureRecordedToLibrary(
        Player player,
        CardModel sourceCard,
        IReadOnlyList<CardModel> recordedCopies)
    {
        var properties = new Dictionary<string, object?>
        {
            ["source_card_id"] = sourceCard.Id,
            ["source_upgraded"] = sourceCard.IsUpgraded,
            ["source_type"] = sourceCard.Type.ToString(),
            ["record_count"] = recordedCopies.Count,
            ["library_count_after"] = GetLibraryCount(player),
        };

        var payload = new JsonObject
        {
            ["source_card"] = CreateCardNode(sourceCard),
            ["recorded_cards"] = CreateCardArray(recordedCopies),
        };

        KnowledgeDemonTelemetry.CapturePayload("library.recorded", payload, properties);
    }

    public static void CaptureMaterializedFromLibrary(
        Player player,
        CardModel? sourceCard,
        IReadOnlyList<CardModel> selectedCards,
        IReadOnlyList<CardModel> materializedCards)
    {
        var properties = new Dictionary<string, object?>
        {
            ["source_card_id"] = sourceCard?.Id,
            ["source_upgraded"] = sourceCard?.IsUpgraded,
            ["selected_count"] = selectedCards.Count,
            ["materialized_count"] = materializedCards.Count,
            ["library_count_after"] = GetLibraryCount(player),
        };

        var payload = new JsonObject
        {
            ["source_card"] = sourceCard is null ? null : CreateCardNode(sourceCard),
            ["selected_cards"] = CreateCardArray(selectedCards),
            ["materialized_cards"] = CreateCardArray(materializedCards),
        };

        KnowledgeDemonTelemetry.CapturePayload("library.materialized", payload, properties);
    }

    public static void CaptureChooseResolved(
        Player player,
        CardModel? chooseSource,
        CardModel? chosenCard,
        IReadOnlyList<CardModel> candidates)
    {
        var properties = new Dictionary<string, object?>
        {
            ["source_card_id"] = chooseSource?.Id,
            ["source_upgraded"] = chooseSource?.IsUpgraded,
            ["candidate_count"] = candidates.Count,
            ["chosen_card_id"] = chosenCard?.Id,
            ["chosen_upgraded"] = chosenCard?.IsUpgraded,
            ["library_count_after_extract"] = GetLibraryCount(player),
        };

        var payload = new JsonObject
        {
            ["source_card"] = chooseSource is null ? null : CreateCardNode(chooseSource),
            ["chosen_card"] = chosenCard is null ? null : CreateCardNode(chosenCard),
            ["candidates"] = CreateCardArray(candidates),
        };

        KnowledgeDemonTelemetry.CapturePayload("choose.resolved", payload, properties);
    }

    public static void CaptureKnowledgeOverloadResolved(
        Player player,
        CardModel? sourceCard,
        int libraryCountBefore,
        int libraryCountAfter,
        int chooseCount,
        int chooseOfferCount)
    {
        var properties = new Dictionary<string, object?>
        {
            ["source_card_id"] = sourceCard?.Id,
            ["source_upgraded"] = sourceCard?.IsUpgraded,
            ["library_count_before"] = libraryCountBefore,
            ["library_count_after"] = libraryCountAfter,
            ["choose_count"] = chooseCount,
            ["threshold"] = chooseOfferCount,
        };

        var payload = new JsonObject
        {
            ["source_card"] = sourceCard is null ? null : CreateCardNode(sourceCard),
        };

        KnowledgeDemonTelemetry.CapturePayload("knowledge_overload.resolved", payload, properties);
    }

    private static JsonArray CreateCardArray(IEnumerable<CardModel> cards) =>
        new(cards.Select(CreateCardNode).ToArray());

    private static JsonObject CreateCardNode(CardModel card) =>
        new()
        {
            ["id"] = card.Id.ToString(),
            ["upgraded"] = card.IsUpgraded,
            ["type"] = card.Type.ToString(),
        };

    private static int GetLibraryCount(Player player) =>
        BookLibraryUtility.TryGetLibraryPile(player)?.Cards.Count ?? 0;
}
