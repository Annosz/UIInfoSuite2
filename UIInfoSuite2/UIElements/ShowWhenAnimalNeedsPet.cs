using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Network;
using System;
using System.Linq;

namespace UIInfoSuite2.UIElements
{
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
        private void OnWarped(object sender, WarpedEventArgs e)
        {

        }

        private void OnRenderingHud_DrawNeedsPetTooltip(object sender, RenderingHudEventArgs e)
        {
            if (!Game1.eventUp && Game1.activeClickableMenu == null && (Game1.currentLocation is AnimalHouse || Game1.currentLocation is Farm || Game1.currentLocation is FarmHouse))
            {
                DrawIconForFarmAnimals();
                DrawIconForPets();
            }
        }

        private void OnRenderingHud_DrawAnimalHasProduct(object sender, RenderingHudEventArgs e)
        {
            if (!Game1.eventUp && Game1.activeClickableMenu == null && (Game1.currentLocation is AnimalHouse || Game1.currentLocation is Farm))
            {
                DrawAnimalHasProduct();
            }
        }

        private void UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (Game1.eventUp || Game1.activeClickableMenu != null || !(Game1.currentLocation is AnimalHouse || Game1.currentLocation is Farm || Game1.currentLocation is FarmHouse))
                return;

            float sine = (float)Math.Sin(e.Ticks / 20.0);
            _yMovementPerDraw.Value = -6f + 6f * sine;
            _alpha.Value = 0.8f + 0.2f * sine;
        }
        #endregion

        #region Logic
        private void DrawAnimalHasProduct()
        {
            var animalsInCurrentLocation = GetAnimalsInCurrentLocation();
            if (animalsInCurrentLocation != null)
            {
                foreach (var animal in animalsInCurrentLocation.Pairs)
                {
                    if (animal.Value.GetHarvestType() != FarmAnimal.newHome &&
                        !animal.Value.IsEmoting &&
                        animal.Value.currentProduce.Value != "430" &&
                        animal.Value.currentProduce.Value != null &&
                        animal.Value.age.Value >= animal.Value.GetDaysOwned())
                    {
                        Vector2 positionAboveAnimal = GetPetPositionAboveAnimal(animal.Value);
                        positionAboveAnimal.Y += (float)(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 300.0 + animal.Value.Name.GetHashCode()) * 5.0);
                        Game1.spriteBatch.Draw(
                            Game1.emoteSpriteSheet,
                            Utility.ModifyCoordinatesForUIScale(new Vector2(positionAboveAnimal.X + 14f, positionAboveAnimal.Y)),
                            new Rectangle(3 * (Game1.tileSize / 4) % Game1.emoteSpriteSheet.Width, 3 * (Game1.tileSize / 4) / Game1.emoteSpriteSheet.Width * (Game1.tileSize / 4), Game1.tileSize / 4, Game1.tileSize / 4),
                            Color.White * 0.9f,
                            0.0f,
                            Vector2.Zero,
                            4f,
                            SpriteEffects.None,
                            1f);

                        Rectangle sourceRectangle = ItemRegistry.GetDataOrErrorItem(animal.Value.currentProduce.Value).GetSourceRect()/*GameLocation.getSourceRectForObject(animal.Value.currentProduce.Value)*/;
                        Game1.spriteBatch.Draw(
                            Game1.objectSpriteSheet,
                            Utility.ModifyCoordinatesForUIScale(new Vector2(positionAboveAnimal.X + 28f, positionAboveAnimal.Y + 8f)),
                            sourceRectangle,
                            Color.White * 0.9f,
                            0.0f,
                            Vector2.Zero,
                            2.2f,
                            SpriteEffects.None,
                            1f);
                    }
                }
            }
        }

        private void DrawIconForFarmAnimals()
        {
            var animalsInCurrentLocation = GetAnimalsInCurrentLocation();

            if (animalsInCurrentLocation != null)
            {
                foreach (var animal in animalsInCurrentLocation.Pairs)
                {
                    if (!animal.Value.IsEmoting &&
                        !animal.Value.wasPet.Value &&
                        (animal.Value.friendshipTowardFarmer.Value < 1000 || !HideOnMaxFriendship))
                    {
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
                            Utility.ModifyCoordinatesForUIScale(new Vector2(positionAboveAnimal.X, positionAboveAnimal.Y + _yMovementPerDraw.Value)),
                            new Rectangle(32, 0, 16, 16),
                            Color.White * _alpha.Value,
                            0.0f,
                            Vector2.Zero,
                            4f,
                            SpriteEffects.None,
                            1f);
                    }
                }
            }
        }

        private void DrawIconForPets()
        {
            foreach (var character in Game1.currentLocation.characters)
            {
                if (character is Pet pet &&
                    !pet.lastPetDay.Values.Any(day => day == Game1.Date.TotalDays)
                    && (pet.friendshipTowardFarmer.Value < 1000 || !HideOnMaxFriendship))
                {
                    Vector2 positionAboveAnimal = GetPetPositionAboveAnimal(character);
                    positionAboveAnimal.X += 50f;
                    positionAboveAnimal.Y -= 20f;
                    Game1.spriteBatch.Draw(
                        Game1.mouseCursors,
                        Utility.ModifyCoordinatesForUIScale(new Vector2(positionAboveAnimal.X, positionAboveAnimal.Y + _yMovementPerDraw.Value)),
                        new Rectangle(32, 0, 16, 16),
                        Color.White * _alpha.Value,
                        0.0f,
                        Vector2.Zero,
                        4f,
                        SpriteEffects.None,
                        1f);
                }
            }
        }

        private Vector2 GetPetPositionAboveAnimal(Character animal)
        {
            return new Vector2(Game1.viewport.Width <= Game1.currentLocation.map.DisplayWidth ? animal.position.X - Game1.viewport.X + 16 : animal.position.X + ((Game1.viewport.Width - Game1.currentLocation.map.DisplayWidth) / 2 + 18),
                Game1.viewport.Height <= Game1.currentLocation.map.DisplayHeight ? animal.position.Y - Game1.viewport.Y - 34 : animal.position.Y + ((Game1.viewport.Height - Game1.currentLocation.map.DisplayHeight) / 2 - 50));
        }

        private NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>> GetAnimalsInCurrentLocation()
        {
            NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>> animals = null;

            if (Game1.currentLocation is AnimalHouse)
            {
                animals = (Game1.currentLocation as AnimalHouse).animals;
            }
            else if (Game1.currentLocation is Farm)
            {
                animals = (Game1.currentLocation as Farm).animals;
            }

            return animals;
        }
        #endregion
    }
}
