using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.GameData.Machines;
using StardewValley.Menus;
using SObject = StardewValley.Object;

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
                //cambio
                //Game1.setDialogue(dialogue, true);
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

        public static int GetSellToStorePrice(Item item)
        {
            if (item is SObject obj)
            {
                return obj.sellToStorePrice();        
            }
            else
            {
                return item.salePrice() / 2;
            }
        }

        public static SObject? GetHarvest(Item item)
        {
             //cambio   
            if (item is SObject seedsObject
                && seedsObject.Category == StardewValley.Object.SeedsCategory
                && seedsObject.QualifiedItemId != Crop.mixedSeedsQId)
            {
                if (seedsObject.isSapling())
                {
                    //var tree = new StardewValley.TerrainFeatures.FruitTree(seedsObject.ItemId);
                    if (StardewValley.TerrainFeatures.FruitTree.TryGetData(seedsObject.itemId.Value, out StardewValley.GameData.FruitTrees.FruitTreeData data))
                    {
                        //ModEntry.MonitorObject.Log(data.Fruit.Count > 0 ? $"Count:{data.Fruit.Count}  ID:{data.Fruit[0].ItemId}" :$"Count:{data.Fruit.Count}" ,LogLevel.Info);
                        return new SObject(data.Fruit[0].ItemId, 1);
                    }
                    else
                        return null;
                    //ModEntry.MonitorObject.Log("Arbol:" + tree.GetData().Fruit[0].ObjectDisplayName,LogLevel.Info);
                    //ModEntry.MonitorObject.Log("Arbol:"+new SObject(tree.fruit/*.indexOfFruit.Value*/, 1).DisplayName,LogLevel.Info);
                    
                }
                else if (ModEntry.DGA.IsCustomObject(item, out var dgaHelper))
                {
                    try
                    {
                        return dgaHelper.GetSeedsHarvest(item);
                    }
                    catch (Exception e)
                    {
                        string? itemId = null;
                        try
                        {
                            itemId = dgaHelper.GetFullId(item);
                        }
                        catch (Exception catchException)
                        {
                            ModEntry.MonitorObject.Log(catchException.ToString(), LogLevel.Trace);
                        }
                        ModEntry.MonitorObject.LogOnce($"An error occured while fetching the harvest for {itemId ?? "unknownItem"}", LogLevel.Error);
                        ModEntry.MonitorObject.Log(e.ToString(), LogLevel.Debug);
                        return null;
                    }
                }
                else
                {
                    var crop = new Crop(seedsObject.itemId.Value, 0, 0,Game1.getFarm());
                    return new SObject(crop.indexOfHarvest.Value, 1);
                }
            } else {
                return null;
            }
        }

        public static int GetHarvestPrice(Item item)
        {
            return GetHarvest(item)?.sellToStorePrice() ?? 0;
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

        public static Item? GetHoveredItem()
        {
            Item? hoverItem = null;

            if (Game1.activeClickableMenu == null && Game1.onScreenMenus != null)
            {
                foreach (var menu in Game1.onScreenMenus)
                {
                    if (menu is Toolbar toolbar)
                    {
                        FieldInfo hoverItemField = typeof(Toolbar).GetField("hoverItem", BindingFlags.Instance | BindingFlags.NonPublic);
                        hoverItem = hoverItemField.GetValue(toolbar) as Item;
                    }
                }
            }

            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.GetCurrentPage() is InventoryPage inventory)
            {
                
                //FieldInfo hoveredItemField = typeof(InventoryPage).GetField("hoveredItem", BindingFlags.Instance | BindingFlags.NonPublic);
                //hoverItem = hoveredItemField.GetValue(inventory) as Item;
                hoverItem = inventory.hoveredItem;
            }

            if (Game1.activeClickableMenu is ItemGrabMenu itemMenu)
            {
                hoverItem = itemMenu.hoveredItem;
            }

            return hoverItem;
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
