using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics.CodeAnalysis;
using StardewValley;

namespace UIInfoSuite2.Compatibility.CustomBush;

/// <summary>Represents a distinct instance of a custom bush.</summary>
public interface ICustomBush
{
  /// <summary>Gets the condition pertaining to the bush's current season.</summary>
  public string? Condition { get; }

  /// <summary>Gets the bush data.</summary>
  public ICustomBushData Data { get; }

  /// <summary>Gets the custom bush's id.</summary>
  public string Id { get; }

  /// <summary>Gets a value indicating whether the bush is in season.</summary>
  public bool IsInSeason { get; }

  /// <summary>Gets the current shake off item (if any).</summary>
  public Item? Item { get; }

  /// <summary>Gets an offset to the bush sprite.</summary>
  public int SpriteOffset { get; }

  /// <summary>Gets the current bush stage.</summary>
  public ICustomBushStage Stage { get; }

  /// <summary>Gets the number of counted days in the stage.</summary>
  public int StageCounter { get; }

  /// <summary>Gets the bush's stage id.</summary>
  public string StageId { get; }

  /// <summary>Gets the bush's texture.</summary>
  public Texture2D Texture { get; }

  /// <summary>Tests a Game State Query condition, passing the relevant parameters.</summary>
  /// <param name="condition">The condition to test.</param>
  /// <returns>True if the condition is null or empty, or if the condition passes.</returns>
  bool TestCondition(string? condition);

  /// <summary>Try to produce an item drop.</summary>
  /// <param name="drop">The drop to produce.</param>
  /// <param name="item">The item produced from the drop.</param>
  /// <returns>True if the drop could be produced.</returns>
  bool TryProduceDrop(ICustomBushDrop drop, [NotNullWhen(true)] out Item? item);
}
