using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FruitTrees;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;

namespace UIInfoSuite2.Infrastructure.Helpers;

public record DropInfo(string? Condition, float Chance, string ItemId)
{
  public int? GetNextDay(bool includeToday)
  {
    return DropsHelper.GetNextDay(Condition, includeToday);
  }
}

public record PossibleDroppedItem(int NextDayToProduce, ParsedItemData Item, float Chance, string? CustomId = null)
{
  public bool ReadyToPick => Game1.dayOfMonth == NextDayToProduce;
}

public record FruitTreeInfo(string TreeName, List<PossibleDroppedItem> Items);

public static class DropsHelper
{
  private static readonly Dictionary<string, string> CropNamesCache = new();

  public static int? GetNextDay(string? condition, bool includeToday)
  {
    return string.IsNullOrEmpty(condition)
      ? Game1.dayOfMonth + (includeToday ? 0 : 1)
      : Tools.GetNextDayFromCondition(condition, includeToday);
  }

  public static int? GetLastDay(string? condition)
  {
    return Tools.GetLastDayFromCondition(condition);
  }

  public static string GetCropHarvestName(Crop crop)
  {
    if (crop.indexOfHarvest.Value is null)
    {
      return "Unknown Crop";
    }

    // If you look at Crop.cs in the decompiled sources, it seems that there's a special case for spring onions - that's what the =="1" is about.
    string itemId = crop.isWildSeedCrop() ? crop.whichForageCrop.Value : crop.indexOfHarvest.Value;
    if (crop.whichForageCrop.Value == "1")
    {
      itemId = "399";
    }

    if (CropNamesCache.TryGetValue(itemId, out string? harvestName))
    {
      return harvestName;
    }

    // Technically has the best compatibility for looking up items vs ItemRegistry.
    harvestName = new Object(itemId, 1).DisplayName;
    CropNamesCache.Add(itemId, harvestName);

    return harvestName;
  }

  public static List<PossibleDroppedItem> GetFruitTreeDropItems(FruitTree tree, bool includeToday = false)
  {
    return GetGenericDropItems(tree.GetData().Fruit, null, includeToday, "Fruit Tree", FruitTreeDropConverter);

    DropInfo FruitTreeDropConverter(FruitTreeFruitData input)
    {
      return new DropInfo(input.Condition, input.Chance, input.ItemId);
    }
  }

  public static FruitTreeInfo GetFruitTreeInfo(FruitTree tree, bool harvestIncludeToday = false)
  {
    var name = "Fruit Tree";
    List<PossibleDroppedItem> drops = GetFruitTreeDropItems(tree, harvestIncludeToday);
    if (drops.Count == 1)
    {
      name = $"{drops[0].Item.DisplayName}{I18n.Tree()}";
    }

    return new FruitTreeInfo(name, drops);
  }

  public static List<PossibleDroppedItem> GetGenericDropItems<T>(
    IEnumerable<T> drops,
    string? customId,
    bool includeToday,
    string displayName,
    Func<T, DropInfo> extractDropInfo
  )
  {
    List<PossibleDroppedItem> items = new();

    foreach (T drop in drops)
    {
      DropInfo dropInfo = extractDropInfo(drop);
      int? nextDay = GetNextDay(dropInfo.Condition, includeToday);
      int? lastDay = GetLastDay(dropInfo.Condition);

      if (!nextDay.HasValue)
      {
        if (!lastDay.HasValue)
        {
          ModEntry.MonitorObject.Log(
            $"Couldn't parse the next day the {displayName} will drop {dropInfo.ItemId}. Condition: {dropInfo.Condition}. Please report this error.",
            LogLevel.Error
          );
        }

        continue;
      }

      ParsedItemData? itemData = ItemRegistry.GetData(dropInfo.ItemId);
      if (itemData == null)
      {
        ModEntry.MonitorObject.Log(
          $"Couldn't parse the correct item {displayName} will drop. ItemId: {dropInfo.ItemId}. Please report this error.",
          LogLevel.Error
        );
        continue;
      }

      if (Game1.dayOfMonth == nextDay.Value && !includeToday)
      {
        continue;
      }

      items.Add(new PossibleDroppedItem(nextDay.Value, itemData, dropInfo.Chance, customId));
    }

    return items;
  }
}
