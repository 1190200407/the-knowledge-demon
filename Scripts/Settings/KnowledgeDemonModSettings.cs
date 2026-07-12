using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.RuntimeInput;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace ComicChess.KnowledgeDemon;

public sealed class KnowledgeDemonModSettings
{
    public string ToggleLibraryHotkey { get; set; } = KnowledgeDemonRuntimeHotkeys.DefaultToggleLibraryBinding;
}

public static class KnowledgeDemonModSettingsPage
{
    private const string SettingsDataKey = "settings";
    private const string SettingsFileName = "settings.json";

    private static readonly ModDataStore Store = ModDataStore.For(Entry.ModId);

    private static readonly IModSettingsValueBinding<string> ToggleLibraryHotkeyBinding =
        ModSettingsBindings.WithDefault(
            new ModSettingsValueBinding<KnowledgeDemonModSettings, string>(
                Entry.ModId,
                SettingsDataKey,
                SaveScope.Global,
                static settings => settings.ToggleLibraryHotkey,
                static (settings, value) =>
                {
                    var normalized = RuntimeHotkeyService.NormalizeOrDefault(
                        value,
                        KnowledgeDemonRuntimeHotkeys.DefaultToggleLibraryBinding);
                    settings.ToggleLibraryHotkey = normalized;
                    KnowledgeDemonRuntimeHotkeys.TryRebindToggleLibraryHotkey(normalized);
                }),
            static () => KnowledgeDemonRuntimeHotkeys.DefaultToggleLibraryBinding);

    public static void Register()
    {
        Store.Register<KnowledgeDemonModSettings>(
            SettingsDataKey,
            SettingsFileName,
            SaveScope.Global,
            defaultFactory: static () => new KnowledgeDemonModSettings(),
            autoCreateIfMissing: true);

        RitsuLibFramework.RegisterModSettings(
            Entry.ModId,
            page => page
                .WithTitle(ModSettingsText.Literal("设置"))
                .WithModDisplayName(ModSettingsText.Literal("知识恶魔"))
                .AddSection("controls", section => section
                    .WithTitle(ModSettingsText.Literal("操作"))
                    .AddKeyBinding(
                        "toggle_library_hotkey",
                        ModSettingsText.Literal("藏书库快捷键"),
                        ToggleLibraryHotkeyBinding,
                        allowModifierCombos: true,
                        allowModifierOnly: false,
                        distinguishModifierSides: false,
                        description: ModSettingsText.Literal("显示或隐藏藏书库。"))));
    }

    public static string GetToggleLibraryHotkey()
    {
        return RuntimeHotkeyService.NormalizeOrDefault(
            ToggleLibraryHotkeyBinding.Read(),
            KnowledgeDemonRuntimeHotkeys.DefaultToggleLibraryBinding);
    }
}
