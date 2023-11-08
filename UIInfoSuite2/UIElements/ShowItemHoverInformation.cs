using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.UIElements
{
    internal class ShowItemHoverInformation : IDisposable
    {
        private readonly Dictionary<string, List<KeyValuePair<int, int>>> _prunedRequiredBundles = new();
        private readonly ClickableTextureComponent _bundleIcon =
            new(
                new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
                Game1.mouseCursors,
                new Rectangle(331, 374, 15, 14),
                3f);
        private readonly ClickableTextureComponent _museumIcon;
        private readonly ClickableTextureComponent _shippingBottomIcon =
            new(
                new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
                Game1.mouseCursors,
                new Rectangle(526, 218, 30, 22),
                1.2f);
        private readonly ClickableTextureComponent _shippingTopIcon =
            new(
                new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
                Game1.mouseCursors,
                new Rectangle(134, 236, 30, 15),
                1.2f);

        private readonly PerScreen<Item?> _hoverItem = new();
        private CommunityCenter _communityCenter;
        private LibraryMuseum _libraryMuseum;

        private Dictionary<string, string> _bundleNameOverrides;

        private readonly IModHelper _helper;

        public ShowItemHoverInformation(IModHelper helper)
        {
            _helper = helper;

            var gunther = Game1.getCharacterFromName("Gunther");
            if (gunther == null) {
                ModEntry.MonitorObject.Log($"{this.GetType().Name}: Could not find Gunther in the game, creating a fake one for ourselves.", LogLevel.Warn);
                gunther = new NPC() {
                    Name = "Gunther",
                    Age = 0,
                    Sprite = new AnimatedSprite("Characters\\Gunther"),
                };
            }

            _museumIcon = new(
                new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
                gunther.Sprite.Texture,
                gunther.GetHeadShot(),
                Game1.pixelZoom);
        }

        public void ToggleOption(bool showItemHoverInformation)
        {
            _helper.Events.GameLoop.DayStarted -= OnDayStarted;
            _helper.Events.Player.InventoryChanged -= OnInventoryChanged;
            _helper.Events.Display.Rendered -= OnRendered;
            _helper.Events.Display.RenderedHud -= OnRenderedHud;
            _helper.Events.Display.Rendering -= OnRendering;

            if (showItemHoverInformation)
            {
                _communityCenter = Game1.getLocationFromName("CommunityCenter") as CommunityCenter;

                _libraryMuseum = Game1.getLocationFromName("ArchaeologyHouse") as LibraryMuseum;

                _helper.Events.GameLoop.DayStarted += OnDayStarted;
                _helper.Events.Player.InventoryChanged += OnInventoryChanged;
                _helper.Events.Display.Rendered += OnRendered;
                _helper.Events.Display.RenderedHud += OnRenderedHud;
                _helper.Events.Display.Rendering += OnRendering;
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        // [EventPriority(EventPriority.Low)]
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            // NB The Custom Community Center mod is only ready at this point
            if (_helper.GameContent.CurrentLocaleConstant == LocalizedContentManager.LanguageCode.en && _helper.ModRegistry.IsLoaded("blueberry.CustomCommunityCentre"))
                _bundleNameOverrides = GetEnglishNamesForCustomCommunityCenterBundles();
            else
                _bundleNameOverrides = null;
            PopulateRequiredBundles();
        }

        /// <summary>Raised before the game draws anything to the screen in a draw tick, as soon as the sprite batch is opened.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRendering(object sender, EventArgs e)
        {
            _hoverItem.Value = Tools.GetHoveredItem();
        }

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open). Content drawn to the sprite batch at this point will appear over the HUD.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedHud(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu == null)
            {
                DrawAdvancedTooltip();
            }
        }

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRendered(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu != null)
            {
                DrawAdvancedTooltip();
            }
        }

        /// <summary>Raised after items are added or removed to a player's inventory. NOTE: this event is currently only raised for the current player.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnInventoryChanged(object sender, InventoryChangedEventArgs e)
        {
            if (e.IsLocalPlayer)
                this.PopulateRequiredBundles();
        }

        /// Retrieve English bundle names for Custom Community Center bundles as the bundle id is not the bundle's English name.
        /// For example, the bundle id is "Custom_blueberry_Kitchen_Area0_Bundle0" but the name is "Baker's"
        private Dictionary<string, string> GetEnglishNamesForCustomCommunityCenterBundles()
        {
            try
            {
                Dictionary<string, string> englishNames = new();
                var bundleNamesAsset = _helper.GameContent.Load<Dictionary<string, string>>(@"Strings\BundleNames");
                foreach (var namePair in bundleNamesAsset)
                {
                    if (namePair.Key != namePair.Value)
                        englishNames[namePair.Key] = namePair.Value;
                }
                return englishNames;
            }
            catch (Exception e)
            {
                ModEntry.MonitorObject.LogOnce("Failed to retrieve English names for Custom Community Center bundles. Custom bundle names may be displayed incorrectly.", LogLevel.Warn);
                ModEntry.MonitorObject.Log(e.ToString());
                return null;
            }
        }

        private void PopulateRequiredBundles()
        {
            _prunedRequiredBundles.Clear();
            if (!Game1.player.mailReceived.Contains("JojaMember"))
            {

                foreach (var bundle in Game1.netWorldState.Value.BundleData)
                {
                    string[] bundleRoomInfo = bundle.Key.Split('/');
                    string bundleRoom = bundleRoomInfo[0];
                    int areaNum = CommunityCenter.getAreaNumberFromName(bundleRoom);

                    if (_communityCenter.shouldNoteAppearInArea(areaNum))
                    {
                        int bundleNumber = bundleRoomInfo[1].SafeParseInt32();
                        string[] bundleInfo = bundle.Value.Split('/');
                        string bundleName = _helper.GameContent.CurrentLocaleConstant == LocalizedContentManager.LanguageCode.en || int.TryParse(bundleInfo[^1], out _)
                            ? bundleInfo[0]
                            : bundleInfo[^1];
                        string[] bundleValues = bundleInfo[2].Split(' ');
                        List<KeyValuePair<int, int>> source = new List<KeyValuePair<int, int>>();

                        for (int i = 0; i < bundleValues.Length; i += 3)
                        {
                            int bundleValue = bundleValues[i].SafeParseInt32();
                            int quality = bundleValues[i + 2].SafeParseInt32();
                            if (bundleValue != -1 &&
                                !_communityCenter.bundles[bundleNumber][i / 3])
                            {
                                source.Add(new KeyValuePair<int, int>(bundleValue, quality));
                            }
                        }

                        if (source.Count > 0)
                        {
                            if (_bundleNameOverrides != null && _bundleNameOverrides.TryGetValue(bundleName, out string value))
                                bundleName = value;
                            _prunedRequiredBundles.Add(bundleName, source);
                        }
                    }
                }
            }
        }

        //private Item _lastHoverItem;
        //private int lastStackSize;
        //private int lastItemPrice;
        //private int lastStackPrice;
        //private int lastCropPrice;
        //private int lastTruePrice;
        //private int lastWindowWidth;
        //private string lastRequiredBundleName;

        private void DrawAdvancedTooltip()
        {

            if (_hoverItem.Value != null
                && !(_hoverItem.Value is StardewValley.Tools.MeleeWeapon weapon && weapon.isScythe())
                && !(_hoverItem.Value is StardewValley.Tools.FishingRod))
            {
                var hoveredObject = _hoverItem.Value as StardewValley.Object;
                int itemPrice = Tools.GetSellToStorePrice(_hoverItem.Value);

                int stackPrice = 0;
                if (itemPrice > 0 && _hoverItem.Value.Stack > 1)
                    stackPrice = itemPrice * _hoverItem.Value.Stack;
                
                int cropPrice = Tools.GetHarvestPrice(_hoverItem.Value);

                bool notDonatedYet = _libraryMuseum.isItemSuitableForDonation(_hoverItem.Value);

                bool notShippedYet = (hoveredObject != null
                    && hoveredObject.countsForShippedCollection()
                    && !Game1.player.basicShipped.ContainsKey(hoveredObject.QualifiedItemId)
                    && hoveredObject.Type != "Fish");
                if (notShippedYet && hoveredObject != null && ModEntry.DGA.IsCustomObject(hoveredObject, out var dgaHelper))
                {
                    // NB For DGA items, Game1.player.basicShipped.ContainsKey(hoveredObject.ParentSheetIndex) will always be false
                    //    and Object.countsForShippedCollection bypasses the type and category checks
                    try
                    {
                        // For shipping, DGA patches:
                        // - CollectionsPage()
                        // - ShippingMenu.parseItems()
                        // - StardewValley.Object.countsForShippedCollection()
                        // - StardewValley.Object.isIndexOkForBasicShippedCategory()
                        // But it doesn't patch Utility.getFarmerItemsShippedPercent() which determines if the requirements for the achievement are met.
                        // This means that DGA items do not (yet) count for the "Full Shipment" achievement even though they appear in the collections page.
                        
                        // Nonetheless, show the icon if that item is still hidden in the collections page.
                        string dgaId = dgaHelper.GetDgaObjectFakeId(hoveredObject);
                        string t = hoveredObject.Type;
                        bool inCollectionsPage = !(t.Contains("Arch") || t.Contains("Fish") || t.Contains("Mineral") || t.Contains("Cooking"))
                            && StardewValley.Object.isPotentialBasicShippedCategory(dgaId, hoveredObject.Category);
                            
                        notShippedYet = inCollectionsPage && !Game1.player.basicShipped.ContainsKey(dgaId);
                    }
                    catch (Exception e)
                    {
                        ModEntry.MonitorObject.LogOnce($"An error occured while checking if the DGA item {hoveredObject.Name} has been shipped.", LogLevel.Error);
                        ModEntry.MonitorObject.Log(e.ToString(), LogLevel.Debug);
                    }
                }

                string? requiredBundleName = null;
                // Bundle items must be "small" objects. This avoids marking other kinds of objects as needed, such as Chest (id 130), Recycling Machine (id 20), etc...
                if (hoveredObject != null && !hoveredObject.bigCraftable.Value && hoveredObject is not Furniture)
                {
                    foreach (var requiredBundle in _prunedRequiredBundles)
                    {
                        if (requiredBundle.Value.Any(itemQuality => itemQuality.Key == hoveredObject.ParentSheetIndex && hoveredObject.Quality >= itemQuality.Value))
                        {
                            requiredBundleName = requiredBundle.Key;
                            break;
                        }
                    }
                }

                var drawPositionOffset = new Vector2();
                int windowWidth, windowHeight;
                
                int bundleHeaderWidth = 0;
                if (!string.IsNullOrEmpty(requiredBundleName))
                {
                    // bundleHeaderWidth = ((bundleIcon.Width * 3 = 45) - 7 = 38) + 3 + bundleTextSize.X + 3 + ((shippingBin.Width * 1.2 = 36) - 12 = 24)
                    bundleHeaderWidth = 68 + (int)Game1.dialogueFont.MeasureString(requiredBundleName).X;
                }
                int itemTextWidth = (int)Game1.smallFont.MeasureString(itemPrice.ToString()).X;
                int stackTextWidth = (int)Game1.smallFont.MeasureString(stackPrice.ToString()).X;
                int cropTextWidth = (int)Game1.smallFont.MeasureString(cropPrice.ToString()).X;
                int minTextWidth = (int)Game1.smallFont.MeasureString("000").X;
                // largestTextWidth = 12 + 4 + (icon.Width = 32) + 4 + max(textSize.X) + 8 + 16
                int largestTextWidth = 76 + Math.Max(minTextWidth, Math.Max(stackTextWidth, Math.Max(itemTextWidth, cropTextWidth)));
                windowWidth = Math.Max(bundleHeaderWidth, largestTextWidth);

                windowHeight = 20 + 16;
                if (itemPrice > 0)
                    windowHeight += 40;
                if (stackPrice > 0)
                    windowHeight += 40;
                if (cropPrice > 0)
                    windowHeight += 40;
                if (!string.IsNullOrEmpty(requiredBundleName))
                {
                    windowHeight += 4;
                    drawPositionOffset.Y += 4;
                }
                
                // Minimal window dimensions
                windowHeight = Math.Max(windowHeight, 40);
                windowWidth = Math.Max(windowWidth, 40);

                int windowY = Game1.getMouseY() + 20;
                int windowX = Game1.getMouseX() - 25 - windowWidth;

                // Adjust the tooltip's position when it overflows
                var safeArea = Utility.getSafeArea();

                if (windowY + windowHeight > safeArea.Bottom)
                    windowY = safeArea.Bottom - windowHeight;

                if (Game1.getMouseX() + 300 > safeArea.Right)
                    windowX = safeArea.Right - 350 - windowWidth;
                else if (windowX < safeArea.Left)
                    windowX = Game1.getMouseX() + 350;

                Vector2 windowPos = new Vector2(windowX, windowY);
                //ModEntry.MonitorObject.Log($"Mouse:{Game1.getMouseX()},{Game1.getMouseY()} WindPos:{windowPos.X},{windowPos.Y} ", LogLevel.Info);
                Vector2 drawPosition = windowPos + new Vector2(16, 20) + drawPositionOffset;

                // Icons are drawn in 32x40 cells. The small font has a cap height of 18 and an offset of (2, 6)
                int rowHeight = 40;
                Vector2 iconCenterOffset = new Vector2(16, 20);
                Vector2 textOffset = new Vector2(32 + 4, (rowHeight - 18) / 2 - 6);

                if (itemPrice > 0 || stackPrice > 0 || cropPrice > 0 || !String.IsNullOrEmpty(requiredBundleName) || notDonatedYet || notShippedYet)
                {
                    IClickableMenu.drawTextureBox(
                        Game1.spriteBatch,
                        Game1.menuTexture,
                        new Rectangle(0, 256, 60, 60),
                        (int)windowPos.X,
                        (int)windowPos.Y,
                        windowWidth,
                        windowHeight,
                        Color.White);
                }

                if (itemPrice > 0)
                {
                    Game1.spriteBatch.Draw(
                        Game1.debrisSpriteSheet,
                        drawPosition + iconCenterOffset,
                        Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
                        Color.White,
                        0,
                        new Vector2(8, 8),
                        Game1.pixelZoom,
                        SpriteEffects.None,
                        0.95f);
                    
                    this.DrawSmallTextWithShadow(Game1.spriteBatch,itemPrice.ToString(), drawPosition + textOffset);

                    drawPosition.Y += rowHeight;
                }

                if (stackPrice > 0)
                {
                    Vector2 overlapOffset = new Vector2(0, 10);
                    Game1.spriteBatch.Draw(
                        Game1.debrisSpriteSheet,
                        drawPosition + iconCenterOffset - overlapOffset / 2,
                        Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
                        Color.White,
                        0,
                        new Vector2(8, 8),
                        Game1.pixelZoom,
                        SpriteEffects.None,
                        0.95f);
                    Game1.spriteBatch.Draw(
                        Game1.debrisSpriteSheet,
                        drawPosition + iconCenterOffset + overlapOffset / 2,
                        Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
                        Color.White,
                        0,
                        new Vector2(8, 8),
                        Game1.pixelZoom,
                        SpriteEffects.None,
                        0.95f);

                    this.DrawSmallTextWithShadow(Game1.spriteBatch,stackPrice.ToString(), drawPosition + textOffset);

                    drawPosition.Y += rowHeight;
                }

                if (cropPrice > 0)
                {
                    Game1.spriteBatch.Draw(
                        Game1.mouseCursors,
                        drawPosition + iconCenterOffset,
                        new Rectangle(60, 428, 10, 10),
                        Color.White,
                        0.0f,
                        new Vector2(5, 5),
                        Game1.pixelZoom * 0.75f,
                        SpriteEffects.None,
                        0.85f);

                    this.DrawSmallTextWithShadow(Game1.spriteBatch, cropPrice.ToString(), drawPosition + textOffset);
                }

                if (notDonatedYet)
                {
                    Game1.spriteBatch.Draw(
                        _museumIcon.texture,
                        windowPos + new Vector2(2, windowHeight + 8),
                        _museumIcon.sourceRect,
                        Color.White,
                        0f,
                        new Vector2(_museumIcon.sourceRect.Width / 2, _museumIcon.sourceRect.Height),
                        2,
                        SpriteEffects.None,
                        0.86f);
                }

                if (!string.IsNullOrEmpty(requiredBundleName))
                {
                    // Draws a 30x42 bundle icon offset by (-7, -13) from the top-left corner of the window
                    // and the 36px high banner with the bundle name
                    this.DrawBundleBanner(requiredBundleName, windowPos + new Vector2(-7, -13), windowWidth);
                }

                if (notShippedYet)
                {
                    // Draws a 36x28 shipping bin offset by (-24, -6) from the top-right corner of the window
                    var shippingBinDims = new Vector2(30, 24);
                    this.DrawShippingBin(Game1.spriteBatch, windowPos + new Vector2(windowWidth - 6, 8), shippingBinDims / 2);
                }

                //memorize the result to save processing time when calling again with same values
                //_lastHoverItem = (_lastHoverItem != _hoverItem.Value) ? _hoverItem.Value : _lastHoverItem;
                //lastItemPrice = (itemPrice != lastItemPrice) ? itemPrice : lastItemPrice;
                //lastCropPrice = (lastCropPrice != cropPrice) ? cropPrice : lastCropPrice;
                //lastStackPrice = (lastStackPrice != stackPrice) ? stackPrice : lastStackPrice;
                //lastTruePrice = (lastTruePrice != truePrice) ? truePrice : lastTruePrice;
                //lastWindowWidth = (lastWindowWidth != windowWidth) ? windowWidth : lastWindowWidth;
                //lastRequiredBundleName = (lastRequiredBundleName != requiredBundleName) ? requiredBundleName : lastRequiredBundleName;
                //lastStackSize = (_hoverItem.Value != null && lastStackSize != _hoverItem.Value.Stack) ? _hoverItem.Value.Stack : lastStackSize;
            }
        }

        private void DrawSmallTextWithShadow(SpriteBatch b, string text, Vector2 position)
        {
            b.DrawString(Game1.smallFont, text, position + new Vector2(2, 2), Game1.textShadowColor);
            b.DrawString(Game1.smallFont, text, position, Game1.textColor);
        }

        private void DrawBundleBanner(string bundleName, Vector2 position, int windowWidth)
        {
            // NB The dialogue font has a cap height of 30 and an offset of (3, 6)

            int bundleBannerX = (int)position.X;
            int bundleBannerY = (int)position.Y + 3;
            int cellCount = 36;
            int solidCells = 6;
            int cellWidth = windowWidth / cellCount;
            for (int cell = 0; cell < cellCount; ++cell)
            {
                float fadeAmount = 0.92f - (cell < solidCells ? 0 : 1.0f * (cell-solidCells)/(cellCount-solidCells));
                Game1.spriteBatch.Draw(
                    Game1.staminaRect,
                    new Rectangle(bundleBannerX + cell * cellWidth, bundleBannerY, cellWidth, 36),
                    Color.Crimson * fadeAmount);
            }

            Game1.spriteBatch.Draw(
                Game1.mouseCursors,
                position,
                _bundleIcon.sourceRect,
                Color.White,
                0f,
                Vector2.Zero,
                _bundleIcon.scale,
                SpriteEffects.None,
                0.86f);

            Game1.spriteBatch.DrawString(
                Game1.dialogueFont,
                bundleName,
                position + new Vector2(_bundleIcon.sourceRect.Width * _bundleIcon.scale + 3, 0),
                Color.White);
        }

        private void DrawShippingBin(SpriteBatch b, Vector2 position, Vector2 origin)
        {
            var shippingBinOffset = new Vector2(0, 2);
            // var shippingBinLidOffset = Vector2.Zero;
            
            // NB This is not the texture used to draw the shipping bin on the farm map.
            //    The one for the farm is located in "Buildings\Shipping Bin".
            Game1.spriteBatch.Draw(
                _shippingBottomIcon.texture,
                position,
                _shippingBottomIcon.sourceRect,
                Color.White,
                0f,
                origin - shippingBinOffset,
                _shippingBottomIcon.scale,
                SpriteEffects.None,
                0.86f);
            Game1.spriteBatch.Draw(
                _shippingTopIcon.texture,
                position,
                _shippingTopIcon.sourceRect,
                Color.White,
                0f,
                origin,
                _shippingTopIcon.scale,
                SpriteEffects.None,
                0.86f);
        }
    }
}
