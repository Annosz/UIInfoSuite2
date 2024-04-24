using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Quests;
using StardewValley.WorldMaps;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Options;

namespace UIInfoSuite2.UIElements;

internal class LocationOfTownsfolk : IDisposable
{
#region Properties
  private SocialPage _socialPage = null!;
  private string[] _friendNames = null!;
  private readonly List<NPC> _townsfolk = new();
  private readonly List<OptionsCheckbox> _checkboxes = new();

  private readonly ModOptions _options;
  private readonly IModHelper _helper;

  private const int SocialPanelWidth = 190;
  private const int SocialPanelXOffset = 160;
#endregion

#region Lifecycle
  public LocationOfTownsfolk(IModHelper helper, ModOptions options)
  {
    _helper = helper;
    _options = options;
  }

  public void ToggleShowNPCLocationsOnMap(bool showLocations)
  {
    InitializeProperties();
    _helper.Events.Display.MenuChanged -= OnMenuChanged;
    _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu_DrawSocialPageOptions;
    _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu_DrawNPCLocationsOnMap;
    _helper.Events.Input.ButtonPressed -= OnButtonPressed_ForSocialPage;
    _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

    if (showLocations)
    {
      _helper.Events.Display.MenuChanged += OnMenuChanged;
      _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu_DrawSocialPageOptions;
      _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu_DrawNPCLocationsOnMap;
      _helper.Events.Input.ButtonPressed += OnButtonPressed_ForSocialPage;
      _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }
  }

  public void Dispose()
  {
    ToggleShowNPCLocationsOnMap(false);
  }
#endregion

#region Event subscriptions
  private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
  {
    InitializeProperties();
  }

  private void OnButtonPressed_ForSocialPage(object? sender, ButtonPressedEventArgs e)
  {
    if (Game1.activeClickableMenu is GameMenu &&
        e.Button is SButton.MouseLeft or SButton.ControllerA or SButton.ControllerX)
    {
      CheckSelectedBox(e);
    }
  }

  private void OnRenderedActiveMenu_DrawSocialPageOptions(object? sender, RenderedActiveMenuEventArgs e)
  {
    if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == GameMenu.socialTab)
    {
      DrawSocialPageOptions();
    }
  }

  private void OnRenderedActiveMenu_DrawNPCLocationsOnMap(object? sender, RenderedActiveMenuEventArgs e)
  {
    if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == GameMenu.mapTab)
    {
      DrawNPCLocationsOnMap(gameMenu);
    }
  }

  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (!e.IsOneSecond || (Context.IsSplitScreen && Context.ScreenId != 0))
    {
      return;
    }

    _townsfolk.Clear();

    // We shouldn't render if the RSV map is open, it already does its own NPC Tracking
    bool isRsvWorldMap =
      Game1.activeClickableMenu?.GetChildMenu()?.GetType().FullName?.Equals("RidgesideVillage.RSVWorldMap") ?? false;

    if (isRsvWorldMap)
    {
      ModEntry.MonitorObject.Log("Not Rendering Villagers, in RSV Map");
      return;
    }

    foreach (GameLocation? loc in Game1.locations)
    {
      foreach (NPC? character in loc.characters)
      {
        if (character.isVillager())
        {
          _townsfolk.Add(character);
        }
      }
    }
  }
#endregion

