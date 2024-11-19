using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Tools;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Helpers;
using Object = StardewValley.Object;

namespace UIInfoSuite2.UIElements;

internal class ShowItemHoverInformation : IDisposable
{
  private readonly ClickableTextureComponent _bundleIcon = new(
    new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
    Game1.mouseCursors,
    new Rectangle(331, 374, 15, 14),
    3f
  );

  private readonly IModHelper _helper;

  private readonly PerScreen<Item?> _hoverItem = new();
  private readonly ClickableTextureComponent _museumIcon;

  private readonly ClickableTextureComponent _shippingBottomIcon = new(
    new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
    Game1.mouseCursors,
    new Rectangle(526, 218, 30, 22),
    1.2f
  );

  private readonly ClickableTextureComponent _shippingTopIcon = new(
    new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
    Game1.mouseCursors,
    new Rectangle(134, 236, 30, 15),
    1.2f
  );

  private LibraryMuseum _libraryMuseum;

  public ShowItemHoverInformation(IModHelper helper)
  {
    _helper = helper;

    NPC? gunther = Game1.getCharacterFromName("Gunther");
    if (gunther == null)
    {
      ModEntry.MonitorObject.Log(
        $"{GetType().Name}: Could not find Gunther in the game, creating a fake one for ourselves.",
        LogLevel.Warn
      );
      gunther = new NPC { Name = "Gunther", Age = 0, Sprite = new AnimatedSprite("Characters\\Gunther") };
    }

    _museumIcon = new ClickableTextureComponent(
      new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
      gunther.Sprite.Texture,
      gunther.GetHeadShot(),
      Game1.pixelZoom
    );
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showItemHoverInformation)
  {
    _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;
    _helper.Events.Display.RenderedHud -= OnRenderedHud;
    _helper.Events.Display.Rendering -= OnRendering;

    if (showItemHoverInformation)
    {
      _libraryMuseum = Game1.getLocationFromName("ArchaeologyHouse") as LibraryMuseum;

      _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
      _helper.Events.Display.RenderedHud += OnRenderedHud;
      _helper.Events.Display.Rendering += OnRendering;
    }
  }

  /// <summary>Raised before the game draws anything to the screen in a draw tick, as soon as the sprite batch is opened.</summary>
  /// <param name="sender">The event sender.</param>
  /// <param name="e">The event arguments.</param>
  private void OnRendering(object? sender, EventArgs e)
  {
    _hoverItem.Value = Tools.GetHoveredItem();
  }

  /// <summary>
  ///   Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the
  ///   screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open). Content drawn to the sprite batch
  ///   at this point will appear over the HUD.
  /// </summary>
  /// <param name="sender">The event sender.</param>
  /// <param name="e">The event arguments.</param>
  private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
  {
    // ModEntry.MonitorObject.Log(Game1.player.currentLocation.Name);
    if (Game1.activeClickableMenu == null)
    {
      DrawAdvancedTooltip(e.SpriteBatch);
    }
  }

  /// <summary>
  ///   Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered
  ///   to the screen.
  /// </summary>
  /// <param name="sender">The event sender.</param>
  /// <param name="e">The event arguments.</param>
  [EventPriority(EventPriority.Low)]
  private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
  {
    if (Game1.activeClickableMenu != null)
    {
      DrawAdvancedTooltip(e.SpriteBatch);
    }
  }

