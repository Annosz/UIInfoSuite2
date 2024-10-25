using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.AdditionalFeatures;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Compatibility.CustomBush;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Options;

namespace UIInfoSuite2;

public class ModEntry : Mod
{
  private static SkipIntro _skipIntro; // Needed so GC won't throw away object with subscriptions
  private static ModConfig _modConfig;

  private static EventHandler<ButtonsChangedEventArgs> _calendarAndQuestKeyBindingsHandler;

  private ModOptions _modOptions;
  private ModOptionsPageHandler _modOptionsPageHandler;

  public static IReflectionHelper Reflection { get; private set; } = null!;

  public static IMonitor MonitorObject { get; private set; } = null!;

#region Entry
  public override void Entry(IModHelper helper)
  {
    I18n.Init(helper.Translation);
    Reflection = helper.Reflection;
    MonitorObject = Monitor;

    _skipIntro = new SkipIntro(helper.Events);
    _modConfig = Helper.ReadConfig<ModConfig>();

    helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
    helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    helper.Events.GameLoop.Saved += OnSaved;
    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.Display.Rendering += IconHandler.Handler.Reset;

    IconHandler.Handler.IsQuestLogPermanent = helper.ModRegistry.IsLoaded("MolsonCAD.DeluxeJournal");
  }
#endregion

#region Generic mod config menu
  private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
  {
    SoundHelper.Instance.Initialize(Helper);

    // get Generic Mod Config Menu's API (if it's installed)
    var configMenu = ApiManager.TryRegisterApi<IGenericModConfigMenuApi>(Helper, ModCompat.Gmcm, "1.6.0");
    ApiManager.TryRegisterApi<ICustomBushApi>(Helper, ModCompat.CustomBush, "1.2.1", true);

    if (configMenu is null)
    {
      return;
    }

    // register mod
    configMenu.Register(ModManifest, () => _modConfig = new ModConfig(), () => Helper.WriteConfig(_modConfig));

    // add some config options
    configMenu.AddBoolOption(
      ModManifest,
      name: () => I18n.Bool_ShowOptionsTabInMenu_DisplayedName(),
      tooltip: () => I18n.Bool_ShowOptionsTabInMenu_Tooltip(),
      getValue: () => _modConfig.ShowOptionsTabInMenu,
      setValue: value => _modConfig.ShowOptionsTabInMenu = value
    );
    configMenu.AddTextOption(
      ModManifest,
      name: () => I18n.Text_ApplyDefaultSettingsFromThisSave_DisplayedName(),
      tooltip: () => I18n.Text_ApplyDefaultSettingsFromThisSave_Tooltip(),
      getValue: () => _modConfig.ApplyDefaultSettingsFromThisSave,
      setValue: value => _modConfig.ApplyDefaultSettingsFromThisSave = value
    );
    configMenu.AddKeybindList(
      ModManifest,
      name: () => I18n.Keybinds_OpenCalendarKeybind_DisplayedName(),
      tooltip: () => I18n.Keybinds_OpenCalendarKeybind_Tooltip(),
      getValue: () => _modConfig.OpenCalendarKeybind,
      setValue: value => _modConfig.OpenCalendarKeybind = value
    );
    configMenu.AddKeybindList(
      ModManifest,
      name: () => I18n.Keybinds_OpenQuestBoardKeybind_DisplayedName(),
      tooltip: () => I18n.Keybinds_OpenQuestBoardKeybind_Tooltip(),
      getValue: () => _modConfig.OpenQuestBoardKeybind,
      setValue: value => _modConfig.OpenQuestBoardKeybind = value
    );
  }
#endregion

#region Event subscriptions
  private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
  {
    // Unload if the main player quits.
    if (Context.ScreenId != 0)
    {
      return;
    }

    _modOptionsPageHandler?.Dispose();
    _modOptionsPageHandler = null;
  }

  private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
  {
    // Only load once for split screen.
    if (Context.ScreenId != 0)
    {
      return;
    }

    _modOptions = Helper.Data.ReadJsonFile<ModOptions>($"data/{Constants.SaveFolderName}.json") ??
                  Helper.Data.ReadJsonFile<ModOptions>($"data/{_modConfig.ApplyDefaultSettingsFromThisSave}.json") ??
                  new ModOptions();

    _modOptionsPageHandler = new ModOptionsPageHandler(Helper, _modOptions, _modConfig.ShowOptionsTabInMenu);
  }

  private void OnSaved(object sender, EventArgs e)
  {
    // Only save for the main player.
    if (Context.ScreenId != 0)
    {
      return;
    }

    Helper.Data.WriteJsonFile($"data/{Constants.SaveFolderName}.json", _modOptions);
  }

  public static void RegisterCalendarAndQuestKeyBindings(IModHelper helper, bool subscribe)
  {
    if (_calendarAndQuestKeyBindingsHandler == null)
    {
      _calendarAndQuestKeyBindingsHandler = (sender, e) => HandleCalendarAndQuestKeyBindings(helper);
    }

    helper.Events.Input.ButtonsChanged -= _calendarAndQuestKeyBindingsHandler;

    if (subscribe)
    {
      helper.Events.Input.ButtonsChanged += _calendarAndQuestKeyBindingsHandler;
    }
  }

  private static void HandleCalendarAndQuestKeyBindings(IModHelper helper)
  {
    if (_modConfig != null)
    {
      if (Context.IsPlayerFree && _modConfig.OpenCalendarKeybind.JustPressed())
      {
        helper.Input.SuppressActiveKeybinds(_modConfig.OpenCalendarKeybind);
        Game1.activeClickableMenu = new Billboard();
      }
      else if (Context.IsPlayerFree && _modConfig.OpenQuestBoardKeybind.JustPressed())
      {
        helper.Input.SuppressActiveKeybinds(_modConfig.OpenQuestBoardKeybind);
        Game1.RefreshQuestOfTheDay();
        Game1.activeClickableMenu = new Billboard(true);
      }
    }
  }
#endregion
}
