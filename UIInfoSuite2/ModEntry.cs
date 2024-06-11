using System;
using System.Reflection;
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
    Reflection = helper.Reflection;
    MonitorObject = Monitor;
    I18n.Init(helper.Translation);

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
      name: () => "Show options in in-game menu",
      tooltip: () => "Enables an extra tab in the in-game menu where you can configure every options for this mod.",
      getValue: () => _modConfig.ShowOptionsTabInMenu,
      setValue: value => _modConfig.ShowOptionsTabInMenu = value
    );
    configMenu.AddTextOption(
      ModManifest,
      name: () => "Apply default settings from this save",
      tooltip: () => "New characters will inherit the settings for the mod from this save file.",
      getValue: () => _modConfig.ApplyDefaultSettingsFromThisSave,
      setValue: value => _modConfig.ApplyDefaultSettingsFromThisSave = value
    );
    configMenu.AddKeybindList(
      ModManifest,
      name: () => "Open calendar keybind",
      tooltip: () => "Opens the calendar tab.",
      getValue: () => _modConfig.OpenCalendarKeybind,
      setValue: value => _modConfig.OpenCalendarKeybind = value
    );
    configMenu.AddKeybindList(
      ModManifest,
      name: () => "Open quest board keybind",
      tooltip: () => "Opens the quest board.",
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
      if (_modConfig.OpenCalendarKeybind.JustPressed())
      {
        helper.Input.SuppressActiveKeybinds(_modConfig.OpenCalendarKeybind);
        ToggleBillboard(false);
      }
      else if (_modConfig.OpenQuestBoardKeybind.JustPressed())
      {
        helper.Input.SuppressActiveKeybinds(_modConfig.OpenQuestBoardKeybind);
        ToggleBillboard(true);
      }
    }
  }

  private static void ToggleBillboard(bool shouldShowDailyQuest)
  {
    if (Context.IsPlayerFree)
    {
      Game1.activeClickableMenu = new Billboard(shouldShowDailyQuest);
    }
    else if (Game1.activeClickableMenu is Billboard billboard)
    {
      bool isRequestedBillboardCurrentlyOpen =
        (bool)(
          typeof(Billboard)
            .GetField("dailyQuestBoard", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(billboard) ?? false
        ) == shouldShowDailyQuest;
      if (isRequestedBillboardCurrentlyOpen)
      {
        Game1.activeClickableMenu.exitThisMenu();
      }
    }
  }
#endregion
}
