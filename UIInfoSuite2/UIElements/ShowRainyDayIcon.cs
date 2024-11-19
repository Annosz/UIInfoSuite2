using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure;

namespace UIInfoSuite2.UIElements;

internal class ShowRainyDayIcon : IDisposable
{
#region Properties
  private class LocationWeather
  {
    internal bool IsRainyTomorrow { get; set; }
    internal Rectangle? SpriteLocation { get; set; }
    internal string HoverText { get; set; }
    internal ClickableTextureComponent IconComponent { get; set; }
  }

  private readonly LocationWeather _valleyWeather = new();
  private readonly LocationWeather _islandWeather = new();
  private Texture2D _iconSheet;

  private Color[] _weatherIconColors;
  private const int WeatherSheetWidth = 15 * 4 + 18 * 3;
  private const int WeatherSheetHeight = 18;

  private readonly IModHelper _helper;
#endregion

#region Lifecycle
  public ShowRainyDayIcon(IModHelper helper)
  {
    _helper = helper;
    CreateTileSheet();
  }

  public void Dispose()
  {
    ToggleOption(false);
    _iconSheet.Dispose();
  }

  public void ToggleOption(bool showRainyDay)
  {
    _helper.Events.Display.RenderingHud -= OnRenderingHud;
    _helper.Events.Display.RenderedHud -= OnRenderedHud;

    if (showRainyDay)
    {
      _helper.Events.Display.RenderingHud += OnRenderingHud;
      _helper.Events.Display.RenderedHud += OnRenderedHud;
    }
  }
#endregion

#region Event subscriptions
  private void OnRenderingHud(object sender, RenderingHudEventArgs e)
  {
    GetWeatherIconSpriteLocation();

    if (!UIElementUtils.IsRenderingNormally())
    {
      return;
    }

    RenderLocationWeatherIcon(_valleyWeather);
    if (HasVisitedIsland())
    {
      RenderLocationWeatherIcon(_islandWeather);
    }
  }

  private void OnRenderedHud(object sender, RenderedHudEventArgs e)
  {
    // Show text on hover
    RenderWeatherHoverText(_valleyWeather);
    if (HasVisitedIsland())
    {
      RenderWeatherHoverText(_islandWeather);
    }
  }

  private void RenderLocationWeatherIcon(LocationWeather weather)
  {
    // Escape if we don't have that weather or a sprite
    if (!weather.IsRainyTomorrow || !weather.SpriteLocation.HasValue)
    {
      return;
    }

    // Draw icon
    Point iconPosition = IconHandler.Handler.GetNewIconPosition();
    weather.IconComponent = new ClickableTextureComponent(
      new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
      _iconSheet,
      weather.SpriteLocation.Value,
      8 / 3f
    );
    weather.IconComponent.draw(Game1.spriteBatch);
  }

  private static void RenderWeatherHoverText(LocationWeather weather)
  {
    bool hasMouse = weather.IconComponent?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false;
    bool hasText = !string.IsNullOrEmpty(weather.HoverText);
    if (weather.IsRainyTomorrow && hasMouse && hasText)
    {
      IClickableMenu.drawHoverText(Game1.spriteBatch, weather.HoverText, Game1.dialogueFont);
    }
  }
#endregion

#region Logic
  private static bool HasVisitedIsland()
  {
    return Game1.MasterPlayer.mailReceived.Contains("willyBoatFixed");
  }

  private static string GetWeatherForTomorrow()
  {
    var date = new WorldDate(Game1.Date);
    ++date.TotalDays;
    string tomorrowWeather = Game1.IsMasterGame
      ? Game1.weatherForTomorrow
      : Game1.netWorldState.Value.WeatherForTomorrow;
    return Game1.getWeatherModificationsForDate(date, tomorrowWeather);
  }

  private static string GetIslandWeatherForTomorrow()
  {
    return Game1.netWorldState.Value.GetWeatherForLocation("Island").WeatherForTomorrow;
  }

  /// <summary>
  ///   Creates a custom tilesheet for weather icons.
  ///   Meant to mimic the TV screen, which has a border around it, while the individual icons in the Cursors tilesheet
  ///   don't have a border
  ///   Extracts the border, and each individual weather icon and stitches them together into one separate sheet
  /// </summary>
  private void CreateTileSheet()
  {
    ModEntry.MonitorObject.Log("Setting up icon sheet", LogLevel.Info);
    // Setup Texture sheet as a copy, so as not to disturb existing sprites
    _iconSheet = new Texture2D(Game1.graphics.GraphicsDevice, WeatherSheetWidth, WeatherSheetHeight);
    _weatherIconColors = new Color[WeatherSheetWidth * WeatherSheetHeight];
    Texture2D weatherBorderTexture = Texture2D.FromFile(
      Game1.graphics.GraphicsDevice,
      Path.Combine(_helper.DirectoryPath, "assets", "weatherbox.png")
    );
    var weatherBorderColors = new Color[15 * 15];
    var cursorColors = new Color[Game1.mouseCursors.Width * Game1.mouseCursors.Height];
    var cursorColors_1_6 = new Color[Game1.mouseCursors_1_6.Width * Game1.mouseCursors_1_6.Height];
    var bounds = new Rectangle(0, 0, Game1.mouseCursors.Width, Game1.mouseCursors.Height);
    var bounds_1_6 = new Rectangle(0, 0, Game1.mouseCursors_1_6.Width, Game1.mouseCursors_1_6.Height);
    weatherBorderTexture.GetData(weatherBorderColors);
    Game1.mouseCursors.GetData(cursorColors);
    Game1.mouseCursors_1_6.GetData(cursorColors_1_6);
    var subTextureColors = new Color[15 * 15];

    // Copy over the bits we want
    // Border from TV screen
    Tools.GetSubTexture(
      subTextureColors,
      weatherBorderColors,
      new Rectangle(0, 0, 15, 15),
      new Rectangle(0, 0, 15, 15)
    );
    // Copy to each destination
    for (var i = 0; i < 4; i++)
    {
      Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(i * 15, 0, 15, 15));
    }