  private void DrawAdvancedTooltip(SpriteBatch spriteBatch)
  {
    if (_hoverItem.Value != null &&
        !(_hoverItem.Value is MeleeWeapon weapon && weapon.isScythe()) &&
        _hoverItem.Value is not FishingRod)
    {
      var hoveredObject = _hoverItem.Value as Object;

      int itemPrice = Tools.GetSellToStorePrice(_hoverItem.Value);

      var stackPrice = 0;
      if (itemPrice > 0 && _hoverItem.Value.Stack > 1)
      {
        stackPrice = itemPrice * _hoverItem.Value.Stack;
      }

      int cropPrice = Tools.GetHarvestPrice(_hoverItem.Value);

      bool notDonatedYet = _libraryMuseum.isItemSuitableForDonation(_hoverItem.Value);


      bool notShippedYet = hoveredObject != null &&
                           hoveredObject.countsForShippedCollection() &&
                           !Game1.player.basicShipped.ContainsKey(hoveredObject.ItemId) &&
                           hoveredObject.Type != "Fish" &&
                           hoveredObject.Category != Object.skillBooksCategory;

      string? requiredBundleName = null;
      Color? bundleColor = null;
      if (hoveredObject != null)
      {
        BundleRequiredItem? bundleDisplayData = BundleHelper.GetBundleItemIfNotDonated(hoveredObject);
        if (bundleDisplayData != null)
        {
          requiredBundleName = bundleDisplayData.Name;

          // TODO cache these colors so we're not doing it every time
          bundleColor = BundleHelper.GetRealColorFromIndex(bundleDisplayData.Id)?.Desaturate(0.35f);
        }
      }

      var drawPositionOffset = new Vector2();
      int windowWidth, windowHeight;

      var bundleHeaderWidth = 0;
      if (!string.IsNullOrEmpty(requiredBundleName))
      {
        // bundleHeaderWidth = ((bundleIcon.Width * 3 = 45) - 7 = 38) + 3 + bundleTextSize.X + 3 + ((shippingBin.Width * 1.2 = 36) - 12 = 24)
        bundleHeaderWidth = 68 + (int)Game1.dialogueFont.MeasureString(requiredBundleName).X;
      }

      var itemTextWidth = (int)Game1.smallFont.MeasureString(itemPrice.ToString()).X;
      var stackTextWidth = (int)Game1.smallFont.MeasureString(stackPrice.ToString()).X;
      var cropTextWidth = (int)Game1.smallFont.MeasureString(cropPrice.ToString()).X;
      var minTextWidth = (int)Game1.smallFont.MeasureString("000").X;
      // largestTextWidth = 12 + 4 + (icon.Width = 32) + 4 + max(textSize.X) + 8 + 16
      int largestTextWidth =
        76 + Math.Max(minTextWidth, Math.Max(stackTextWidth, Math.Max(itemTextWidth, cropTextWidth)));
      windowWidth = Math.Max(bundleHeaderWidth, largestTextWidth);

      windowHeight = 20 + 16;
      if (itemPrice > 0)
      {
        windowHeight += 40;
      }

      if (stackPrice > 0)
      {
        windowHeight += 40;
      }

      if (cropPrice > 0)
      {
        windowHeight += 40;
      }

      if (!string.IsNullOrEmpty(requiredBundleName))
      {
        windowHeight += 4;
        drawPositionOffset.Y += 4;
      }

      // Minimal window dimensions
      windowHeight = Math.Max(windowHeight, 40);
      windowWidth = Math.Max(windowWidth, 40);

      int windowY = Game1.getMouseY() + 20;
      int windowX = Game1.getMouseX() - 25 - windowWidth;

      // Adjust the tooltip's position when it overflows
      Rectangle safeArea = Utility.getSafeArea();

      if (windowY + windowHeight > safeArea.Bottom)
      {
        windowY = safeArea.Bottom - windowHeight;
      }

      if (Game1.getMouseX() + 300 > safeArea.Right)
      {
        windowX = safeArea.Right - 350 - windowWidth;
      }
      else if (windowX < safeArea.Left)
      {
        windowX = Game1.getMouseX() + 350;
      }

      var windowPos = new Vector2(windowX, windowY);
      Vector2 drawPosition = windowPos + new Vector2(16, 20) + drawPositionOffset;

      // Icons are drawn in 32x40 cells. The small font has a cap height of 18 and an offset of (2, 6)
      var rowHeight = 40;
      var iconCenterOffset = new Vector2(16, 20);
      var textOffset = new Vector2(32 + 4, (rowHeight - 18) / 2 - 6);

      if (itemPrice > 0 ||
          stackPrice > 0 ||
          cropPrice > 0 ||
          !string.IsNullOrEmpty(requiredBundleName) ||
          notDonatedYet ||
          notShippedYet)
      {
        IClickableMenu.drawTextureBox(
          spriteBatch,
          Game1.menuTexture,
          new Rectangle(0, 256, 60, 60),
          (int)windowPos.X,
          (int)windowPos.Y,
          windowWidth,
          windowHeight,
          Color.White
        );
      }

      if (itemPrice > 0)
      {
        spriteBatch.Draw(
          Game1.debrisSpriteSheet,
          drawPosition + iconCenterOffset,
          Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
          Color.White,
          0,
          new Vector2(8, 8),
          Game1.pixelZoom,
          SpriteEffects.None,
          0.95f
        );

        DrawSmallTextWithShadow(spriteBatch, itemPrice.ToString(), drawPosition + textOffset);

        drawPosition.Y += rowHeight;
      }

      if (stackPrice > 0)
      {
        var overlapOffset = new Vector2(0, 10);
        spriteBatch.Draw(
          Game1.debrisSpriteSheet,
          drawPosition + iconCenterOffset - overlapOffset / 2,
          Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
          Color.White,
          0,
          new Vector2(8, 8),
          Game1.pixelZoom,
          SpriteEffects.None,
          0.95f
        );
        spriteBatch.Draw(
          Game1.debrisSpriteSheet,
          drawPosition + iconCenterOffset + overlapOffset / 2,
          Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
          Color.White,
          0,
          new Vector2(8, 8),
          Game1.pixelZoom,
          SpriteEffects.None,
          0.95f
        );

        DrawSmallTextWithShadow(spriteBatch, stackPrice.ToString(), drawPosition + textOffset);

        drawPosition.Y += rowHeight;
      }

      if (cropPrice > 0)
      {
        spriteBatch.Draw(
          Game1.mouseCursors,
          drawPosition + iconCenterOffset,
          new Rectangle(60, 428, 10, 10),
          Color.White,
          0.0f,
          new Vector2(5, 5),
          Game1.pixelZoom * 0.75f,
          SpriteEffects.None,
          0.85f
        );

        DrawSmallTextWithShadow(spriteBatch, cropPrice.ToString(), drawPosition + textOffset);
      }

      if (notDonatedYet)
      {
        spriteBatch.Draw(
          _museumIcon.texture,
          windowPos + new Vector2(2, windowHeight + 8),
          _museumIcon.sourceRect,
          Color.White,
          0f,
          new Vector2(_museumIcon.sourceRect.Width / 2, _museumIcon.sourceRect.Height),
          2,
          SpriteEffects.None,
          0.86f
        );
      }

      if (!string.IsNullOrEmpty(requiredBundleName))
      {
        // Draws a 30x42 bundle icon offset by (-7, -13) from the top-left corner of the window
        // and the 36px high banner with the bundle name
        DrawBundleBanner(spriteBatch, requiredBundleName, windowPos + new Vector2(-7, -13), windowWidth, bundleColor);
      }

      if (notShippedYet)
      {
        // Draws a 36x28 shipping bin offset by (-24, -6) from the top-right corner of the window
        var shippingBinDims = new Vector2(30, 24);
        DrawShippingBin(spriteBatch, windowPos + new Vector2(windowWidth - 6, 8), shippingBinDims / 2);
      }
    }
  }

