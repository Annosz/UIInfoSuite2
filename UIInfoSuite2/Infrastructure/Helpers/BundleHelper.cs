using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace UIInfoSuite2.Infrastructure.Helpers;

using BundleIngredientsCache = Dictionary<string, List<List<int>>>;

public record BundleRequiredItem(string Name, int BannerWidth, int Id, string QualifiedId, int Quality, int Count);

public record BundleKeyData(string Name, int Color);

internal static class BundleHelper
{
  private static readonly Dictionary<int, BundleKeyData> BundleIdToBundleKeyDataMap = new();

  public static BundleKeyData? GetBundleKeyDataFromIndex(int bundleIdx, bool forceRefresh = false)
  {
    PopulateBundleNameMappings(forceRefresh);
    return BundleIdToBundleKeyDataMap.GetValueOrDefault(bundleIdx);
  }

  public static int GetBundleColorFromIndex(int bundleIdx, bool forceRefresh = false)
  {
    PopulateBundleNameMappings(forceRefresh);
    return BundleIdToBundleKeyDataMap.GetValueOrDefault(bundleIdx)?.Color ?? 0;
  }

  public static Color? GetRealColorFromIndex(int bundleIdx, bool forceRefresh = false)
  {
    PopulateBundleNameMappings(forceRefresh);
    BundleKeyData? bundleData = BundleIdToBundleKeyDataMap.GetValueOrDefault(bundleIdx);
    if (bundleData == null)
    {
      return null;
    }

    return Bundle.getColorFromColorIndex(bundleData.Color);
  }

  private static int GetBundleBannerWidthForName(string bundleName)
  {
    return 68 + (int)Game1.dialogueFont.MeasureString(bundleName).X;
  }

  public static List<BundleRequiredItem>? GetBundleItemIfNotDonated(Item item)
  {
    if (item is not SObject donatedItem || donatedItem.bigCraftable.Value)
    {
      return null;
    }

    var communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");

    BundleIngredientsCache bundlesIngredientsInfo;
    try
    {
      IReflectedField<BundleIngredientsCache> bundlesIngredientsInfoField =
        ModEntry.Reflection.GetField<BundleIngredientsCache>(communityCenter, "bundlesIngredientsInfo");
      bundlesIngredientsInfo = bundlesIngredientsInfoField.GetValue();
    }
    catch (Exception e)
    {
      ModEntry.MonitorObject.Log("Failed to get bundles info", LogLevel.Error);
      ModEntry.MonitorObject.Log(e.ToString(), LogLevel.Error);
      return null;
    }


    List<BundleRequiredItem>? output;
    List<List<int>>? bundleRequiredItemsList;

    if (bundlesIngredientsInfo.TryGetValue(donatedItem.QualifiedItemId, out bundleRequiredItemsList))
    {
      output = GetBundleItemIfNotDonatedFromList(bundleRequiredItemsList, donatedItem);
      if (output != null)
      {
        return output;
      }
    }

    if (donatedItem.Category >= 0 ||
        !bundlesIngredientsInfo.TryGetValue(donatedItem.Category.ToString(), out bundleRequiredItemsList))
    {
      return null;
    }

    output = GetBundleItemIfNotDonatedFromList(bundleRequiredItemsList, donatedItem);
    return output ?? null;
  }

  private static List<BundleRequiredItem>? GetBundleItemIfNotDonatedFromList(List<List<int>>? lists, ISalable obj)
  {
    
    List<BundleRequiredItem> output = new();
    if (lists == null)
    {
      return output;
    }

    foreach (List<int> list in lists)
    {
      if (list.Count < 3 || obj.Quality < list[2])
      {
        continue;
      }
      BundleKeyData? bundleKeyData = GetBundleKeyDataFromIndex(list[0]);
      if (bundleKeyData == null)
      {
        continue;
      }

      output.Add(new BundleRequiredItem(
        bundleKeyData.Name,
        GetBundleBannerWidthForName(bundleKeyData.Name),
        list[0],
        obj.QualifiedItemId,
        obj.Quality,
        list[1]
        ));
    }

    return output;
  }

  public static void PopulateBundleNameMappings(bool force = false)
  {
    if (BundleIdToBundleKeyDataMap.Count != 0 && !force)
    {
      return;
    }

    BundleIdToBundleKeyDataMap.Clear();
    foreach (KeyValuePair<string, string> bundleInfo in Game1.netWorldState.Value.BundleData)
    {
      try
      {
        string[] bundleLocationInfo = bundleInfo.Key.Split('/');
        var bundleIdx = Convert.ToInt32(bundleLocationInfo[1]);
        string[] bundleContentsData = bundleInfo.Value.Split('/');
        string localizedName = bundleContentsData[6];
        var color = Convert.ToInt32(bundleContentsData[3]);
        BundleIdToBundleKeyDataMap[bundleIdx] = new BundleKeyData(localizedName, color);
      }
      catch (Exception)
      {
        ModEntry.MonitorObject.Log(
          $"Failed to parse info for bundle {bundleInfo.ToString()}, some information may be unavailable"
        );
      }
    }
  }
}
