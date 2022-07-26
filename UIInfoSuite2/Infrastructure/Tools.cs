using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace UIInfoSuite2.Infrastructure
{
    public static class Tools
    {
        public static void CreateSafeDelayedDialogue(string dialogue, int timer)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(timer);

                do
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                while (Game1.activeClickableMenu is GameMenu);
                Game1.setDialogue(dialogue, true);
            });
        }

        public static int GetWidthInPlayArea()
        {
            if (Game1.isOutdoorMapSmallerThanViewport())
            {
                int right = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right;
                int totalWidth = Game1.currentLocation.map.Layers[0].LayerWidth * Game1.tileSize;
                int someOtherWidth = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right - totalWidth;

                return right - someOtherWidth / 2;
            }
            else
            {
                return Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right;
            }
        }

        public static int GetTruePrice(Item item)
        {
            int truePrice = 0;

            if (item is StardewValley.Object objectItem)
            {
                truePrice = objectItem.sellToStorePrice() * 2;
            }
            else if (item is Item thing)
            {
                truePrice = thing.salePrice();
            }

            return truePrice;
        }

        public static void DrawMouseCursor()
        {
            if (!Game1.options.hardwareCursor)
            {
                int mouseCursorToRender = Game1.options.gamepadControls ? Game1.mouseCursor + 44 : Game1.mouseCursor;
                var what = Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, mouseCursorToRender, 16, 16);

                Game1.spriteBatch.Draw(
                    Game1.mouseCursors,
                    new Vector2(Game1.getMouseX(), Game1.getMouseY()),
                    what,
                    Color.White,
                    0.0f,
                    Vector2.Zero,
                    Game1.pixelZoom + (Game1.dialogueButtonScale / 150.0f),
                    SpriteEffects.None,
                    1f);
            }
        }

        public static Item GetHoveredItem()
        {
            Item hoverItem = null;

            if (Game1.onScreenMenus != null)
            {
                for (int i = 0; i < Game1.onScreenMenus.Count; ++i)
                {
                    Toolbar onScreenMenu = Game1.onScreenMenus[i] as Toolbar;
                    if (onScreenMenu != null)
                    {
                        FieldInfo hoverItemField = typeof(Toolbar).GetField("hoverItem", BindingFlags.Instance | BindingFlags.NonPublic);
                        hoverItem = hoverItemField.GetValue(onScreenMenu) as Item;
                    }
                }
            }

            if (Game1.activeClickableMenu is GameMenu gameMenu)
            {
                foreach (var menu in gameMenu.pages)
                {
                    if (menu is InventoryPage inventory)
                    {
                        FieldInfo hoveredItemField = typeof(InventoryPage).GetField("hoveredItem", BindingFlags.Instance | BindingFlags.NonPublic);
                        hoverItem = hoveredItemField.GetValue(inventory) as Item;
                    }
                }
            }

            if (Game1.activeClickableMenu is ItemGrabMenu itemMenu)
            {
                hoverItem = itemMenu.hoveredItem;
            }

            return hoverItem;
        }

        public static CraftingRecipe GetHoveredCraftingRecipe()
        {
            CraftingRecipe hoverRecipe = null;

            if(Game1.activeClickableMenu is CraftingPage craftingPage)
            {
                FieldInfo hoveredCraftingRecipeField = typeof(CraftingPage).GetField("hoverRecipe", BindingFlags.Instance | BindingFlags.NonPublic);
                hoverRecipe = hoveredCraftingRecipeField.GetValue(craftingPage) as CraftingRecipe;
            }

            return hoverRecipe;
        }

        public static void GetSubTexture(Color[] output, Color[] originalColors, Rectangle sourceBounds, Rectangle clipArea)
        {
            if (output.Length < clipArea.Width * clipArea.Height)
            {
                return;
            }

            var dest = 0;
            for (var yOffset = 0; yOffset < clipArea.Height; yOffset++)
            {
                for (var xOffset = 0; xOffset < clipArea.Width; xOffset++)
                {
                    var idx = (clipArea.X + xOffset) + (sourceBounds.Width * (yOffset + clipArea.Y));
                    output[dest++] = originalColors[idx];
                }
            }

        }

        public static void SetSubTexture(Color[] sourceColors, Color[] destColors, int destWidth, Rectangle destBounds, bool overlay = false)
        {
            if(sourceColors.Length > destColors.Length || (destBounds.Width * destBounds.Height) > destColors.Length) {
                return;
            }
            var emptyColor = new Color(0, 0, 0, 0);
            var srcIdx = 0;
            for (var yOffset = 0; yOffset < destBounds.Height; yOffset++)
            {
                for (var xOffset = 0; xOffset < destBounds.Width; xOffset++)
                {
                    var idx = (destBounds.X + xOffset) + (destWidth * (yOffset + destBounds.Y));
                    Color sourcePixel = sourceColors[srcIdx++];
                    
                    // If using overlay mode, don't copy transparent pixels
                    if (overlay && emptyColor.Equals(sourcePixel))
                    {
                        continue;
                    }
                    destColors[idx] = sourcePixel;
                }
            }
        }
    }
}
