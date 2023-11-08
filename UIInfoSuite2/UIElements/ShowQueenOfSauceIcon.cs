using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Reflection;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.UIElements
{
    internal class ShowQueenOfSauceIcon : IDisposable
    {
        #region Properties
        private Dictionary<string, string> _recipesByDescription = new();
        private Dictionary<string, string> _recipes = new();
        private CraftingRecipe _todaysRecipe;

        private readonly PerScreen<bool> _drawQueenOfSauceIcon = new();
        //private bool _drawDishOfDayIcon = false;
        private readonly PerScreen<ClickableTextureComponent> _icon = new();

        private readonly IModHelper _helper;
        #endregion

        #region Life cycle
        public ShowQueenOfSauceIcon(IModHelper helper)
        {
            _helper = helper;
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        public void ToggleOption(bool showQueenOfSauceIcon)
        {
            _helper.Events.Display.RenderingHud -= OnRenderingHud;
            _helper.Events.Display.RenderedHud -= OnRenderedHud;
            _helper.Events.GameLoop.DayStarted -= OnDayStarted;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            _helper.Events.GameLoop.SaveLoaded -= OnSaveLoaded;

            if (showQueenOfSauceIcon)
            {
                LoadRecipes();
                CheckForNewRecipe();

                _helper.Events.GameLoop.DayStarted += OnDayStarted;
                _helper.Events.Display.RenderingHud += OnRenderingHud;
                _helper.Events.Display.RenderedHud += OnRenderedHud;
                _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
                _helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            }
        }
        #endregion

        #region Event subscriptions
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            CheckForNewRecipe();
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            CheckForNewRecipe();
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (e.IsOneSecond && _drawQueenOfSauceIcon.Value && Game1.player.knowsRecipe(_todaysRecipe.name))
                _drawQueenOfSauceIcon.Value = false;
        }

        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            if (!Game1.eventUp)
            {
                if (_drawQueenOfSauceIcon.Value)
                {
                    Point iconPosition = IconHandler.Handler.GetNewIconPosition();

                    _icon.Value = new ClickableTextureComponent(
                        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
                        Game1.mouseCursors,
                        new Rectangle(609, 361, 28, 28),
                        1.3f);
                    _icon.Value.draw(Game1.spriteBatch);
                }

                //if (_drawDishOfDayIcon)
                //{
                //    Point iconLocation = IconHandler.Handler.GetNewIconPosition();
                //    float scale = 2.9f;

                //    Game1.spriteBatch.Draw(
                //        Game1.objectSpriteSheet,
                //        new Vector2(iconLocation.X, iconLocation.Y),
                //        new Rectangle(306, 291, 14, 14),
                //        Color.White,
                //        0,
                //        Vector2.Zero,
                //        scale,
                //        SpriteEffects.None,
                //        1f);

                //    ClickableTextureComponent texture =
                //        new ClickableTextureComponent(
                //            _gus.Name,
                //            new Rectangle(
                //                iconLocation.X - 7,
                //                iconLocation.Y - 2,
                //                (int)(16.0 * scale),
                //                (int)(16.0 * scale)),
                //            null,
                //            _gus.Name,
                //            _gus.Sprite.Texture,
                //            _gus.GetHeadShot(),
                //            2f);

                //    texture.draw(Game1.spriteBatch);

                //    if (texture.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                //    {
                //        IClickableMenu.drawHoverText(
                //            Game1.spriteBatch,
                //            "Gus is selling " + Game1.dishOfTheDay.DisplayName + " recipe today!",
                //            Game1.dialogueFont);
                //    }
                //}
            }
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (_drawQueenOfSauceIcon.Value && !Game1.IsFakedBlackScreen() && (_icon.Value?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false))
            {
                IClickableMenu.drawHoverText(Game1.spriteBatch, _helper.SafeGetString(LanguageKeys.TodaysRecipe) + _todaysRecipe.DisplayName, Game1.dialogueFont);
            }
        }
        #endregion

        #region Logic
        private void LoadRecipes()
        {
            if (_recipes.Count == 0)
            {
                _recipes = Game1.content.Load<Dictionary<string, string>>("Data\\TV\\CookingChannel");
                foreach (var next in _recipes)
                {
                    string[] values = next.Value.Split('/');
                    if (values.Length > 1)
                    {
                        _recipesByDescription[values[1]] = values[0];
                    }
                }
            }
        }

        private void CheckForNewRecipe()
        {
            int recipiesKnownBeforeTvCall = Game1.player.cookingRecipes.Count();

            string[] dialogue = typeof(TV).GetMethod("getWeeklyRecipe",BindingFlags.Instance | BindingFlags.NonPublic,null,Type.EmptyTypes,null).Invoke(new TV(), null) as string[];
            _todaysRecipe = new CraftingRecipe(_recipesByDescription.SafeGet(dialogue[0]), true);

            if (Game1.player.cookingRecipes.Count() > recipiesKnownBeforeTvCall)
                Game1.player.cookingRecipes.Remove(_todaysRecipe.name);

            _drawQueenOfSauceIcon.Value = (Game1.dayOfMonth % 7 == 0 || (Game1.dayOfMonth - 3) % 7 == 0)
                && Game1.stats.DaysPlayed > 5 && !Game1.player.knowsRecipe(_todaysRecipe.name);
        }

        //private void FindGus()
        //{
        //    foreach (var location in Game1.locations)
        //    {
        //        foreach (var npc in location.characters)
        //        {
        //            if (npc.Name == "Gus")
        //            {
        //                _gus = npc;
        //                break;
        //            }
        //        }
        //        if (_gus != null)
        //            break;
        //    }
        //}
        #endregion
    }
}
