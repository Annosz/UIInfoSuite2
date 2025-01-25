using StardewValley;
using StardewValley.GameData;
using System.Collections.Generic;

namespace UIInfoSuite2.Compatibility.CustomBush;

/// <summary>Model used for custom bush data.</summary>
public interface ICustomBushData
{
  /// <summary>Gets a list of conditions where any have to match for the bush to produce items.</summary>
  public List<string> ConditionsToProduce { get; }

  /// <summary>Gets the description of the bush.</summary>
  public string Description { get; }

  /// <summary>Gets the display name of the bush.</summary>
  public string DisplayName { get; }

  /// <summary>Gets a unique identifier for the custom bush.</summary>
  public string Id { get; }

  /// <summary>Gets the initial bush stage.</summary>
  public string InitialStage { get; }

  /// <summary>Gets the rules which override the locations that custom bushes can be planted in.</summary>
  public List<PlantableRule> PlantableLocationRules { get; }

  /// <summary>Gets all the growth stages.</summary>
  public ICustomBushStages Stages { get; }

  /// <summary>Try to get the age to mature.</summary>
  /// <returns>The age in which <see cref="ConditionsToProduce"/> will return true; otherwise, 0.</returns>
  public int GetAgeToMature();

  /// <summary>Try to get the seasons.</summary>
  /// <returns>The seasons in which <see cref="ConditionsToProduce" /> will return true.</returns>
  public List<Season> GetSeasons();
}
