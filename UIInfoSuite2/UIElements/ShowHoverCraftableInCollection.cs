using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using UIInfoSuite2.Infrastructure;

namespace UIInfoSuite2.UIElements
{
    internal class ShowHoverCraftableInCollection : IDisposable
    {

        private readonly IModHelper _helper;
        private readonly PerScreen<CraftingRecipe> _hoverRecipe = new();

        public ShowHoverCraftableInCollection(IModHelper helper)
        {
            _helper = helper;
        }

        public void ToggleOption(bool showItemHoverInformation)
        {
            _helper.Events.Display.Rendering -= OnRendering;
            _helper.Events.Display.Rendered -= OnRendered;


            if (showItemHoverInformation)
            {
                _helper.Events.Display.Rendering += OnRendering;
                _helper.Events.Display.Rendered += OnRendered;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private void OnRendering(object sender, EventArgs e)
        {
            _hoverRecipe.Value = Tools.GetHoveredCraftingRecipe();
        }

        private void OnRendered(object sender, EventArgs e)
        {
            DrawToolTip();
        }

        private void DrawToolTip()
        {
            if (Game1.activeClickableMenu != null)
            {
                if (_hoverRecipe.Value is StardewValley.CraftingRecipe recipe)
                {
                    if (recipe.isCookingRecipe && !Game1.player.recipesCooked.ContainsKey(recipe.getIndexOfMenuView()))
                    {
                        int windowHeight = 80;

                        int windowY = Game1.getMouseY() + 20;
                        windowY = Game1.viewport.Height - windowHeight - windowY < 0 ? Game1.viewport.Height - windowHeight : windowY;

                        int windowWidth = 80;

                        int windowX = Game1.getMouseX() - windowWidth - 25;

                        if (Game1.getMouseX() > Game1.viewport.Width - 300)
                        {
                            windowX = Game1.viewport.Width - windowWidth - 350;
                        }
                        else if (windowX < 0)
                        {
                            windowX = Game1.getMouseX() + 350;
                        }

                        Vector2 windowPos = new Vector2(windowX, windowY);
                        
                        // +22 centers the image in the box
                        int num1 = (int)windowPos.X + 22; 
                        int num2 = (int)windowPos.Y + 22;

                        IClickableMenu.drawTextureBox(
                            Game1.spriteBatch,
                            Game1.menuTexture,
                            new Rectangle(0, 256, 60, 60),
                            (int)windowPos.X,
                            (int)windowPos.Y,
                            windowWidth,
                            windowHeight,
                            Color.White);

                        var _icon = new ClickableTextureComponent(
                            new Rectangle(num1, num2, 40, 40),
                            Game1.mouseCursors,
                            new Rectangle(609, 361, 28, 28),
                            1.3f);

                        _icon.draw(Game1.spriteBatch);
                    }
                }
            }
        }
    }
}