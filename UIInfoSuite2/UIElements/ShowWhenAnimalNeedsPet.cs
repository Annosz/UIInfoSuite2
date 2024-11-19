using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData.FarmAnimals;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Network;

namespace UIInfoSuite2.UIElements;

internal class ShowWhenAnimalNeedsPet : IDisposable
{
#region Properties
  private readonly PerScreen<float> _yMovementPerDraw = new();
  private readonly PerScreen<float> _alpha = new();

  private bool Enabled { get; set; }
  private bool HideOnMaxFriendship { get; set; }

  private readonly IModHelper _helper;
#endregion


#region Lifecycle
  public ShowWhenAnimalNeedsPet(IModHelper helper)
  {
    _helper = helper;
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showWhenAnimalNeedsPet)
  {
    Enabled = showWhenAnimalNeedsPet;

    _helper.Events.Player.Warped -= OnWarped;
    _helper.Events.Display.RenderingHud -= OnRenderingHud_DrawAnimalHasProduct;
    _helper.Events.Display.RenderingHud -= OnRenderingHud_DrawNeedsPetTooltip;
    _helper.Events.GameLoop.UpdateTicked -= UpdateTicked;

    if (showWhenAnimalNeedsPet)
    {
      _helper.Events.Player.Warped += OnWarped;
      _helper.Events.Display.RenderingHud += OnRenderingHud_DrawAnimalHasProduct;
      _helper.Events.Display.RenderingHud += OnRenderingHud_DrawNeedsPetTooltip;
      _helper.Events.GameLoop.UpdateTicked += UpdateTicked;
    }
  }

  public void ToggleDisableOnMaxFriendshipOption(bool hideOnMaxFriendship)
  {
    HideOnMaxFriendship = hideOnMaxFriendship;
    ToggleOption(Enabled);
  }
#endregion


#region Event subscriptions
  private void OnWarped(object? sender, WarpedEventArgs e) { }

  private void OnRenderingHud_DrawNeedsPetTooltip(object? sender, RenderingHudEventArgs e)
  {
    if (UIElementUtils.IsRenderingNormally() &&
        Game1.activeClickableMenu == null &&
        (Game1.currentLocation is AnimalHouse || Game1.currentLocation is Farm || Game1.currentLocation is FarmHouse))
    {
      DrawIconForFarmAnimals();
      DrawIconForPets();
    }
  }

  private void OnRenderingHud_DrawAnimalHasProduct(object? sender, RenderingHudEventArgs e)
  {
    if (UIElementUtils.IsRenderingNormally() &&
        Game1.activeClickableMenu == null &&
        (Game1.currentLocation is AnimalHouse || Game1.currentLocation is Farm))
    {
      DrawAnimalHasProduct();
    }
  }

  private void UpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (!UIElementUtils.IsRenderingNormally() ||
        Game1.activeClickableMenu != null ||
        !(Game1.currentLocation is AnimalHouse || Game1.currentLocation is Farm || Game1.currentLocation is FarmHouse))
    {
      return;
    }

    var sine = (float)Math.Sin(e.Ticks / 20.0);
    _yMovementPerDraw.Value = -6f + 6f * sine;
    _alpha.Value = 0.8f + 0.2f * sine;
  }
#endregion

