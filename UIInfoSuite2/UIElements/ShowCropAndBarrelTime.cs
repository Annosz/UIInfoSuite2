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
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Compatibility.CustomBush;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Helpers;
using Object = StardewValley.Object;

namespace UIInfoSuite2.UIElements;

internal class ShowCropAndBarrelTime : IDisposable
{
  private const int MAX_TREE_GROWTH_STAGE = 5;

  private static readonly List<Func<Building?, List<string>, bool>> BuildingDetailRenderers = new()
  {
    DetailRenderers.BuildingOutput
  };

  private static readonly List<Func<Object?, List<string>, bool>> MachineDetailRenderers = new()
  {
    DetailRenderers.MachineTime
  };

  private static readonly List<Func<TerrainFeature?, List<string>, bool>> TerrainDetailRenderers = new()
  {
    DetailRenderers.CropRender, DetailRenderers.TreeRender, DetailRenderers.FruitTreeRender, DetailRenderers.TeaBush
  };

  private readonly PerScreen<TerrainFeature?> _currentTerrain = new();
  private readonly PerScreen<Object?> _currentTile = new();
  private readonly PerScreen<Building?> _currentTileBuilding = new();

  private readonly IModHelper _helper;

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

    if (!showCropAndBarrelTimes)
    {
      return;
    }

    _helper.Events.Display.RenderingHud += OnRenderingHud;
    _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
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
    _currentTerrain.Value = null;

    Vector2 gamepadTile = Game1.player.CurrentTool != null
      ? Utility.snapToInt(Game1.player.GetToolLocation() / Game1.tileSize)
      : Utility.snapToInt(Game1.player.GetGrabTile());
    Vector2 mouseTile = Game1.currentCursorTile;

    Vector2 tile = Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0 ? gamepadTile : mouseTile;

    if (Game1.currentLocation == null)
    {
      return;
    }

    if (Game1.currentLocation.IsBuildableLocation())
    {
      _currentTileBuilding.Value = Game1.currentLocation.getBuildingAt(tile);
    }

    if (Game1.currentLocation.Objects?.TryGetValue(tile, out Object? currentObject) ?? false)
    {
      _currentTile.Value = currentObject;
    }

    if (Game1.currentLocation.terrainFeatures?.TryGetValue(tile, out TerrainFeature? terrain) ?? false)
    {
      _currentTerrain.Value = terrain;
    }

    if (_currentTile.Value is IndoorPot pot)
    {
      if (pot.hoeDirt.Value != null)
      {
        _currentTerrain.Value = pot.hoeDirt.Value;
      }

      if (pot.bush.Value != null)
      {
        _currentTerrain.Value = pot.bush.Value;
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

    List<string> lines = new();
    Vector2 tile = Vector2.Zero;
    Building? currentTileBuilding = _currentTileBuilding.Value;
    Object? currentTile = _currentTile.Value;
    TerrainFeature? terrain = _currentTerrain.Value;

    int overrideX = -1;
    int overrideY = -1;

    if (currentTileBuilding is not null)
    {
      foreach (Func<Building?, List<string>, bool> buildingDetailRenderer in BuildingDetailRenderers)
      {
        if (!buildingDetailRenderer(currentTileBuilding, lines))
        {
          continue;
        }

        Vector2 buildingTile = new(currentTileBuilding.tileX.Value, currentTileBuilding.tileY.Value);
        tile = Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(buildingTile * Game1.tileSize));
      }
    }

    if (currentTile is not null)
    {
      foreach (Func<Object?, List<string>, bool> machineDetailRenderer in MachineDetailRenderers)
      {
        if (machineDetailRenderer(currentTile, lines))
        {
          tile = Utility.ModifyCoordinatesForUIScale(
            Game1.GlobalToLocal(new Vector2(currentTile.TileLocation.X, currentTile.TileLocation.Y) * Game1.tileSize)
          );
        }
      }
    }

    if (terrain is not null)
    {
      foreach (Func<TerrainFeature, List<string>, bool> terrainDetailRenderer in TerrainDetailRenderers)
      {
        if (terrainDetailRenderer(terrain, lines))
        {
          tile = Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(terrain.Tile * Game1.tileSize));
        }
      }
    }