    // Add in expanded sprites for the island parrot
    Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(60, 0, 15, 15));
    Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(78, 0, 15, 15));
    Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(96, 0, 15, 15));

    subTextureColors = new Color[13 * 13];
    // Rainy Weather
    Tools.GetSubTexture(subTextureColors, cursorColors, bounds, new Rectangle(504, 333, 13, 13));
    Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(1, 1, 13, 13));
    Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(61, 1, 13, 13));

    // Stormy Weather
    Tools.GetSubTexture(subTextureColors, cursorColors, bounds, new Rectangle(426, 346, 13, 13));
    Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(16, 1, 13, 13));
    Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(79, 1, 13, 13));

    // Snowy Weather
    Tools.GetSubTexture(subTextureColors, cursorColors, bounds, new Rectangle(465, 346, 13, 13));
    Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(31, 1, 13, 13));

    // Green Rain
    Tools.GetSubTexture(subTextureColors, cursorColors_1_6, bounds_1_6, new Rectangle(178, 363, 13, 13));
    Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(46, 1, 13, 13));
    Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(97, 1, 13, 13));

    // Size of the parrot icon
    subTextureColors = new Color[9 * 14];
    // Tools.GetSubTexture(subTextureColors, cursorColors, bounds, new Rectangle(155, 148, 9, 14));
    Tools.GetSubTexture(subTextureColors, cursorColors, bounds, new Rectangle(146, 149, 9, 14));
    Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(69, 4, 9, 14), true);
    Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(87, 4, 9, 14), true);
    Tools.SetSubTexture(subTextureColors, _weatherIconColors, WeatherSheetWidth, new Rectangle(105, 4, 9, 14), true);

    _iconSheet.SetData(_weatherIconColors);
  }

  private void GetWeatherIconSpriteLocation()
  {
    SetValleyWeatherSprite();
    if (HasVisitedIsland())
    {
      SetIslandWeatherSprite();
    }
  }

  private void SetValleyWeatherSprite()
  {
    switch (GetWeatherForTomorrow())
    {
      case Game1.weather_sunny:
      case Game1.weather_debris:
      case Game1.weather_festival:
      case Game1.weather_wedding:
        _valleyWeather.IsRainyTomorrow = false;
        break;

      case Game1.weather_rain:
        _valleyWeather.IsRainyTomorrow = true;
        _valleyWeather.SpriteLocation = new Rectangle(0, 0, 15, 15);
        _valleyWeather.HoverText = I18n.RainNextDay();
        break;

      case Game1.weather_lightning:
        _valleyWeather.IsRainyTomorrow = true;
        _valleyWeather.SpriteLocation = new Rectangle(15, 0, 15, 15);
        _valleyWeather.HoverText = I18n.ThunderstormNextDay();
        break;

      case Game1.weather_snow:
        _valleyWeather.IsRainyTomorrow = true;
        _valleyWeather.SpriteLocation = new Rectangle(30, 0, 15, 15);
        _valleyWeather.HoverText = I18n.SnowNextDay();
        break;

      case Game1.weather_green_rain:
        _valleyWeather.IsRainyTomorrow = true;
        _valleyWeather.SpriteLocation = new Rectangle(45, 0, 15, 15);
        _valleyWeather.HoverText = I18n.RainNextDay();
        break;

      default:
        _valleyWeather.IsRainyTomorrow = false;
        break;
    }
  }

  private void SetIslandWeatherSprite()
  {
    switch (GetIslandWeatherForTomorrow())
    {
      case Game1.weather_rain:
        _islandWeather.IsRainyTomorrow = true;
        _islandWeather.SpriteLocation = new Rectangle(60, 0, 18, 18);
        _islandWeather.HoverText = I18n.IslandRainNextDay();
        break;

      case Game1.weather_lightning:
        _islandWeather.IsRainyTomorrow = true;
        _islandWeather.SpriteLocation = new Rectangle(78, 0, 18, 18);
        _islandWeather.HoverText = I18n.IslandThunderstormNextDay();
        break;

      case Game1.weather_green_rain:
        _islandWeather.IsRainyTomorrow = true;
        _islandWeather.SpriteLocation = new Rectangle(96, 0, 18, 18);
        _islandWeather.HoverText = I18n.IslandRainNextDay();
        break;

      default:
        _islandWeather.IsRainyTomorrow = false;
        break;
    }
  }
#endregion
}
