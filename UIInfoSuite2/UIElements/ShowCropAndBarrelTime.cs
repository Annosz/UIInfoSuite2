using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;
using Object = StardewValley.Object;

namespace UIInfoSuite2.UIElements;

internal class ShowCropAndBarrelTime : IDisposable
{
  private const int MAX_TREE_GROWTH_STAGE = 5;

  private readonly PerScreen<Object> _currentTile = new();
  private readonly PerScreen<Building> _currentTileBuilding = new();
  private readonly IModHelper _helper;
  private readonly Dictionary<string, string> _indexOfCropNames = new();

  private readonly Dictionary<string, string> _indexOfDgaCropNames = new();
  private readonly PerScreen<TerrainFeature> _terrain = new();

  public ShowCropAndBarrelTime(IModHelper helper)
  {
    _helper = helper;
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showCropAndBarrelTimes)
  {
    _helper.Events.Display.RenderingHud -= OnRenderingHud;
    _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

    if (showCropAndBarrelTimes)
    {
      _helper.Events.Display.RenderingHud += OnRenderingHud;
      _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }
  }

  /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
  /// <param name="sender">The event sender.</param>
  /// <param name="e">The event arguments.</param>
  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (!e.IsMultipleOf(4))
    {
      return;
    }

    _currentTileBuilding.Value = null;
    _currentTile.Value = null;
    _terrain.Value = null;

    Vector2 gamepadTile = Game1.player.CurrentTool != null
      ? Utility.snapToInt(Game1.player.GetToolLocation() / Game1.tileSize)
      : Utility.snapToInt(Game1.player.GetGrabTile());
    Vector2 mouseTile = Game1.currentCursorTile;

    Vector2 tile = Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0 ? gamepadTile : mouseTile;

    if (Game1.currentLocation != null && Game1.currentLocation.IsBuildableLocation())
    {
      _currentTileBuilding.Value = Game1.currentLocation.getBuildingAt(tile);
    }