#region Logic
  private void DrawAnimalHasProduct()
  {
    NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>>? animalsInCurrentLocation = GetAnimalsInCurrentLocation();
    if (animalsInCurrentLocation == null)
    {
      return;
    }

    foreach (KeyValuePair<long, FarmAnimal> animal in animalsInCurrentLocation.Pairs)
    {
      FarmAnimalHarvestType? harvestType = animal.Value.GetHarvestType();
      if (harvestType == FarmAnimalHarvestType.DropOvernight ||
          animal.Value.IsEmoting ||
          animal.Value.currentProduce.Value == "430" || // 430 is truffle
          animal.Value.currentProduce.Value == null ||
          animal.Value.age.Value < animal.Value.GetAnimalData().DaysToMature)
      {
        continue;
      }

      Vector2 positionAboveAnimal = GetPetPositionAboveAnimal(animal.Value);
      positionAboveAnimal.Y += (float)(Math.Sin(
                                         Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 300.0 +
                                         animal.Value.Name.GetHashCode()
                                       ) *
                                       5.0);
      Game1.spriteBatch.Draw(
        Game1.emoteSpriteSheet,
        Utility.ModifyCoordinatesForUIScale(new Vector2(positionAboveAnimal.X + 14f, positionAboveAnimal.Y)),
        new Rectangle(
          3 * (Game1.tileSize / 4) % Game1.emoteSpriteSheet.Width,
          3 * (Game1.tileSize / 4) / Game1.emoteSpriteSheet.Width * (Game1.tileSize / 4),
          Game1.tileSize / 4,
          Game1.tileSize / 4
        ),
        Color.White * 0.9f,
        0.0f,
        Vector2.Zero,
        4f,
        SpriteEffects.None,
        1f
      );

      string produceItemId = animal.Value.currentProduce.Value;
      ParsedItemData? produceData = ItemRegistry.GetData(produceItemId);
      Rectangle sourceRectangle = produceData.GetSourceRect();
      Game1.spriteBatch.Draw(
        produceData.GetTexture(),
        Utility.ModifyCoordinatesForUIScale(new Vector2(positionAboveAnimal.X + 28f, positionAboveAnimal.Y + 8f)),
        sourceRectangle,
        Color.White * 0.9f,
        0.0f,
        Vector2.Zero,
        2.2f,
        SpriteEffects.None,
        1f
      );
    }
  }

  private void DrawIconForFarmAnimals()
  {
    NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>>? animalsInCurrentLocation = GetAnimalsInCurrentLocation();

    if (animalsInCurrentLocation == null)
    {
      return;
    }

    foreach (KeyValuePair<long, FarmAnimal> animal in animalsInCurrentLocation.Pairs)
    {
      if (animal.Value.IsEmoting ||
          animal.Value.wasPet.Value ||
          (animal.Value.friendshipTowardFarmer.Value >= 1000 && HideOnMaxFriendship))
      {
        continue;
      }

      Vector2 positionAboveAnimal = GetPetPositionAboveAnimal(animal.Value);
      string animalType = animal.Value.type.Value.ToLower();

      if (animalType.Contains("cow") ||
          animalType.Contains("sheep") ||
          animalType.Contains("goat") ||
          animalType.Contains("pig"))
      {
        positionAboveAnimal.X += 50f;
        positionAboveAnimal.Y += 50f;
      }

      Game1.spriteBatch.Draw(
        Game1.mouseCursors,
        Utility.ModifyCoordinatesForUIScale(
          new Vector2(positionAboveAnimal.X, positionAboveAnimal.Y + _yMovementPerDraw.Value)
        ),
        new Rectangle(32, 0, 16, 16),
        Color.White * _alpha.Value,
        0.0f,
        Vector2.Zero,
        4f,
        SpriteEffects.None,
        1f
      );
    }
  }

  private void DrawIconForPets()
  {
    foreach (NPC? character in Game1.currentLocation.characters)
    {
      if (character is not Pet pet ||
          pet.lastPetDay.Values.Any(day => day == Game1.Date.TotalDays) ||
          (pet.friendshipTowardFarmer.Value >= 1000 && HideOnMaxFriendship))
      {
        continue;
      }

      Vector2 positionAboveAnimal = GetPetPositionAboveAnimal(character);

      positionAboveAnimal.X += 50f;
      positionAboveAnimal.Y += 20f;
      Game1.spriteBatch.Draw(
        Game1.mouseCursors,
        Utility.ModifyCoordinatesForUIScale(
          new Vector2(positionAboveAnimal.X, positionAboveAnimal.Y + _yMovementPerDraw.Value)
        ),
        new Rectangle(32, 0, 16, 16),
        Color.White * _alpha.Value,
        0.0f,
        Vector2.Zero,
        4f,
        SpriteEffects.None,
        1f
      );
    }
  }

  private Vector2 GetPetPositionAboveAnimal(Character animal)
  {
    Vector2 animalPosition = animal.getLocalPosition(Game1.viewport);
    animalPosition.X += 10;
    animalPosition.Y -= 34;
    return animalPosition;
  }

  private NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>>? GetAnimalsInCurrentLocation()
  {
    NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>>? animals = Game1.currentLocation switch
    {
      AnimalHouse animalHouse => animalHouse.Animals,
      Farm farm => farm.Animals,
      _ => null
    };

    return animals;
  }
#endregion
}
