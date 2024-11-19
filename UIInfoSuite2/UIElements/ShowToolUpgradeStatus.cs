using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Tools;
using UIInfoSuite2.Infrastructure;

namespace UIInfoSuite2.UIElements;

internal class ShowToolUpgradeStatus : IDisposable
{
#region Logic
  private void UpdateToolInfo()
  {
    Tool toolBeingUpgraded = _toolBeingUpgraded.Value = Game1.player.toolBeingUpgraded.Value;

    if (toolBeingUpgraded == null)
    {
      return;
    }

    if (toolBeingUpgraded is Axe
        or Pickaxe
        or Hoe
        or WateringCan
        or Pan
        or GenericTool { IndexOfMenuItemView: >= 13 and <= 16 })
    {
      ParsedItemData? itemData = ItemRegistry.GetDataOrErrorItem(toolBeingUpgraded.QualifiedItemId);
      Texture2D? itemTexture = itemData.GetTexture();
      Rectangle itemTextureLocation = itemData.GetSourceRect();
      float scaleFactor = 40.0f / itemTextureLocation.Width;
      _toolUpgradeIcon.Value = new ClickableTextureComponent(
        new Rectangle(0, 0, 40, 40),
        itemTexture,
        itemTextureLocation,
        scaleFactor
      );
    }

    if (Game1.player.daysLeftForToolUpgrade.Value > 0)
    {
      _hoverText.Value = string.Format(
        I18n.DaysUntilToolIsUpgraded(),
        Game1.player.daysLeftForToolUpgrade.Value,
        toolBeingUpgraded.DisplayName
      );
    }
    else
    {
      _hoverText.Value = string.Format(I18n.ToolIsFinishedBeingUpgraded(), toolBeingUpgraded.DisplayName);
    }
  }
#endregion

#region Properties
  private readonly PerScreen<string> _hoverText = new();
  private readonly PerScreen<Tool?> _toolBeingUpgraded = new();

  private readonly PerScreen<ClickableTextureComponent> _toolUpgradeIcon = new(
    () =>
    {
      return new ClickableTextureComponent(
        new Rectangle(0, 0, 40, 40),
        Game1.mouseCursors,
        new Rectangle(322, 498, 12, 12),
        40 / 12f
      );
    }
  );

  private readonly IModHelper _helper;
#endregion


#region Life cycle
  public ShowToolUpgradeStatus(IModHelper helper)
  {
    _helper = helper;
  }

  public void Dispose()
  {
    ToggleOption(false);
    _toolBeingUpgraded.Value = null;
  }

  public void ToggleOption(bool showToolUpgradeStatus)
  {
    _helper.Events.Display.RenderingHud -= OnRenderingHud;
    _helper.Events.Display.RenderedHud -= OnRenderedHud;
    _helper.Events.GameLoop.DayStarted -= OnDayStarted;
    _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

    if (showToolUpgradeStatus)
    {
      UpdateToolInfo();
      _helper.Events.Display.RenderingHud += OnRenderingHud;
      _helper.Events.Display.RenderedHud += OnRenderedHud;
      _helper.Events.GameLoop.DayStarted += OnDayStarted;
      _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }
  }
#endregion


#region Event subscriptions
  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (e.IsOneSecond && _toolBeingUpgraded.Value != Game1.player.toolBeingUpgraded.Value)
    {
      UpdateToolInfo();
    }
  }

  private void OnDayStarted(object? sender, DayStartedEventArgs e)
  {
    UpdateToolInfo();
  }

  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    if (!UIElementUtils.IsRenderingNormally() || _toolBeingUpgraded.Value == null)
    {
      return;
    }

    Point iconPosition = IconHandler.Handler.GetNewIconPosition();
    _toolUpgradeIcon.Value.bounds.X = iconPosition.X;
    _toolUpgradeIcon.Value.bounds.Y = iconPosition.Y;

    _toolUpgradeIcon.Value.draw(e.SpriteBatch);
  }

  private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
  {
    // Show text on hover
    if (_toolBeingUpgraded.Value != null && _toolUpgradeIcon.Value.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
    {
      IClickableMenu.drawHoverText(Game1.spriteBatch, _hoverText.Value, Game1.dialogueFont);
    }
  }
#endregion
}
