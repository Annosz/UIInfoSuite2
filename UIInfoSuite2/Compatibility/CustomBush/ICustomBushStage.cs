using Microsoft.Xna.Framework;

namespace UIInfoSuite2.Compatibility.CustomBush;

/// <summary>Represents a stage that a custom bush can change into.</summary>
public interface ICustomBushStage
{
  /// <summary>Gets the bush type for the growth stage.</summary>
  public BushType BushType { get; }

  /// <summary>A game state query which indicates whether the bush should increment the days in its current stage.</summary>
  public string? ConditionToProgress { get; }

  /// <summary>Gets the default texture used when planted indoors.</summary>
  public string IndoorTexture { get; }

  /// <summary>Gets or sets the items produced by this custom bush.</summary>
  public ICustomBushDrops ItemsProduced { get; }

  /// <summary>Gets the rules for progressing.</summary>
  public ICustomBushProgressRules ProgressRules { get; }

  /// <summary>Gets the coordinates for the sprite at this stage.</summary>
  public Point SpritePosition { get; }

  /// <summary>Gets the texture of the tea bush.</summary>
  public string Texture { get; }
}