    if (Game1.currentLocation != null)
    {
      if (Game1.currentLocation.Objects != null &&
          Game1.currentLocation.Objects.TryGetValue(tile, out Object? currentObject))
      {
        _currentTile.Value = currentObject;
      }

      if (Game1.currentLocation.terrainFeatures != null &&
          Game1.currentLocation.terrainFeatures.TryGetValue(tile, out TerrainFeature? terrain))
      {
        _terrain.Value = terrain;
      }

      // Make sure that _terrain is null before overwriting it because Tea Saplings are added to terrainFeatures and not IndoorPot.bush
      if (_terrain.Value == null && _currentTile.Value is IndoorPot pot)
      {
        if (pot.hoeDirt.Value != null)
        {
          _terrain.Value = pot.hoeDirt.Value;
        }

        if (pot.bush.Value != null)
        {
          _terrain.Value = pot.bush.Value;
        }
      }
    }
  }

  /// <summary>
  ///   Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this
  ///   point (e.g. because a menu is open).
  /// </summary>
  /// <param name="sender">The event sender.</param>
  /// <param name="e">The event arguments.</param>
  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    if (Game1.activeClickableMenu != null)
    {
      return;
    }

    Building? currentTileBuilding = _currentTileBuilding.Value;
    Object? currentTile = _currentTile.Value;
    TerrainFeature? terrain = _terrain.Value;

    int overrideX = -1;
    int overrideY = -1;

    // draw hover tooltip
    var inputKey = 0;
    // TODO 1.6 <= The tooltip for Mill says:
    //     The Mill class is only used to preserve data from old save files. All mills were converted into plain Building instances based on the rules in Data/Buildings.
    //     The input and output items are now stored in Building.buildingChests with the 'Input' and 'Output' keys respectively.
    //   Perhaps this was written when the 'buildingChests' property was a dictionary.  Now it's a list, and there's no property on Chest or ChestData
    //   that indicates which chest is the input and which is the output...  I must be missing something.
    // if (currentTileBuilding != null && currentTileBuilding is Mill millBuilding && millBuilding.input.Value != null && !millBuilding.input.Value.isEmpty())
    if (currentTileBuilding != null &&
        currentTileBuilding.buildingChests.Count > inputKey &&
        !currentTileBuilding.buildingChests[inputKey].isEmpty())
    {
      var wheatCount = 0;
      var beetCount = 0;
      var unmilledriceCount = 0;

      foreach (Item item in currentTileBuilding.buildingChests[inputKey].Items)
      {
        if (item != null && !string.IsNullOrEmpty(item.Name))
        {
          switch (item.Name)
          {
            case "Wheat":
              wheatCount += item.Stack;
              break;
            case "Beet":
              beetCount += item.Stack;
              break;
            case "Unmilled Rice":
              unmilledriceCount += item.Stack;
              break;
          }
        }
      }

      var builder = new StringBuilder();

      if (wheatCount > 0)
      {
        builder.Append($"{ItemRegistry.GetData("(O)262").DisplayName}:{wheatCount}");
      }

      if (beetCount > 0)
      {
        if (wheatCount > 0)
        {
          builder.Append(Environment.NewLine);
        }

        builder.Append($"{ItemRegistry.GetData("(O)284").DisplayName}:{beetCount}");
      }

      if (unmilledriceCount > 0)
      {
        if (beetCount > 0 || wheatCount > 0)
        {
          builder.Append(Environment.NewLine);
        }

        builder.Append($"{ItemRegistry.GetData("(O)271").DisplayName}:{unmilledriceCount}");
      }

      if (builder.Length > 0)
      {
        if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
        {
          Vector2 tilePosition = Utility.ModifyCoordinatesForUIScale(
            Game1.GlobalToLocal(
              new Vector2(currentTileBuilding.tileX.Value, currentTileBuilding.tileY.Value) * Game1.tileSize
            )
          );
          overrideX = (int)(tilePosition.X + Utility.ModifyCoordinateForUIScale(32));
          overrideY = (int)(tilePosition.Y + Utility.ModifyCoordinateForUIScale(32));
        }

        IClickableMenu.drawHoverText(
          Game1.spriteBatch,
          builder.ToString(),
          Game1.smallFont,
          overrideX: overrideX,
          overrideY: overrideY
        );
      }
    }
    else if (currentTile != null && (!currentTile.bigCraftable.Value || currentTile.MinutesUntilReady > 0))
    {
      if (currentTile.bigCraftable.Value &&
          currentTile.MinutesUntilReady > 0 &&
          currentTile.heldObject.Value != null &&
          currentTile.Name != "Heater")
      {
        var hoverText = new StringBuilder();

        hoverText.AppendLine(currentTile.heldObject.Value.DisplayName);

        if (currentTile is Cask cask)
        {
          hoverText.Append((int)(cask.daysToMature.Value / cask.agingRate.Value))
                   .Append(" " + _helper.SafeGetString(LanguageKeys.DaysToMature));
        }
        else
        {
          int timeLeft = currentTile.MinutesUntilReady;
          int longTime = timeLeft / 60;
          string longText = LanguageKeys.Hours;
          int shortTime = timeLeft % 60;
          string shortText = LanguageKeys.Minutes;

          // 1600 minutes per day if you go to bed at 2am, more if you sleep early.
          if (timeLeft >= 1600)
          {
            // Unlike crops and casks, this is only an approximate number of days
            // because of how time works while sleeping. It's close enough though.
            longText = LanguageKeys.Days;
            longTime = timeLeft / 1600;

            shortText = LanguageKeys.Hours;
            shortTime = timeLeft % 1600;

            // Hours below 1200 are 60 minutes per hour. Overnight it's 100 minutes per hour.
            // We could just divide by 60 here but then you could see strange times like
            // "2 days, 25 hours".
            // This is a bit of a fudge since depending on the current time of day and when the
            // farmer goes to bed, the night might happen earlier or last longer, but it's just
            // an approximation; regardless the processing won't finish before tomorrow.
            if (shortTime <= 1200)
            {
              shortTime /= 60;
            }
            else
            {
              shortTime = 20 + (shortTime - 1200) / 100;
            }
          }

          if (longTime > 0)
          {
            hoverText.Append(longTime).Append(" ").Append(_helper.SafeGetString(longText)).Append(", ");
          }

          hoverText.Append(shortTime).Append(" ").Append(_helper.SafeGetString(shortText));
        }

        if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
        {
          Vector2 tilePosition = Utility.ModifyCoordinatesForUIScale(
            Game1.GlobalToLocal(new Vector2(currentTile.TileLocation.X, currentTile.TileLocation.Y) * Game1.tileSize)
          );
          overrideX = (int)(tilePosition.X + Utility.ModifyCoordinateForUIScale(32));
          overrideY = (int)(tilePosition.Y + Utility.ModifyCoordinateForUIScale(32));
        }

        IClickableMenu.drawHoverText(
          Game1.spriteBatch,
          hoverText.ToString(),
          Game1.smallFont,
          overrideX: overrideX,
          overrideY: overrideY
        );
      }
    }
    else if (terrain is not null)
    {
      if (terrain is HoeDirt hoeDirt)
      {
        if (hoeDirt.crop is not null && !hoeDirt.crop.dead.Value)
        {
          var num = 0;

          if (hoeDirt.crop.fullyGrown.Value && hoeDirt.crop.dayOfCurrentPhase.Value > 0)
          {
            num = hoeDirt.crop.dayOfCurrentPhase.Value;
          }
          else
          {
            for (var i = 0; i < hoeDirt.crop.phaseDays.Count - 1; ++i)
            {
              if (hoeDirt.crop.currentPhase.Value == i)
              {
                num -= hoeDirt.crop.dayOfCurrentPhase.Value;
              }

              if (hoeDirt.crop.currentPhase.Value <= i)
              {
                num += hoeDirt.crop.phaseDays[i];
              }
            }
          }

          string? harvestName = GetCropHarvestName(hoeDirt.crop);
          if (!string.IsNullOrEmpty(harvestName))
          {
            StringBuilder hoverText = new StringBuilder(harvestName).Append(": ");
            if (num > 0)
            {
              hoverText.Append(num).Append(" ").Append(_helper.SafeGetString(LanguageKeys.Days));
            }
            else
            {
              hoverText.Append(_helper.SafeGetString(LanguageKeys.ReadyToHarvest));
            }

            if (!string.IsNullOrEmpty(hoeDirt.fertilizer.Value))
            {
              string fertilizerName = ItemRegistry.GetData(hoeDirt.fertilizer.Value).DisplayName ?? "Unknown Fertilizer";
              string withText = _helper.SafeGetString(LanguageKeys.With);
              hoverText.Append($"\n({withText} {fertilizerName})");
            }

            if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
            {
              Vector2 tilePosition = Utility.ModifyCoordinatesForUIScale(
                Game1.GlobalToLocal(new Vector2(terrain.Tile.X, terrain.Tile.Y) * Game1.tileSize)
              );
              overrideX = (int)(tilePosition.X + Utility.ModifyCoordinateForUIScale(32));
              overrideY = (int)(tilePosition.Y + Utility.ModifyCoordinateForUIScale(32));
            }

            IClickableMenu.drawHoverText(
              Game1.spriteBatch,
              hoverText.ToString(),
              Game1.smallFont,
              overrideX: overrideX,
              overrideY: overrideY
            );
          }
        }
        else if (!string.IsNullOrEmpty(hoeDirt.fertilizer.Value))
        {
          string fertilizerName = ItemRegistry.GetData(hoeDirt.fertilizer.Value).DisplayName ?? "Unknown Fertilizer";
          string hoverText = fertilizerName;
          IClickableMenu.drawHoverText(
            Game1.spriteBatch,
            hoverText,
            Game1.smallFont,
            overrideX: overrideX,
            overrideY: overrideY
          );
        }
      }
      else if (terrain is Tree tree)
      {
        string treeTypeName = GetTreeTypeName(tree.treeType.Value);
        string treeText = I18n.Tree();
        var treeName = $"{treeTypeName} {treeText}";
        if (tree.stump.Value)
        {
          treeName += " (stump)";
        }

        string text;

        if (tree.growthStage.Value < MAX_TREE_GROWTH_STAGE)
        {
          text = $"{treeName}\n" + I18n.Stage() + $"{tree.growthStage.Value} / {MAX_TREE_GROWTH_STAGE}";

          if (tree.fertilized.Value)
          {
            string fertilizedText = I18n.Fertilized();
            text += $"\n({fertilizedText})";
          }
        }
        else
        {
          text = treeName;
        }

        if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
        {
          Vector2 tilePosition =
            Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(terrain.Tile * Game1.tileSize));
          overrideX = (int)(tilePosition.X + Utility.ModifyCoordinateForUIScale(32));
          overrideY = (int)(tilePosition.Y + Utility.ModifyCoordinateForUIScale(32));
        }

        IClickableMenu.drawHoverText(
          Game1.spriteBatch,
          text,
          Game1.smallFont,
          overrideX: overrideX,
          overrideY: overrideY
        );
      }
      else if (terrain is FruitTree fruitTree)
      {
        string itemIdOfFruit = fruitTree.GetData().Fruit.First().ItemId; // TODO 1.6: Might be broken because of more than one item.
        string fruitName = ItemRegistry.GetData(itemIdOfFruit).DisplayName ?? "Unknown";
        string treeText = _helper.SafeGetString(LanguageKeys.Tree);
        var treeName = $"{fruitName} {treeText}";

        string text;

        if (fruitTree.daysUntilMature.Value > 0)
        {
          string daysToMatureText = _helper.SafeGetString(LanguageKeys.DaysToMature);
          text = $"{treeName}\n{fruitTree.daysUntilMature.Value} {daysToMatureText}";
        }
        else
        {
          text = treeName;
        }

        if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
        {
          Vector2 tilePosition =
            Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(terrain.Tile * Game1.tileSize));
          overrideX = (int)(tilePosition.X + Utility.ModifyCoordinateForUIScale(32));
          overrideY = (int)(tilePosition.Y + Utility.ModifyCoordinateForUIScale(32));
        }

        IClickableMenu.drawHoverText(
          Game1.spriteBatch,
          text,
          Game1.smallFont,
          overrideX: overrideX,
          overrideY: overrideY
        );
      }
      else if (terrain is Bush bush)
      {
        // Tea saplings (which are actually bushes)
        if (bush.size.Value == Bush.greenTeaBush)
        {
          int teaAge = bush.getAge();
          if (teaAge < 20)
          {
            string text = ItemRegistry.GetData("(O)251").DisplayName // 251 <- Tea Sapling
                          +
                          $"\n{20 - teaAge} " +
                          _helper.SafeGetString(LanguageKeys.DaysToMature);

            if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
            {
              Vector2 tilePosition =
                Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(terrain.Tile * Game1.tileSize));
              overrideX = (int)(tilePosition.X + Utility.ModifyCoordinateForUIScale(32));
              overrideY = (int)(tilePosition.Y + Utility.ModifyCoordinateForUIScale(32));
            }

            IClickableMenu.drawHoverText(
              Game1.spriteBatch,
              text,
              Game1.smallFont,
              overrideX: overrideX,
              overrideY: overrideY
            );
          }
        }
      }
    }
  }

  // See: https://stardewvalleywiki.com/Trees
  private static string GetTreeTypeName(string treeType)
  {
    switch (treeType)
    {
      case "1":
        return I18n.Oak();
      case "2":
        return I18n.Maple();
      case "3":
        return I18n.Pine();
      case "6":
        return I18n.Palm();
      case "7":
        return I18n.Mushroom();
      case "8":
        return I18n.Mahogany();
      case "9":
        return I18n.PalmJungle();
      case "10":
        return I18n.GreenRainType1();
      case "11":
        return I18n.GreenRainType2();
      case "12":
        return I18n.GreenRainType3();
      case "13":
        return I18n.Mystic();
      default:
        return $"Unknown (#{treeType})";
    }
  }

  private string? GetCropHarvestName(Crop crop)
  {
    if (crop.indexOfHarvest.Value is not null)
    {
      // If you look at Crop.cs in the decompiled sources, it seems that there's a special case for spring onions - that's what the =="1" is about.
      string itemId = crop.whichForageCrop.Value == "1" ? "399" :
        crop.isWildSeedCrop() ? crop.whichForageCrop.Value : crop.indexOfHarvest.Value;
      if (!_indexOfCropNames.TryGetValue(itemId, out string? harvestName))
      {
        harvestName = new Object(itemId, 1).DisplayName;
        _indexOfCropNames.Add(itemId, harvestName);
      }

      return harvestName;
    }

    if (ModEntry.DGA.IsCustomCrop(crop, out DynamicGameAssetsHelper? dgaHelper))
    {
      string? cropId = null;
      try
      {
        cropId = dgaHelper.GetFullId(crop)!;
        if (!_indexOfDgaCropNames.TryGetValue(cropId, out string? harvestName))
        {
          Object? harvestCrop = dgaHelper.GetCropHarvest(crop);
          if (harvestCrop == null)
          {
            return null;
          }

          harvestName = harvestCrop.DisplayName;
          _indexOfDgaCropNames.Add(cropId, harvestName);
        }

        return harvestName;
      }
      catch (Exception e)
      {
        ModEntry.MonitorObject.LogOnce(
          $"An error occured while retrieving the crop harvest name for {cropId ?? "unknownCrop"}.",
          LogLevel.Error
        );
        ModEntry.MonitorObject.Log(e.ToString(), LogLevel.Debug);
        return null;
      }
    }

    return null;
  }
}
