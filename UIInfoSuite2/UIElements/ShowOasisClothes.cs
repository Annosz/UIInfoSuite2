using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.UIElements
{
  internal class ShowOasisClothes : IDisposable
  {
    #region Properties

    private int[] _valuableIds =
    {
            20, 23, 24, 41, 42, 44, 46, 47, 50, 55, 57, 61, 77, 81, 83, 85, 88, 91, 109, 110, 111, 119, 120, 121, 122
        };

    private Clothing? _clothingItem;

    private readonly PerScreen<bool> _shouldRenderItem = new();
    private readonly PerScreen<ClickableTextureComponent?> _icon = new();
    private readonly IModHelper _helper;

    private bool Enabled { get; set; }
    private bool ShowAllClothes { get; set; }

    #endregion

    #region Life cycle

    public ShowOasisClothes(IModHelper helper)
    {
      _helper = helper;
      ToggleOption(true);
    }

    public void Dispose()
    {
      ToggleOption(false);
    }

    public void ToggleOption(bool enabled)
    {
      Enabled = enabled;

      _helper.Events.Display.RenderingHud -= OnRenderingHud;
      _helper.Events.Display.RenderedHud -= OnRenderedHud;
      _helper.Events.Display.MenuChanged -= OnMenuChanged;
      _helper.Events.GameLoop.DayStarted -= OnDayStarted;
      _helper.Events.GameLoop.SaveLoaded -= OnSaveLoaded;

      if (enabled)
      {
        _helper.Events.GameLoop.DayStarted += OnDayStarted;
        _helper.Events.Display.RenderingHud += OnRenderingHud;
        _helper.Events.Display.RenderedHud += OnRenderedHud;
        _helper.Events.Display.MenuChanged += OnMenuChanged;
        _helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
      }

      UpdateOasisItem();
    }

    public void ToggleShowAllClothes(bool showAllClothes)
    {
      ShowAllClothes = showAllClothes;
      ToggleOption(Enabled);
    }

    #endregion

    #region Event subscriptions

    private void OnDayStarted(object sender, DayStartedEventArgs e)
    {
      UpdateOasisItem();
    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
      UpdateOasisItem();
    }

    private void OnMenuChanged(object sender, MenuChangedEventArgs e)
    {
      // Stop rendering if we visit Sandy
      if (e.NewMenu is not ShopMenu || Game1.currentLocation.Name != "SandyHouse") return;
      _shouldRenderItem.Value = false;
    }

    private void OnRenderingHud(object sender, RenderingHudEventArgs e)
    {
      if (Game1.eventUp || !_shouldRenderItem.Value) return;

      Point iconPosition = IconHandler.Handler.GetNewIconPosition();

      _icon.Value = new ClickableTextureComponent(
          new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
          null,
          Rectangle.Empty,
          1);
      _icon.Value.draw(Game1.spriteBatch);

      _clothingItem?.drawInMenu(
          Game1.spriteBatch,
          new Vector2(iconPosition.X - 12, iconPosition.Y - 12),
          1.25f
      );
    }

    private void OnRenderedHud(object sender, RenderedHudEventArgs e)
    {
      if (_clothingItem == null || !_shouldRenderItem.Value || Game1.IsFakedBlackScreen() ||
          !(_icon.Value?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false)) return;
      var formattedStr = GetHoverString();
      IClickableMenu.drawHoverText(Game1.spriteBatch, formattedStr, Game1.dialogueFont);
    }

    #endregion

    #region Logic

    private string GetHoverString()
    {
      return ShowAllClothes
          ? string.Format(_helper.SafeGetString(LanguageKeys.SandyClothingItemAll), _clothingItem?.displayName)
          : _helper.SafeGetString(LanguageKeys.SandyClothingItemRare);
    }

    private static bool HasVisitedDesert()
    {
      return Game1.player.eventsSeen.Contains("67");
    }

    private void UpdateOasisItem()
    {
      _clothingItem = GetClothingItem();
      _shouldRenderItem.Value = false;
      // Early escape if we don't have an item for some reason
      if (_clothingItem == null || !Enabled) return;

      // Check to make sure the store is open, and the desert is accessible
      if (!HasVisitedDesert())
        return;

      var isExclusive = _valuableIds.Contains(_clothingItem.ParentSheetIndex - 1000);
      if (isExclusive || ShowAllClothes)
      {
        _shouldRenderItem.Value = true;
      }
    }

    private static Clothing? GetClothingItem()
    {
      var oasisStock = GetOasisStock();
      return oasisStock?.Keys.FirstOrDefault(elem => elem is Clothing) as Clothing;
    }

    private static Dictionary<ISalable, int[]>? GetOasisStock()
    {
      var oasis = Game1.getLocationFromName("SandyHouse");
      if (oasis == null)
      {
        return null;
      }

      var getShopStockMethod =
          typeof(GameLocation).GetMethod("sandyShopStock", BindingFlags.Instance | BindingFlags.NonPublic);
      if (getShopStockMethod == null)
      {
        return null;
      }

      var ret = getShopStockMethod.Invoke(oasis, null);

      return ret as Dictionary<ISalable, int[]>;
    }

    #endregion
  }
}
