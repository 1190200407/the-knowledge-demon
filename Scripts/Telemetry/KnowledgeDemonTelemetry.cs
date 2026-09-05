using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry;

namespace ComicChess.KnowledgeDemon;

public static class KnowledgeDemonTelemetry
{
    private static ITelemetryClient? _client;

    public static void Register()
    {
        if (!KnowledgeDemonTelemetryConfig.IsConfigured)
        {
            Entry.Logger.Info(
                "[Telemetry] Skipping telemetry registration because Host or ProjectApiKey is not configured.");
            return;
        }

        RitsuLibFramework.RegisterTelemetryApplicant(new TelemetryApplicant
        {
            ApplicantId = KnowledgeDemonTelemetryConfig.ApplicantId,
            OwnerModId = Entry.ModId,
            DisplayName = "Knowledge Demon",
            DisplayNameText = ModSettingsText.Literal("Knowledge Demon"),
            Adapter = new PostHogTelemetryAdapter(
                KnowledgeDemonTelemetryConfig.Host,
                KnowledgeDemonTelemetryConfig.ProjectApiKey),
            Requests =
            [
                TelemetryRequest.BasicUsage(ModSettingsText.Literal(
                    "发送游戏版本、平台、语言和匿名安装 ID，用来排查兼容性与版本覆盖范围。")),
                TelemetryRequest.ModInventory(ModSettingsText.Literal(
                    "发送已启用的模组列表，用来确认联动、兼容性与环境差异。")),
                TelemetryRequest.Diagnostics(ModSettingsText.Literal(
                    "发送异常与少量诊断上下文，用来定位崩溃、卡死和脚本错误。")),
                TelemetryRequest.RunHistory(
                    ModSettingsText.Literal(
                        "发送已结束跑局的 run-history，用来分析卡牌、遗物与玩法平衡。")),
                TelemetryRequest.Custom(
                    KnowledgeDemonTelemetryConfig.GameplayRequestId,
                    ModSettingsText.Literal(
                        "发送知识恶魔的局内关键事件，例如记录、具象、抉择与知识过载，用来分析玩法节奏和平衡。")),
            ],
        });

        _client = RitsuLibFramework.GetTelemetryClient(KnowledgeDemonTelemetryConfig.ApplicantId);
        Entry.Logger.Info("[Telemetry] Registered Knowledge Demon telemetry applicant.");
    }

    public static void Capture(
        string eventName,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        var client = _client;
        if (client is null)
        {
            return;
        }

        try
        {
            client.Capture(eventName, KnowledgeDemonTelemetryConfig.GameplayRequestId, properties);
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[Telemetry] Failed to capture event '{eventName}': {ex.Message}");
        }
    }

    public static void CapturePayload(
        string eventName,
        JsonNode payload,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        var client = _client;
        if (client is null)
        {
            return;
        }

        try
        {
            client.CapturePayload(eventName, KnowledgeDemonTelemetryConfig.GameplayRequestId, payload, properties);
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[Telemetry] Failed to capture payload event '{eventName}': {ex.Message}");
        }
    }

    public static void CaptureException(
        Exception exception,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        var client = _client;
        if (client is null)
        {
            return;
        }

        try
        {
            client.CaptureException(exception, properties);
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[Telemetry] Failed to capture diagnostics event: {ex.Message}");
        }
    }
}
