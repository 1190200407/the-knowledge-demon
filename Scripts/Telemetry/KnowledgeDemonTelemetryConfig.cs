using System;

namespace ComicChess.KnowledgeDemon;

public static class KnowledgeDemonTelemetryConfig
{
    public const string ApplicantId = Entry.ModId;
    public const string GameplayRequestId = "knowledge_demon_gameplay";

    // Fill these in to enable telemetry.
    // Direct PostHog: Host = "https://us.i.posthog.com", ProjectApiKey = "<project key>"
    // Proxy: Host = "https://your-worker.workers.dev", ProjectApiKey = "proxy"
    public const string Host = "https://patient-mountain-76fe.comicchess-knowledge-demon.workers.dev";
    public const string ProjectApiKey = "proxy";

    public static bool IsConfigured =>
        Uri.TryCreate(Host, UriKind.Absolute, out _)
        && !string.IsNullOrWhiteSpace(ProjectApiKey);
}
