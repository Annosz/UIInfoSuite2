using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.GameData.Buildings;
using StardewValley.Objects;

namespace UIInfoSuite2.Infrastructure.Helpers;

public static class MachineHelper
{
  // Welcome to the fun section: Stuff I stole from Pathoschild
  // https://github.com/Pathoschild/StardewMods

  /*********
   ** Building data
   *********/
  /// <summary>Get the building chest names referenced by a building's item conversion rules.</summary>
  /// <param name="data">The building data.</param>
  /// <param name="inputChests">The input chest names found in the conversion rules.</param>
  /// <param name="outputChests">The output chest names found in the conversion rules.</param>
  public static void GetBuildingChestNames(BuildingData? data, ISet<string> inputChests, ISet<string> outputChests)
  {
    if (data?.ItemConversions?.Count is not > 0)
    {
      return;
    }

    foreach (BuildingItemConversion? rule in data.ItemConversions)
    {
      if (rule?.SourceChest is not null)
      {
        inputChests.Add(rule.SourceChest);
      }

      if (rule?.DestinationChest is not null)
      {
        outputChests.Add(rule.DestinationChest);
      }
    }
  }

  /// <summary>Get the building chest names referenced by a building's item conversion rules.</summary>
  /// <param name="data">The building data.</param>
  /// <param name="inputChests">The input chest names found in the conversion rules.</param>
  /// <param name="outputChests">The output chest names found in the conversion rules.</param>
  /// <returns>Returns whether any input or output chests were found.</returns>
  public static bool TryGetBuildingChestNames(
    BuildingData? data,
    out ISet<string> inputChests,
    out ISet<string> outputChests
  )
  {
    inputChests = new HashSet<string>();
    outputChests = new HashSet<string>();

    GetBuildingChestNames(data, inputChests, outputChests);

    return inputChests.Count > 0 || outputChests.Count > 0;
  }

  /// <summary>Get the building chests which match a set of chest names.</summary>
  /// <param name="building">The building whose chests to get.</param>
  /// <param name="chestNames">The chest names to match.</param>
  public static IEnumerable<Chest> GetBuildingChests(Building building, ISet<string> chestNames)
  {
    foreach (Chest chest in building.buildingChests)
    {
      if (chestNames.Contains(chest.Name))
      {
        yield return chest;
      }
    }
  }

  public static void GetBuildingChestItems(Building? building, List<Item?> inputItems, List<Item?> outputItems)
  {
    if (building is null)
    {
      return;
    }

    HashSet<string> inputChestNames = new();
    HashSet<string> outputChestNames = new();
    GetBuildingChestNames(building.GetData(), inputChestNames, outputChestNames);

    IEnumerable<Chest> inputChests = inputChestNames.Select(building.GetBuildingChest)
                                                    .Where(chest => chest is not null);
    IEnumerable<Chest> outputChests =
      outputChestNames.Select(building.GetBuildingChest).Where(chest => chest is not null);

    foreach (Chest chest in inputChests)
    {
      inputItems.AddRange(chest.Items);
    }

    foreach (Chest chest in outputChests)
    {
      outputItems.AddRange(chest.Items);
    }
  }


  /// <summary>
  ///   Get all output items from a building.
  /// </summary>
  /// <param name="building">The input building, nullable.</param>
  /// <param name="whichItems">Which chests to get items for.</param>
  /// <returns>All items from all output chests, or an empty list if the building is null.</returns>
  public static List<Item?> GetBuildingChestItems(
    Building? building,
    BuildingChestType whichItems = BuildingChestType.Chest
  )
  {
    List<Item?> items = new();
    if (building is null)
    {
      return items;
    }

    HashSet<string> inputChests = new();
    HashSet<string> outputChests = new();
    GetBuildingChestNames(building.GetData(), inputChests, outputChests);

    HashSet<string> chestsToGlob = new();

    if (whichItems is BuildingChestType.Chest or BuildingChestType.Load)
    {
      chestsToGlob.AddRange(inputChests);
    }

    if (whichItems is BuildingChestType.Chest or BuildingChestType.Collect)
    {
      chestsToGlob.AddRange(outputChests);
    }

    foreach (string chestName in chestsToGlob)
    {
      Chest? buildingChest = building.GetBuildingChest(chestName);
      if (buildingChest is null)
      {
        continue;
      }

      items.AddRange(buildingChest.Items);
    }

    return items;
  }
}
