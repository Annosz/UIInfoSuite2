using System.Diagnostics.CodeAnalysis;
using StardewValley.TerrainFeatures;

namespace UIInfoSuite2.Compatibility.CustomBush;

/// <summary>Mod API for Custom Bush.</summary>
public interface ICustomBushApi
{
  /// <summary>Determine if the bush is a custom bush.</summary>
  /// <param name="bush">The bush to check.</param>
  /// <returns>True if the bush is a custom bush.</returns>
  public bool IsCustomBush(Bush bush);

  /// <summary>Try to get the custom bush instance associated with the given bush.</summary>
  /// <param name="bush">The bush to check.</param>
  /// <param name="customBush">The resulting custom bush, if applicable.</param>
  /// <returns>True if a custom bush was found.</returns>
  public bool TryGetBush(Bush bush, [NotNullWhen(true)] out ICustomBush? customBush);

  /// <summary>Try to get the custom bush model associated with the given bush.</summary>
  /// <param name="bush">The bush to check.</param>
  /// <param name="customBushData">The resulting custom bush, if applicable.</param>
  /// <returns>True if a custom bush was found.</returns>
  public bool TryGetData(Bush bush, [NotNullWhen(true)] out ICustomBushData? customBushData);
}
