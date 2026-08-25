using Godot;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.RuntimeInput;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace ComicChess.KnowledgeDemon;

public sealed class KnowledgeDemonModSettings
{
    public string LibraryUiPreset { get; set; } = KnowledgeDemonModSettingsPage.LibraryUiPresetOne;
    public int LibraryUiPositionX { get; set; } = KnowledgeDemonModSettingsPage.LibraryUiPresetOneX;
    public int LibraryUiPositionY { get; set; } = KnowledgeDemonModSettingsPage.LibraryUiPresetOneY;
    public string ToggleLibraryHotkey { get; set; } = KnowledgeDemonRuntimeHotkeys.DefaultToggleLibraryBinding;
}

public static class KnowledgeDemonModSettingsPage
{
    public const string LibraryUiPresetOne = "preset_one";
    public const string LibraryUiPresetTwo = "preset_two";
    public const string LibraryUiPresetCustom = "custom";

    public const int LibraryUiPresetOneX = 110;
    public const int LibraryUiPresetOneY = 985;
    public const int LibraryUiPresetTwoX = 10;
    public const int LibraryUiPresetTwoY = 885;

    private const int LibraryUiPositionMinX = 0;
    private const int LibraryUiPositionMaxX = 1840;
    private const int LibraryUiPositionMinY = 0;
    private const int LibraryUiPositionMaxY = 1000;

    private const string SettingsDataKey = "settings";
    private const string SettingsFileName = "settings.json";

    private static readonly ModDataStore Store = ModDataStore.For(Entry.ModId);
    private static bool _synchronizingPresetSliders;

    private static readonly IModSettingsValueBinding<string> LibraryUiPresetBinding =
        ModSettingsBindings.WithDefault(
            new ModSettingsValueBinding<KnowledgeDemonModSettings, string>(
                Entry.ModId,
                SettingsDataKey,
                SaveScope.Global,
                static settings => settings.LibraryUiPreset,
                static (settings, value) =>
                {
                    var preset = IsKnownPreset(value) ? value : LibraryUiPresetOne;
                    settings.LibraryUiPreset = preset;
                    if (preset != LibraryUiPresetCustom)
                    {
                        ApplyPreset(settings, preset);
                        SynchronizePresetSliders(settings);
                    }

                    ApplyLibraryUiPosition();
                }),
            static () => LibraryUiPresetOne);

    private static readonly IModSettingsValueBinding<int> LibraryUiPositionXBinding =
        ModSettingsBindings.WithDefault(
            new ModSettingsValueBinding<KnowledgeDemonModSettings, int>(
                Entry.ModId,
                SettingsDataKey,
                SaveScope.Global,
                static settings => settings.LibraryUiPositionX,
                static (settings, value) =>
                {
                    settings.LibraryUiPositionX = Math.Clamp(value, LibraryUiPositionMinX, LibraryUiPositionMaxX);
                    if (!_synchronizingPresetSliders)
                    {
                        settings.LibraryUiPreset = LibraryUiPresetCustom;
                    }

                    ApplyLibraryUiPosition();
                }),
            static () => LibraryUiPresetOneX);