  private void DrawSmallTextWithShadow(SpriteBatch b, string text, Vector2 position)
  {
    b.DrawString(Game1.smallFont, text, position + new Vector2(2, 2), Game1.textShadowColor);
    b.DrawString(Game1.smallFont, text, position, Game1.textColor);
  }

  private void DrawBundleBanner(
    SpriteBatch spriteBatch,
    string bundleName,
    Vector2 position,
    int windowWidth,
    Color? color = null
  )
  {
    // NB The dialogue font has a cap height of 30 and an offset of (3, 6)

    Color drawColor = color ?? Color.Crimson;

    var bundleBannerX = (int)position.X;
    int bundleBannerY = (int)position.Y + 3;
    var cellCount = 36;
    var solidCells = 8;
    int cellWidth = windowWidth / cellCount;
    for (var cell = 0; cell < cellCount; ++cell)
    {
      float fadeAmount = 0.97f - (cell < solidCells ? 0 : 1.0f * (cell - solidCells) / (cellCount - solidCells));
      spriteBatch.Draw(
        Game1.staminaRect,
        new Rectangle(bundleBannerX + cell * cellWidth, bundleBannerY, cellWidth, 36),
        drawColor * fadeAmount
      );
    }

    spriteBatch.Draw(
      Game1.mouseCursors,
      position,
      _bundleIcon.sourceRect,
      Color.White,
      0f,
      Vector2.Zero,
      _bundleIcon.scale,
      SpriteEffects.None,
      0.86f
    );

    Utility.drawTextWithColoredShadow(
      spriteBatch,
      bundleName,
      Game1.dialogueFont,
      position + new Vector2(_bundleIcon.sourceRect.Width * _bundleIcon.scale + 3, 0),
      Color.Ivory,
      Color.DarkSlateGray,
      horizontalShadowOffset: 2,
      verticalShadowOffset: 2,
      numShadows: 3
    );
  }

  private void DrawShippingBin(SpriteBatch b, Vector2 position, Vector2 origin)
  {
    var shippingBinOffset = new Vector2(0, 2);
    // var shippingBinLidOffset = Vector2.Zero;

    // NB This is not the texture used to draw the shipping bin on the farm map.
    //    The one for the farm is located in "Buildings\Shipping Bin".
    b.Draw(
      _shippingBottomIcon.texture,
      position,
      _shippingBottomIcon.sourceRect,
      Color.White,
      0f,
      origin - shippingBinOffset,
      _shippingBottomIcon.scale,
      SpriteEffects.None,
      0.86f
    );
    b.Draw(
      _shippingTopIcon.texture,
      position,
      _shippingTopIcon.sourceRect,
      Color.White,
      0f,
      origin,
      _shippingTopIcon.scale,
      SpriteEffects.None,
      0.86f
    );
  }
}
