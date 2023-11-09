using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.UIElements
{
    internal class ShowCropAndBarrelTime : IDisposable
    {
        private readonly Dictionary<string, string> _indexOfCropNames = new();
        private readonly PerScreen<StardewValley.Object> _currentTile = new();
        private readonly PerScreen<TerrainFeature> _terrain = new();
        private readonly PerScreen<Building> _currentTileBuilding = new();
        private readonly IModHelper _helper;

        private readonly Dictionary<string, string> _indexOfDgaCropNames = new();

        public ShowCropAndBarrelTime(IModHelper helper)
        {
            _helper = helper;
        }

        public void ToggleOption(bool showCropAndBarrelTimes)
        {
            _helper.Events.Display.RenderingHud -= OnRenderingHud;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showCropAndBarrelTimes)
            {
                _helper.Events.Display.RenderingHud += OnRenderingHud;
                _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(4))
                return;

            _currentTileBuilding.Value = null;
            _currentTile.Value = null;
            _terrain.Value = null;

            var gamepadTile = Game1.player.CurrentTool != null ? Utility.snapToInt(Game1.player.GetToolLocation() / Game1.tileSize) : Utility.snapToInt(Game1.player.GetGrabTile());
            var mouseTile = Game1.currentCursorTile;

            var tile = (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0) ? gamepadTile : mouseTile;

            if (Game1.currentLocation.IsBuildableLocation())
                _currentTileBuilding.Value = Game1.currentLocation.getBuildingAt(tile);

            if (Game1.currentLocation != null)
            {
                if (Game1.currentLocation.Objects != null && Game1.currentLocation.Objects.TryGetValue(tile, out var currentObject))
                    _currentTile.Value = currentObject;

                if (Game1.currentLocation.terrainFeatures != null && Game1.currentLocation.terrainFeatures.TryGetValue(tile, out var terrain))
                    _terrain.Value = terrain;

                // Make sure that _terrain is null before overwriting it because Tea Saplings are added to terrainFeatures and not IndoorPot.bush
                if (_terrain.Value == null && _currentTile.Value is IndoorPot pot)
                {
                    if (pot.hoeDirt.Value != null)
                        _terrain.Value = pot.hoeDirt.Value;
                    if (pot.bush.Value != null)
                        _terrain.Value = pot.bush.Value;
                }
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            if (Game1.activeClickableMenu != null)
                return;

            var currentTileBuilding = _currentTileBuilding.Value;
            var currentTile = _currentTile.Value;
            var terrain = _terrain.Value;
            int overrideX = -1;
            int overrideY = -1;

            // draw hover tooltip
            if (currentTileBuilding != null && currentTileBuilding.buildingType.Value == "Mill" && currentTileBuilding.GetBuildingChest("Input") != null && !currentTileBuilding.GetBuildingChest("Input").isEmpty())
            {
                int wheatCount = 0;
                int beetCount = 0;
                int unmilledriceCount = 0;
                

                foreach (Item item in currentTileBuilding.GetBuildingChest("Input").Items)
                {
                    if (item != null && !string.IsNullOrEmpty(item.Name))
                    {
                        switch (item.Name)
                        {
                            case "Wheat": wheatCount += item.Stack; break;
                            case "Beet": beetCount += item.Stack; break;
                            case "Unmilled Rice": unmilledriceCount += item.Stack; break;
                        }
                    }
                }

                StringBuilder builder = new StringBuilder();
                builder.AppendLine(string.Format(_helper.SafeGetString("Capacity"),currentTileBuilding.GetBuildingChest("Input").Items.Sum(x=> x.Stack),currentTileBuilding.GetBuildingChest("Input").GetActualCapacity() * 999));

                if (wheatCount > 0)
                    builder.Append($"{ItemRegistry.GetData("(O)262").DisplayName}:{wheatCount}");

                if (beetCount > 0)
                {
                    if (wheatCount > 0)
                        builder.Append(Environment.NewLine);
                    builder.Append($"{ItemRegistry.GetData("(O)284").DisplayName}:{beetCount}");
                }
                if (unmilledriceCount > 0)
                {
                    if (beetCount > 0 || wheatCount > 0)
                        builder.Append(Environment.NewLine);
                    builder.Append($"{ItemRegistry.GetData("(O)271").DisplayName}:{unmilledriceCount}");
                }


                if (builder.Length > 0)
                {
                    if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
                    {
                        var tilePosition = Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(new Vector2(currentTileBuilding.tileX.Value, currentTileBuilding.tileY.Value) * Game1.tileSize));
                        overrideX = (int)(tilePosition.X + Utility.ModifyCoordinateForUIScale(32));
                        overrideY = (int)(tilePosition.Y + Utility.ModifyCoordinateForUIScale(32));
                    }

                    IClickableMenu.drawHoverText(
                        Game1.spriteBatch,
                        builder.ToString(),
                        Game1.smallFont, overrideX: overrideX, overrideY: overrideY);
                }
            }
            else if (currentTile != null &&
                (!currentTile.bigCraftable.Value ||
                currentTile.MinutesUntilReady > 0))
            {
                if (currentTile.bigCraftable.Value &&
                    currentTile.MinutesUntilReady > 0 &&
                    currentTile.heldObject.Value != null &&
                    currentTile.Name != "Heater")
                {
                    StringBuilder hoverText = new StringBuilder();

                    hoverText.AppendLine(currentTile.heldObject.Value.DisplayName);

                    if (currentTile is Cask)
                    {
                        Cask currentCask = currentTile as Cask;
                        hoverText.Append((int)(currentCask.daysToMature.Value / currentCask.agingRate.Value))
                            .Append(" " + _helper.SafeGetString(
                            LanguageKeys.DaysToMature));
                    }
                    else
                    {
                        int timeLeft = currentTile.MinutesUntilReady;
                        int longTime = timeLeft / 60;
                        string longText = LanguageKeys.Hours;
                        int shortTime = timeLeft % 60;
                        string shortText = LanguageKeys.Minutes;

                        // 1600 minutes per day if you go to bed at 2am, more if you sleep early.
                        if (timeLeft >= 1600)
                        {
                            // Unlike crops and casks, this is only an approximate number of days
                            // because of how time works while sleeping. It's close enough though.
                            longText = LanguageKeys.Days;
                            longTime = timeLeft / 1600;

                            shortText = LanguageKeys.Hours;
                            shortTime = (timeLeft % 1600);

                            // Hours below 1200 are 60 minutes per hour. Overnight it's 100 minutes per hour.
                            // We could just divide by 60 here but then you could see strange times like
                            // "2 days, 25 hours".
                            // This is a bit of a fudge since depending on the current time of day and when the
                            // farmer goes to bed, the night might happen earlier or last longer, but it's just
                            // an approximation; regardless the processing won't finish before tomorrow.
                            if (shortTime <= 1200)
                                shortTime /= 60;
                            else
                                shortTime = 20 + (shortTime - 1200) / 100;
                        }

                        if (longTime > 0)
                            hoverText.Append(longTime).Append(" ")
                                .Append(_helper.SafeGetString(longText))
                                .Append(", ");

                        hoverText.Append(shortTime).Append(" ")
                            .Append(_helper.SafeGetString(shortText));
                    }

                    if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
                    {
                        var tilePosition = Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(new Vector2(currentTile.TileLocation.X, currentTile.TileLocation.Y) * Game1.tileSize));
                        overrideX = (int)(tilePosition.X + Utility.ModifyCoordinateForUIScale(32));
                        overrideY = (int)(tilePosition.Y + Utility.ModifyCoordinateForUIScale(32));
                    }

                    IClickableMenu.drawHoverText(
                        Game1.spriteBatch,
                        hoverText.ToString(),
                        Game1.smallFont, overrideX: overrideX, overrideY: overrideY);
                }
            }
            else if (terrain != null)
            {
                if (terrain is HoeDirt)
                {
                    HoeDirt hoeDirt = terrain as HoeDirt;
                    if (hoeDirt.crop != null &&
                        !hoeDirt.crop.dead.Value)
                    {
                        int num = 0;

                        if (hoeDirt.crop.fullyGrown.Value &&
                            hoeDirt.crop.dayOfCurrentPhase.Value > 0)
                        {
                            num = hoeDirt.crop.dayOfCurrentPhase.Value;
                        }
                        else
                        {
                            for (int i = 0; i < hoeDirt.crop.phaseDays.Count - 1; ++i)
                            {
                                if (hoeDirt.crop.currentPhase.Value == i)
                                    num -= hoeDirt.crop.dayOfCurrentPhase.Value;

                                if (hoeDirt.crop.currentPhase.Value <= i)
                                    num += hoeDirt.crop.phaseDays[i];
                            }
                        }

                        string? harvestName = this.GetCropHarvestName(hoeDirt.crop);
                        if (!String.IsNullOrEmpty(harvestName))
                        {
                            StringBuilder hoverText = new StringBuilder(harvestName).Append(": ");
                            if (num > 0)
                            {
                                hoverText.Append(num).Append(" ")
                                    .Append(_helper.SafeGetString(
                                        LanguageKeys.Days));
                            }
                            else
                            {
                                hoverText.Append(_helper.SafeGetString(
                                    LanguageKeys.ReadyToHarvest));
                            }

                            if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
                            {
                                var tilePosition = Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(new Vector2(terrain.Tile.X, terrain.Tile.Y) * Game1.tileSize));
                                overrideX = (int)(tilePosition.X + Utility.ModifyCoordinateForUIScale(32));
                                overrideY = (int)(tilePosition.Y + Utility.ModifyCoordinateForUIScale(32));
                            }

                            IClickableMenu.drawHoverText(
                                Game1.spriteBatch,
                                hoverText.ToString(),
                                Game1.smallFont, overrideX: overrideX, overrideY: overrideY);
                        }
                    }
                }
                else if (terrain is FruitTree)
                {
                    FruitTree tree = terrain as FruitTree;
                    var text = tree.GetDisplayName();
                    if (tree.daysUntilMature.Value > 0)
                    {
                        text += Environment.NewLine + tree.daysUntilMature.Value + " " +
                                _helper.SafeGetString(
                                    LanguageKeys.DaysToMature);

                    }

                    if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
                    {
                        var tilePosition = Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(new Vector2(terrain.Tile.X, terrain.Tile.Y) * Game1.tileSize));
                        overrideX = (int)(tilePosition.X + Utility.ModifyCoordinateForUIScale(32));
                        overrideY = (int)(tilePosition.Y + Utility.ModifyCoordinateForUIScale(32));
                    }

                    IClickableMenu.drawHoverText(
                            Game1.spriteBatch,
                            text,
                            Game1.smallFont, overrideX: overrideX, overrideY: overrideY);
                }
                else if (terrain is Bush bush)
                {
                    // Tea saplings (which are actually bushes)
                    if (bush.size.Value == Bush.greenTeaBush)
                    {
                        int teaAge = bush.getAge();
                        if (teaAge < 20)
                        {
                            string text = new StardewValley.Object("251", 1).DisplayName
                                + $"\n{20 - teaAge} "
                                + _helper.SafeGetString(LanguageKeys.DaysToMature);

                            if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
                            {
                                var tilePosition = Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(new Vector2(terrain.Tile.X, terrain.Tile.Y) * Game1.tileSize));
                                overrideX = (int)(tilePosition.X + Utility.ModifyCoordinateForUIScale(32));
                                overrideY = (int)(tilePosition.Y + Utility.ModifyCoordinateForUIScale(32));
                            }

                            IClickableMenu.drawHoverText(
                                Game1.spriteBatch,
                                text,
                                Game1.smallFont, overrideX: overrideX, overrideY: overrideY);
                        }
                    }
                }
            }
        }

        string? GetCropHarvestName(Crop crop)
        {
            if (crop.indexOfHarvest.Value != null)
            {
                string itemId = crop.isWildSeedCrop() ? crop.whichForageCrop.Value : crop.indexOfHarvest.Value;
                if (!_indexOfCropNames.TryGetValue(itemId, out string? harvestName)) {
                    harvestName = new StardewValley.Object(itemId, 1).DisplayName;
                    _indexOfCropNames.Add(itemId, harvestName);
                }
                return harvestName;
            }
            else if (ModEntry.DGA.IsCustomCrop(crop, out var dgaHelper))
            {
                string? cropId = null;
                try
                {
                    cropId = dgaHelper.GetFullId(crop)!;
                    if (!_indexOfDgaCropNames.TryGetValue(cropId, out string? harvestName)) {
                        var harvestCrop = dgaHelper.GetCropHarvest(crop);
                        if (harvestCrop == null)
                            return null;
                        
                        harvestName = harvestCrop.DisplayName;
                        _indexOfDgaCropNames.Add(cropId, harvestName);
                    }
                    return harvestName;
                }
                catch (Exception e)
                {
                    ModEntry.MonitorObject.LogOnce($"An error occured while retrieving the crop harvest name for {cropId ?? "unknownCrop"}.", LogLevel.Error);
                    ModEntry.MonitorObject.Log(e.ToString(), LogLevel.Debug);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