#region Logic
  private void InitializeProperties()
  {
    if (Game1.activeClickableMenu is GameMenu gameMenu)
    {
      foreach (IClickableMenu? menu in gameMenu.pages)
      {
        if (menu is SocialPage socialPage)
        {
          _socialPage = socialPage;
          _friendNames = socialPage.GetAllNpcs().Select(n => n.Name).ToArray();
          break;
        }
      }

      _checkboxes.Clear();
      for (var i = 0; i < _friendNames.Length; i++)
      {
        string friendName = _friendNames[i];
        var checkbox = new OptionsCheckbox("", i);
        if (Game1.player.friendshipData.ContainsKey(friendName))
        {
          // npc
          checkbox.greyedOut = false;
          checkbox.isChecked = _options.ShowLocationOfFriends.GetOrDefault(friendName, true);
        }
        else
        {
          // player
          checkbox.greyedOut = true;
          checkbox.isChecked = true;
        }

        _checkboxes.Add(checkbox);
      }
    }
  }

  private void CheckSelectedBox(ButtonPressedEventArgs e)
  {
    var slotPosition =
      (int)typeof(SocialPage).GetField("slotPosition", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(
        _socialPage
      )!;

    for (int i = slotPosition; i < slotPosition + 5; ++i)
    {
      OptionsCheckbox checkbox = _checkboxes[i];
      var rect = new Rectangle(checkbox.bounds.X, checkbox.bounds.Y, checkbox.bounds.Width, checkbox.bounds.Height);
      if (e.Button == SButton.ControllerX)
      {
        rect.Width += SocialPanelWidth + Game1.activeClickableMenu.width;
      }

      if (rect.Contains(
            (int)Utility.ModifyCoordinateForUIScale(Game1.getMouseX()),
            (int)Utility.ModifyCoordinateForUIScale(Game1.getMouseY())
          ) &&
          !checkbox.greyedOut)
      {
        checkbox.isChecked = !checkbox.isChecked;
        _options.ShowLocationOfFriends[_friendNames[checkbox.whichOption]] = checkbox.isChecked;
        Game1.playSound("drumkit6");
      }
    }
  }

  private void DrawSocialPageOptions()
  {
    Game1.drawDialogueBox(
      Game1.activeClickableMenu.xPositionOnScreen - SocialPanelXOffset,
      Game1.activeClickableMenu.yPositionOnScreen,
      SocialPanelWidth,
      Game1.activeClickableMenu.height,
      false,
      true
    );

    var slotPosition =
      (int)typeof(SocialPage).GetField("slotPosition", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(
        _socialPage
      )!;
    var yOffset = 0;

    for (int i = slotPosition; i < slotPosition + 5 && i < _friendNames.Length; ++i)
    {
      OptionsCheckbox checkbox = _checkboxes[i];
      checkbox.bounds.X = Game1.activeClickableMenu.xPositionOnScreen - 60;

      checkbox.bounds.Y = Game1.activeClickableMenu.yPositionOnScreen + 130 + yOffset;

      checkbox.draw(Game1.spriteBatch, 0, 0);
      yOffset += 112;
      Color color = checkbox.isChecked ? Color.White : Color.Gray;

      Game1.spriteBatch.Draw(
        Game1.mouseCursors,
        new Vector2(checkbox.bounds.X - 50, checkbox.bounds.Y),
        new Rectangle(80, 0, 16, 16),
        color,
        0.0f,
        Vector2.Zero,
        3f,
        SpriteEffects.None,
        1f
      );

      if (yOffset != 560)
      {
        // draw seperator line
        Game1.spriteBatch.Draw(
          Game1.staminaRect,
          new Rectangle(checkbox.bounds.X - 50, checkbox.bounds.Y + 72, SocialPanelWidth / 2 - 6, 4),
          Color.SaddleBrown
        );
        Game1.spriteBatch.Draw(
          Game1.staminaRect,
          new Rectangle(checkbox.bounds.X - 50, checkbox.bounds.Y + 76, SocialPanelWidth / 2 - 6, 4),
          Color.BurlyWood
        );
      }

      if (!Game1.options.hardwareCursor)
      {
        Game1.spriteBatch.Draw(
          Game1.mouseCursors,
          new Vector2(Game1.getMouseX(), Game1.getMouseY()),
          Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, Game1.mouseCursor, 16, 16),
          Color.White,
          0.0f,
          Vector2.Zero,
          Game1.pixelZoom + Game1.dialogueButtonScale / 150.0f,
          SpriteEffects.None,
          1f
        );
      }

      if (checkbox.bounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
      {
        IClickableMenu.drawHoverText(Game1.spriteBatch, "Track on map", Game1.dialogueFont);
      }
    }
  }

  private void DrawNPCLocationsOnMap(GameMenu gameMenu)
  {
    var namesToShow = new List<string>();
    foreach (NPC character in _townsfolk)
    {
      try
      {
        bool shouldDrawCharacter = Game1.player.friendshipData.ContainsKey(character.Name) &&
                                   _options.ShowLocationOfFriends.GetOrDefault(character.Name, true) &&
                                   character.id != -1;
        if (shouldDrawCharacter)
        {
          DrawNPC(character, namesToShow);
        }
      }
      catch (Exception ex)
      {
        ModEntry.MonitorObject.Log(ex.Message + Environment.NewLine + ex.StackTrace, LogLevel.Error);
      }
    }

    DrawNPCNames(namesToShow);

    //The cursor needs to show up in front of the character faces
    Tools.DrawMouseCursor();

    string? hoverText = ((MapPage)gameMenu.pages[gameMenu.currentTab]).hoverText;
    IClickableMenu.drawHoverText(Game1.spriteBatch, hoverText, Game1.smallFont);
  }

  private static void DrawNPC(NPC character, List<string> namesToShow)
  {
    // Compare with the game code - MapPage.drawMiniPortraits

    Vector2?
      location = GetMapCoordinatesForNPC(
        character
      ); // location is the absolute position or null if the npc is not on the map
    if (location is null)
    {
      return;
    }

    Rectangle headShot = character.GetHeadShot();
    MapAreaPosition? mapPosition =
      WorldMapManager.GetPositionData(
        Game1.player.currentLocation,
        new Point((int)location.Value.X, (int)location.Value.Y)
      ) ??
      WorldMapManager.GetPositionData(Game1.getFarm(), Point.Zero);
    MapRegion? mapRegion = mapPosition.Region;
    Rectangle mapBounds = mapRegion.GetMapPixelBounds();
    var offsetLocation = new Vector2(
      location.Value.X + mapBounds.X - headShot.Width,
      location.Value.Y + mapBounds.Y - headShot.Height
    );
    // NOTE!  This is the same as the game code, except that where we have 'headShot.Width', the game code has a constant 32.  I think that's
    //  because the player face they draw is 32x32.  So we're keeping to the spirit.

    Color color = character.CurrentDialogue.Count <= 0 ? Color.Gray : Color.White;
    var headShotScale = 2f;
    Game1.spriteBatch.Draw(
      character.Sprite.Texture,
      offsetLocation,
      headShot,
      color,
      0.0f,
      Vector2.Zero,
      headShotScale,
      SpriteEffects.None,
      1f
    );

    int mouseX = Game1.getMouseX();
    int mouseY = Game1.getMouseY();
    if (mouseX >= offsetLocation.X &&
        mouseX - offsetLocation.X <= headShot.Width * headShotScale &&
        mouseY >= offsetLocation.Y &&
        mouseY - offsetLocation.Y <= headShot.Height * headShotScale)
    {
      namesToShow.Add(character.displayName);
    }

    DrawQuestsForNPC(character, (int)offsetLocation.X, (int)offsetLocation.Y);
  }

  private static Vector2? GetMapCoordinatesForNPC(NPC character)
  {
    var playerNormalizedTile = new Point(Math.Max(0, Game1.player.TilePoint.X), Math.Max(0, Game1.player.TilePoint.Y));
    MapAreaPosition playerMapAreaPosition =
      WorldMapManager.GetPositionData(Game1.player.currentLocation, playerNormalizedTile) ??
      WorldMapManager.GetPositionData(Game1.getFarm(), Point.Zero);
    // ^^ Regarding that ?? clause...  If the player is in the farmhouse or barn or any building on the farm, GetPositionData is
    //  going to return null.  Thus the fallback to pretending the player is on the farm.  However, it seems to me that
    //  Game1.player.currentLocation.GetParentLocation() would be the safer long-term bet.  But rule number 1 of modding is this:
    //  the game code is always right, even when it's wrong.

    var characterNormalizedTile = new Point(Math.Max(0, character.TilePoint.X), Math.Max(0, character.TilePoint.Y));
    MapAreaPosition characterMapAreaPosition =
      WorldMapManager.GetPositionData(character.currentLocation, characterNormalizedTile);

    if (playerMapAreaPosition != null &&
        characterMapAreaPosition != null &&
        !(characterMapAreaPosition.Region.Id != playerMapAreaPosition.Region.Id))
    {
      return characterMapAreaPosition.GetMapPixelPosition(character.currentLocation, characterNormalizedTile);
    }

    return null;
  }

  private static void DrawQuestsForNPC(NPC character, int x, int y)
  {
    foreach (Quest? quest in Game1.player.questLog.Where(
               q => q.accepted.Value && q.dailyQuest.Value && !q.completed.Value
             ))
    {
      if ((quest is ItemDeliveryQuest idq && idq.target.Value == character.Name) ||
          (quest is SlayMonsterQuest smq && smq.target.Value == character.Name) ||
          (quest is FishingQuest fq && fq.target.Value == character.Name) ||
          (quest is ResourceCollectionQuest rq && rq.target.Value == character.Name))
      {
        Game1.spriteBatch.Draw(
          Game1.mouseCursors,
          new Vector2(x + 10, y - 12),
          new Rectangle(394, 495, 4, 10),
          Color.White,
          0.0f,
          Vector2.Zero,
          3f,
          SpriteEffects.None,
          1f
        );
      }
    }
  }

  private static void DrawNPCNames(List<string> namesToShow)
  {
    if (namesToShow.Count == 0)
    {
      return;
    }

    var text = new StringBuilder();
    var longestLength = 0;
    foreach (string name in namesToShow)
    {
      text.AppendLine(name);
      longestLength = Math.Max(longestLength, (int)Math.Ceiling(Game1.smallFont.MeasureString(name).Length()));
    }

    int windowHeight = Game1.smallFont.LineSpacing * namesToShow.Count + 25;
    var windowPos = new Vector2(Game1.getMouseX() + 40, Game1.getMouseY() - windowHeight);
    IClickableMenu.drawTextureBox(
      Game1.spriteBatch,
      (int)windowPos.X,
      (int)windowPos.Y,
      longestLength + 30,
      Game1.smallFont.LineSpacing * namesToShow.Count + 25,
      Color.White
    );

    Game1.spriteBatch.DrawString(
      Game1.smallFont,
      text,
      new Vector2(windowPos.X + 17, windowPos.Y + 17),
      Game1.textShadowColor
    );

    Game1.spriteBatch.DrawString(
      Game1.smallFont,
      text,
      new Vector2(windowPos.X + 15, windowPos.Y + 15),
      Game1.textColor
    );
  }
#endregion
}