    private static readonly IModSettingsValueBinding<int> LibraryUiPositionYBinding =
        ModSettingsBindings.WithDefault(
            new ModSettingsValueBinding<KnowledgeDemonModSettings, int>(
                Entry.ModId,
                SettingsDataKey,
                SaveScope.Global,
                static settings => settings.LibraryUiPositionY,
                static (settings, value) =>
                {
                    settings.LibraryUiPositionY = Math.Clamp(value, LibraryUiPositionMinY, LibraryUiPositionMaxY);
                    if (!_synchronizingPresetSliders)
                    {
                        settings.LibraryUiPreset = LibraryUiPresetCustom;
                    }

                    ApplyLibraryUiPosition();
                }),
            static () => LibraryUiPresetOneY);

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
                        description: ModSettingsText.Literal("显示或隐藏藏书库。")))
                .AddSection("library_ui", section => section
                    .WithTitle(ModSettingsText.Literal("藏书库界面"))
                    .AddChoice(
                        "library_ui_preset",
                        ModSettingsText.Literal("位置预设"),
                        LibraryUiPresetBinding,
                        [
                            new ModSettingsChoiceOption<string>(
                                LibraryUiPresetOne,
                                ModSettingsText.Literal("预设一（抽牌堆右方）")),
                            new ModSettingsChoiceOption<string>(
                                LibraryUiPresetTwo,
                                ModSettingsText.Literal("预设二（抽牌堆上方）")),
                            new ModSettingsChoiceOption<string>(
                                LibraryUiPresetCustom,
                                ModSettingsText.Literal("自定义")),
                        ],
                        description: ModSettingsText.Literal("选择预设，或通过下方 X/Y 滑条自定义位置。"),
                        presentation: ModSettingsChoicePresentation.Dropdown)
                    .AddIntSlider(
                        "library_ui_position_x",
                        ModSettingsText.Literal("位置 X"),
                        LibraryUiPositionXBinding,
                        LibraryUiPositionMinX,
                        LibraryUiPositionMaxX,
                        description: ModSettingsText.Literal("藏书库按钮的水平位置。"))
                    .AddIntSlider(
                        "library_ui_position_y",
                        ModSettingsText.Literal("位置 Y"),
                        LibraryUiPositionYBinding,
                        LibraryUiPositionMinY,
                        LibraryUiPositionMaxY,
                        description: ModSettingsText.Literal("藏书库按钮的垂直位置。"))
                    .WithEntryEnabledWhen("library_ui_position_x", IsCustomLibraryUiPresetSelected)
                    .WithEntryEnabledWhen("library_ui_position_y", IsCustomLibraryUiPresetSelected)));
    }

    public static Vector2 GetLibraryUiPosition()
    {
        var settings = Store.Get<KnowledgeDemonModSettings>(SettingsDataKey);
        return new Vector2(
            Math.Clamp(settings.LibraryUiPositionX, LibraryUiPositionMinX, LibraryUiPositionMaxX),
            Math.Clamp(settings.LibraryUiPositionY, LibraryUiPositionMinY, LibraryUiPositionMaxY));
    }

    public static string GetToggleLibraryHotkey()
    {
        return RuntimeHotkeyService.NormalizeOrDefault(
            ToggleLibraryHotkeyBinding.Read(),
            KnowledgeDemonRuntimeHotkeys.DefaultToggleLibraryBinding);
    }

    private static bool IsKnownPreset(string value) =>
        value is LibraryUiPresetOne or LibraryUiPresetTwo or LibraryUiPresetCustom;

    private static bool IsCustomLibraryUiPresetSelected() =>
        LibraryUiPresetBinding.Read() == LibraryUiPresetCustom;

    private static void ApplyPreset(KnowledgeDemonModSettings settings, string preset)
    {
        switch (preset)
        {
            case LibraryUiPresetOne:
                settings.LibraryUiPositionX = LibraryUiPresetOneX;
                settings.LibraryUiPositionY = LibraryUiPresetOneY;
                break;
            case LibraryUiPresetTwo:
                settings.LibraryUiPositionX = LibraryUiPresetTwoX;
                settings.LibraryUiPositionY = LibraryUiPresetTwoY;
                break;
        }
    }

    private static void ApplyLibraryUiPosition()
    {
        NLibraryPileButton.Instance?.ApplyConfiguredPosition();
    }

    private static void SynchronizePresetSliders(KnowledgeDemonModSettings settings)
    {
        _synchronizingPresetSliders = true;
        try
        {
            LibraryUiPositionXBinding.Write(settings.LibraryUiPositionX);
            LibraryUiPositionYBinding.Write(settings.LibraryUiPositionY);
        }
        finally
        {
            _synchronizingPresetSliders = false;
        }
    }
}
