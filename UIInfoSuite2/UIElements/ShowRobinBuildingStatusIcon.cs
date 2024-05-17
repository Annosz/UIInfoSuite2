using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.UIElements;

internal class ShowRobinBuildingStatusIcon : IDisposable
{
#region Properties
  private bool _IsBuildingInProgress;
  private Rectangle? _buildingIconSpriteLocation;
  private string _hoverText;
  private readonly PerScreen<ClickableTextureComponent> _buildingIcon = new();
  private Texture2D _robinIconSheet;

  private readonly IModHelper _helper;
#endregion

#region Lifecycle
  public ShowRobinBuildingStatusIcon(IModHelper helper)
  {
    _helper = helper;
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showRobinBuildingStatus)
  {
    _helper.Events.GameLoop.DayStarted -= OnDayStarted;
    _helper.Events.Display.RenderingHud -= OnRenderingHud;
    _helper.Events.Display.RenderedHud -= OnRenderedHud;
    _helper.Events.GameLoop.OneSecondUpdateTicked -= OnTickInRobinHouse;

    if (showRobinBuildingStatus)
    {
      UpdateRobinBuindingStatusData();

      _helper.Events.GameLoop.DayStarted += OnDayStarted;
      _helper.Events.Display.RenderingHud += OnRenderingHud;
      _helper.Events.Display.RenderedHud += OnRenderedHud;
      _helper.Events.GameLoop.OneSecondUpdateTicked += OnTickInRobinHouse;
    }
  }
#endregion

#region Event subscriptions

  public void OnTickInRobinHouse(object? sender, OneSecondUpdateTickedEventArgs e)
  {
    if (Game1.currentLocation?.Name != "ScienceHouse")
    {
      return;
    }
    UpdateRobinBuindingStatusData();
  }

  private void OnDayStarted(object sender, DayStartedEventArgs e)
  {
    UpdateRobinBuindingStatusData();
  }

  private void OnRenderingHud(object sender, RenderingHudEventArgs e)
  {
    // Draw icon
    if (UIElementUtils.IsRenderingNormally() && _IsBuildingInProgress && _buildingIconSpriteLocation.HasValue)
    {
      Point iconPosition = IconHandler.Handler.GetNewIconPosition();
      _buildingIcon.Value = new ClickableTextureComponent(
        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
        _robinIconSheet,
        _buildingIconSpriteLocation.Value,
        8 / 3f
      );
      _buildingIcon.Value.draw(Game1.spriteBatch);
    }
  }

  private void OnRenderedHud(object sender, RenderedHudEventArgs e)
  {
    // Show text on hover
    if (_IsBuildingInProgress &&
        (_buildingIcon.Value?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false) &&
        !string.IsNullOrEmpty(_hoverText))
    {
      IClickableMenu.drawHoverText(Game1.spriteBatch, _hoverText, Game1.dialogueFont);
    }
  }
#endregion

#region Logic
  private bool GetRobinMessage(out string hoverText)
  {
    int remainingDays = Game1.player.daysUntilHouseUpgrade.Value;

    if (remainingDays <= 0)
    {
      Building? building = Game1.GetBuildingUnderConstruction();

      if (building is not null)
      {
        if (building.daysOfConstructionLeft.Value > building.daysUntilUpgrade.Value)
        {
          hoverText = string.Format(
            _helper.SafeGetString(LanguageKeys.RobinBuildingStatus),
            building.daysOfConstructionLeft.Value
          );
          return true;
        }

        // Add another translation string for this?
        hoverText = string.Format(
          _helper.SafeGetString(LanguageKeys.RobinBuildingStatus),
          building.daysUntilUpgrade.Value
        );
        return true;
      }

      hoverText = string.Empty;
      return false;
    }

    hoverText = string.Format(_helper.SafeGetString(LanguageKeys.RobinHouseUpgradeStatus), remainingDays);
    return true;
  }

  private void UpdateRobinBuindingStatusData()
  {
    if (GetRobinMessage(out _hoverText))
    {
      _IsBuildingInProgress = true;
      FindRobinSpritesheet();
    }
    else
    {
      _IsBuildingInProgress = false;
    }
  }

  private void FindRobinSpritesheet()
  {
    Texture2D? foundTexture = Game1.getCharacterFromName("Robin")?.Sprite?.Texture;
    if (foundTexture != null)
    {
      _robinIconSheet = foundTexture;
    }
    else
    {
      ModEntry.MonitorObject.Log($"{GetType().Name}: Could not find Robin spritesheet.", LogLevel.Warn);
    }

    if (_robinIconSheet == null)
    {
      ModEntry.MonitorObject.Log($"{GetType().Name}: Could not find Robin spritesheet.", LogLevel.Warn);
    }

    _buildingIconSpriteLocation =
      new Rectangle(0, 195 + 1, 15, 15 - 1); // 1px edits for better alignment with other icons
  }
#endregion
}