    if (lines.Count <= 0)
    {
      return;
    }

    if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
    {
      overrideX = (int)(tile.X + Utility.ModifyCoordinateForUIScale(32));
      overrideY = (int)(tile.Y + Utility.ModifyCoordinateForUIScale(32));
    }

    IClickableMenu.drawHoverText(
      Game1.spriteBatch,
      string.Join('\n', lines),
      Game1.smallFont,
      overrideX: overrideX,
      overrideY: overrideY
    );
  }

  private static string GetFertilizerString(HoeDirt dirtTile)
  {
    var fertilizerNames = new Dictionary<string, int>();
    // Ultimate Fertilizer Integration
    foreach (string fertilizerStr in dirtTile.fertilizer.Value.Split("|"))
    {
      string name = ItemRegistry.GetData(fertilizerStr)?.DisplayName ?? "Unknown Fertilizer";
      int count = fertilizerNames.GetOrDefault(name);
      fertilizerNames[name] = count + 1;
    }

    IEnumerable<string> formattedNames = fertilizerNames.OrderBy(kv => kv.Value)
                                                        .ThenBy(kv => kv.Key)
                                                        .Select(
                                                          kv =>
                                                          {
                                                            string quantityStr = kv.Value == 1 ? "" : $" x{kv.Value}";
                                                            return $"{kv.Key}{quantityStr}";
                                                          }
                                                        );
    return string.Join(",\n", formattedNames);
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
      case "Lumisteria.MtVapius.Birchtree":
        return I18n.VmvBirch();
      case "FlashShifter.StardewValleyExpandedCP_Birch_Tree":
        return I18n.SVEBirch();
      case "FlashShifter.StardewValleyExpandedCP_Fir_Tree":
        return I18n.SVEFir();
      default:
        return $"Unknown (#{treeType})";
    }
  }

  private static class DetailRenderers
  {
    private static string GetInfoStringForDrop(PossibleDroppedItem item)
    {
      (int nextDayToProduce, ParsedItemData? parsedItemData, float chance, string? _) = item;

      string chanceStr = 1.0f.Equals(chance) ? "" : $" ({chance * 100:2F}%)";
      int daysUntilReady = nextDayToProduce - Game1.dayOfMonth;
      return daysUntilReady <= 0
        ? $"{parsedItemData.DisplayName}: {I18n.ReadyToHarvest()}"
        : $"{parsedItemData.DisplayName}: {daysUntilReady} {I18n.Days()}{chanceStr}";
    }

    private static Dictionary<string, int> GetItemCountMap(List<Item?> items)
    {
      Dictionary<string, int> itemCounter = new();
      foreach (Item? outputItem in items)
      {
        if (outputItem is null)
        {
          continue;
        }

        int count = itemCounter.GetOrDefault(outputItem.DisplayName, 0) + outputItem.Stack;
        itemCounter[outputItem.DisplayName] = count;
      }

      return itemCounter;
    }

    public static bool BuildingOutput(Building? building, List<string> entries)
    {
      if (building is null)
      {
        return false;
      }

      List<Item?> inputItems = new();
      List<Item?> outputItems = new();
      MachineHelper.GetBuildingChestItems(building, inputItems, outputItems);

      Dictionary<string, int> inputItemsMap = GetItemCountMap(inputItems);
      Dictionary<string, int> outputItemsMap = GetItemCountMap(outputItems);

      if (inputItemsMap.Count > 0)
      {
        entries.Add($"{I18n.MachineProcessing()}:");
        foreach ((string displayName, int count) in inputItemsMap)
        {
          entries.Add($"{displayName} x{count}");
        }
      }

      if (outputItemsMap.Count <= 0)
      {
        return true;
      }

      if (inputItemsMap.Count > 0)
      {
        entries.Add("");
      }

      entries.Add($"{I18n.MachineDone()}:");
      foreach ((string displayName, int count) in outputItemsMap)
      {
        entries.Add($"{displayName} x{count}");
      }


      return true;
    }

    public static bool MachineTime(Object? tileObject, List<string> entries)
    {
      if (tileObject == null ||
          !tileObject.bigCraftable.Value ||
          tileObject.MinutesUntilReady <= 0 ||
          tileObject.heldObject.Value == null ||
          tileObject.Name == "Heater")
      {
        return false;
      }

      entries.Add(tileObject.heldObject.Value.DisplayName);
      if (tileObject is Cask cask)
      {
        entries.Add($"{(int)(cask.daysToMature.Value / cask.agingRate.Value)} {I18n.DaysToMature()}");
        return true;
      }

      int timeLeft = tileObject.MinutesUntilReady;
      int longTime = timeLeft / 60;
      string longText = I18n.Hours();
      int shortTime = timeLeft % 60;
      string shortText = I18n.Minutes();

      // 1600 minutes per day if you go to bed at 2am, more if you sleep early.
      if (timeLeft >= 1600)
      {
        // Unlike crops and casks, this is only an approximate number of days
        // because of how time works while sleeping. It's close enough though.
        longText = I18n.Days();
        longTime = timeLeft / 1600;

        shortText = I18n.Hours();
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

      StringBuilder builder = new();

      if (longTime > 0)
      {
        builder.Append($"{longTime} {longText}, ");
      }

      builder.Append($"{shortTime} {shortText}");
      entries.Add(builder.ToString());
      return true;
    }

    public static bool CropRender(TerrainFeature? terrain, List<string> entries)
    {
      if (terrain is not HoeDirt hoeDirt)
      {
        return false;
      }

      var fertilizerStr = "";
      // Special case, someone had a case where id "0" was set in fertilizer, which is weeds...
      // Weird, right?
      if (!string.IsNullOrEmpty(hoeDirt.fertilizer.Value) && !"0".Equals(hoeDirt.fertilizer.Value))
      {
        fertilizerStr = GetFertilizerString(hoeDirt);
      }

      if (hoeDirt.crop is not null && !hoeDirt.crop.dead.Value)
      {
        Crop crop = hoeDirt.crop;
        var daysLeft = 0;

        if (hoeDirt.crop.fullyGrown.Value && hoeDirt.crop.dayOfCurrentPhase.Value > 0)
        {
          daysLeft = hoeDirt.crop.dayOfCurrentPhase.Value;
        }
        else
        {
          for (int i = hoeDirt.crop.currentPhase.Value; i < hoeDirt.crop.phaseDays.Count - 1; i++)
          {
            daysLeft += hoeDirt.crop.phaseDays[i];
          }

          daysLeft -= hoeDirt.crop.dayOfCurrentPhase.Value;
        }


        string cropName = DropsHelper.GetCropHarvestName(crop);
        string daysLeftStr = daysLeft <= 0 ? I18n.ReadyToHarvest() : $"{daysLeft} {I18n.Days()}";
        entries.Add($"{cropName}: {daysLeftStr}");

        string waterStatus = hoeDirt.state.Value == 1 ? I18n.Watered() : I18n.NotWatered();
        entries.Add(waterStatus);

        if (!string.IsNullOrEmpty(fertilizerStr))
        {
          fertilizerStr = $"({I18n.With()} {fertilizerStr})";
        }
      }

      if (!string.IsNullOrEmpty(fertilizerStr))
      {
        entries.Add(fertilizerStr);
      }

      return true;
    }

    public static bool TreeRender(TerrainFeature? terrain, List<string> entries)
    {
      if (terrain is not Tree tree)
      {
        return false;
      }

      bool isStump = tree.stump.Value;
      string treeTypeName = GetTreeTypeName(tree.treeType.Value);
      string stumpText = isStump ? $" ({I18n.Stump()})" : "";
      entries.Add($"{treeTypeName}{I18n.Tree()}{stumpText}");

      if (tree.growthStage.Value >= MAX_TREE_GROWTH_STAGE)
      {
        return true;
      }

      entries.Add($"{I18n.Stage()} {tree.growthStage.Value} / {MAX_TREE_GROWTH_STAGE}");
      if (tree.fertilized.Value)
      {
        entries.Add($"({I18n.Fertilized()})");
      }

      return true;
    }

    public static bool FruitTreeRender(TerrainFeature? terrain, List<string> entries)
    {
      if (terrain is not FruitTree fruitTree)
      {
        return false;
      }

      FruitTreeInfo treeInfo = DropsHelper.GetFruitTreeInfo(fruitTree);
      entries.Add(treeInfo.TreeName);
      if (fruitTree.daysUntilMature.Value > 0)
      {
        entries.Add($"{fruitTree.daysUntilMature.Value} {I18n.DaysToMature()}");
        return true;
      }

      if (treeInfo.Items.Count <= 1)
      {
        return true;
      }

      entries.AddRange(treeInfo.Items.Select(GetInfoStringForDrop));
      return true;
    }

    public static bool TeaBush(TerrainFeature? terrain, List<string> entries)
    {
      if (terrain is not Bush bush || bush.size.Value != Bush.greenTeaBush)
      {
        return false;
      }

      var ageToMature = 20;
      bool willProduceThisSeason = Game1.season != Season.Winter;
      string bushName = ItemRegistry.GetData("(O)251").DisplayName;
      bool inProductionPeriod = Game1.dayOfMonth >= 22;
      int daysUntilProductionPeriod = inProductionPeriod ? 0 : 22 - Game1.dayOfMonth;
      List<PossibleDroppedItem> droppedItems = new();

      if (bush.tileSheetOffset.Value == 1)
      {
        droppedItems.Add(new PossibleDroppedItem(Game1.dayOfMonth, ItemRegistry.GetData("(O)815"), 1.0f));
      }
      else if (Game1.dayOfMonth >= 21 && Game1.dayOfMonth < 28)
      {
        droppedItems.Add(new PossibleDroppedItem(Game1.dayOfMonth + 1, ItemRegistry.GetData("(O)815"), 1.0f));
      }

      if (ApiManager.GetApi(ModCompat.CustomBush, out ICustomBushApi? customBushApi))
      {
        if (customBushApi.TryGetCustomBush(bush, out ICustomBush? customBushData, out string? id))
        {
          droppedItems.Clear();
          willProduceThisSeason = customBushData.Seasons.Contains(Game1.season);
          string displayName = customBushData.DisplayName;
          if (displayName.Contains("LocalizedText"))
          {
            displayName = TokenParser.ParseText(displayName);
          }

          bushName = $"{displayName} Bush";
          ageToMature = customBushData.AgeToProduce;
          inProductionPeriod = Game1.dayOfMonth >= customBushData.DayToBeginProducing;
          daysUntilProductionPeriod = inProductionPeriod ? 0 : 22 - Game1.dayOfMonth;

          if (customBushData.GetShakeOffItemIfReady(bush, out ParsedItemData? shakeOffItemData))
          {
            droppedItems.Add(new PossibleDroppedItem(Game1.dayOfMonth, shakeOffItemData, 1.0f, id));
          }
          else
          {
            droppedItems = customBushApi.GetCustomBushDropItems(customBushData, id);
          }
        }
      }

      entries.Add(bushName);
      bool isMature = bush.getAge() >= ageToMature;
      if (!isMature || !willProduceThisSeason)
      {
        if (!isMature)
        {
          entries.Add($"{ageToMature - bush.getAge()} {I18n.DaysToMature()}");
        }

        if (!willProduceThisSeason)
        {
          entries.Add(I18n.DoesNotProduceThisSeason());
        }

        return true;
      }

      // Too early in the season to produce
      if (!inProductionPeriod)
      {
        entries.Add($"{daysUntilProductionPeriod} {I18n.Days()}");
        return true;
      }

      entries.AddRange(droppedItems.Select(GetInfoStringForDrop));
      return true;
    }
  }
}
