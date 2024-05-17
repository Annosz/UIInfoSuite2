using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;
using UIInfoSuite2.Infrastructure;

namespace UIInfoSuite2.Compatibility.CustomBush;

public record CustomBushDroppedItem(string BushId, int NextDayToProduce, ParsedItemData Item, float Chance)
{
  public bool ReadyToPick => Game1.dayOfMonth == NextDayToProduce;
}

internal static class CustomBushExtensions
{
  private const string ShakeOffItem = $"{ModCompat.CustomBush}/ShakeOff";

  public static bool GetShakeOffItemIfReady(
    this ICustomBush customBush,
    Bush bush,
    [NotNullWhen(true)] out ParsedItemData? item
  )
  {
    item = null;
    if (bush.size.Value != Bush.greenTeaBush)
    {
      return false;
    }

    if (!bush.modData.TryGetValue(ShakeOffItem, out string itemId))
    {
      return false;
    }

    item = ItemRegistry.GetData(itemId);
    return true;
  }

  public static List<CustomBushDroppedItem> GetCustomBushDropItems(
    this ICustomBushApi api,
    ICustomBush bush,
    string? id,
    bool includeToday = true
  )
  {
    List<CustomBushDroppedItem> items = new();

    if (id == null || string.IsNullOrEmpty(id))
    {
      return items;
    }

    api.TryGetDrops(id, out IList<ICustomBushDrop>? drops);
    if (drops == null)
    {
      return items;
    }

    foreach (ICustomBushDrop drop in drops)
    {
      int? nextDay = string.IsNullOrEmpty(drop.Condition)
        ? Game1.dayOfMonth + (includeToday ? 0 : 1)
        : Tools.GetNextDayFromCondition(drop.Condition, includeToday);
      int? lastDay = Tools.GetLastDayFromCondition(drop.Condition);
      // TODO this assumes that the only item in drop is ItemId. If RandomItemId is used, this will not work.
      ParsedItemData? itemData = ItemRegistry.GetData(drop.ItemId);
      if (!nextDay.HasValue)
      {
        if (!lastDay.HasValue)
        {
          ModEntry.MonitorObject.Log(
            $"Couldn't parse the next day the bush {bush.DisplayName} will drop {drop.ItemId}. Condition: {drop.Condition}. Please report this error.",
            LogLevel.Error
          );
        }

        continue;
      }

      if (itemData == null)
      {
        ModEntry.MonitorObject.Log(
          $"Couldn't parse the correct item {bush.DisplayName} will drop. ItemId: {drop.ItemId}. Please report this error.",
          LogLevel.Error
        );
        continue;
      }

      if (Game1.dayOfMonth == nextDay.Value && !includeToday)
      {
        continue;
      }

      items.Add(new CustomBushDroppedItem(id, nextDay.Value, itemData, drop.Chance));
    }

    return items;
  }
}
