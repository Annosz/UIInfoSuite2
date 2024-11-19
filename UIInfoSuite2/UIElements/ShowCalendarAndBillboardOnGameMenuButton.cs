using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure;

namespace UIInfoSuite2.UIElements;

internal class ShowCalendarAndBillboardOnGameMenuButton : IDisposable
{
#region Properties
  private readonly PerScreen<ClickableTextureComponent> _showBillboardButton = new(
    () => new ClickableTextureComponent(
      new Rectangle(0, 0, 99, 60),
      Game1.content.Load<Texture2D>(Path.Combine("Maps", "summer_town")),
      new Rectangle(122, 291, 35, 20),
      3f
    )
  );

  private readonly IModHelper _helper;

  private readonly PerScreen<Item?> _hoverItem = new();
  private readonly PerScreen<Item?> _heldItem = new();
#endregion

#region Lifecycle
  public ShowCalendarAndBillboardOnGameMenuButton(IModHelper helper)
  {
    _helper = helper;
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showCalendarAndBillboard)
  {
    ModEntry.RegisterCalendarAndQuestKeyBindings(_helper, showCalendarAndBillboard);

    _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;
    _helper.Events.Input.ButtonPressed -= OnButtonPressed;
    _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

    if (showCalendarAndBillboard)
    {
      _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
      _helper.Events.Input.ButtonPressed += OnButtonPressed;
      _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }
  }
#endregion


#region Event subscriptions
  private void OnUpdateTicked(object sender, EventArgs e)
  {
    // Get hovered and hold item
    _hoverItem.Value = Tools.GetHoveredItem();
    if (Game1.activeClickableMenu is not GameMenu gameMenu)
    {
      return;
    }

    List<IClickableMenu> menuList = gameMenu.pages;

    if (menuList[0] is InventoryPage)
    {
      _heldItem.Value = Game1.player.CursorSlotItem;
    }
  }

  private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
  {
    if (e.Button == SButton.MouseLeft)
    {
      ActivateBillboard();
    }
    else if (e.Button == SButton.ControllerA)
    {
      ActivateBillboard();
    }
  }

  private void OnRenderedActiveMenu(object sender, EventArgs e)
  {
    if (_hoverItem.Value == null &&
        Game1.activeClickableMenu is GameMenu gameMenu &&
        gameMenu.currentTab == 0 &&
        _heldItem.Value == null &&
        gameMenu.GetChildMenu() == null)
    {
      DrawBillboard();
    }
  }
#endregion


#region Logic
  private void DrawBillboard()
  {
    ClickableTextureComponent billboardButton = _showBillboardButton.Value;
    billboardButton.bounds.X = Game1.activeClickableMenu.xPositionOnScreen + Game1.activeClickableMenu.width - 160;
    billboardButton.bounds.Y = Game1.activeClickableMenu.yPositionOnScreen +
                               Game1.activeClickableMenu.height -
                               // For compatiblity with BiggerBackpack mod
                               (_helper.ModRegistry.IsLoaded("spacechase0.BiggerBackpack") ? 230 : 300);

    _showBillboardButton.Value = billboardButton;
    _showBillboardButton.Value.draw(Game1.spriteBatch);

    // Draw the mouse again to display it over the billboard
    Game1.activeClickableMenu.drawMouse(Game1.spriteBatch);

    if (_showBillboardButton.Value.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
    {
      string hoverText = Game1.getMouseX() <
                         _showBillboardButton.Value.bounds.X + _showBillboardButton.Value.bounds.Width / 2
        ? I18n.Calendar()
        : I18n.Billboard();
      IClickableMenu.drawHoverText(Game1.spriteBatch, hoverText, Game1.dialogueFont);
    }
  }

  private void ActivateBillboard()
  {
    if (Game1.activeClickableMenu is GameMenu gameMenu &&
        gameMenu.currentTab == 0 &&
        _heldItem.Value == null &&
        _showBillboardButton.Value.containsPoint(
          (int)Utility.ModifyCoordinateForUIScale(Game1.getMouseX()),
          (int)Utility.ModifyCoordinateForUIScale(Game1.getMouseY())
        ))
    {
      if (Game1.questOfTheDay != null && string.IsNullOrEmpty(Game1.questOfTheDay.currentObjective))
      {
        Game1.questOfTheDay.currentObjective = "wat?";
      }

      Game1.activeClickableMenu = new Billboard(
        !(Utility.ModifyCoordinateForUIScale(Game1.getMouseX()) <
          _showBillboardButton.Value.bounds.X + _showBillboardButton.Value.bounds.Width / 2)
      );
    }
  }
#endregion
}
