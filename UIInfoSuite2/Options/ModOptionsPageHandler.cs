using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.UIElements;

namespace UIInfoSuite2.Options
{
    internal class ModOptionsPageHandler : IDisposable
    {
        private readonly IModHelper _helper;
        private readonly bool _showPersonalConfigButton;

        private List<ModOptionsElement> _optionsElements = new();
        private readonly List<IDisposable> _elementsToDispose;

        private ModOptionsPage _modOptionsPage;
        private ModOptionsPageButton _modOptionsPageButton;
        private int _modOptionsTabPageNumber;

        private PerScreen<IClickableMenu> _lastMenu = new();
        private List<int> _instancesWithOptionsPageOpen = new();
        private bool _windowResizing = false;

        public ModOptionsPageHandler(IModHelper helper, ModOptions options, bool showPersonalConfigButton)
        {
            if (showPersonalConfigButton)
            {
                helper.Events.Display.RenderingActiveMenu += OnRenderingMenu;
                helper.Events.Display.RenderedActiveMenu += OnRenderedMenu;
                GameRunner.instance.Window.ClientSizeChanged += OnWindowClientSizeChanged;
                helper.Events.Display.WindowResized += OnWindowResized;
            }
            _helper = helper;
            _showPersonalConfigButton = showPersonalConfigButton;
            var luckOfDay = new LuckOfDay(helper);
            var showBirthdayIcon = new ShowBirthdayIcon(helper);
            var showAccurateHearts = new ShowAccurateHearts(helper.Events);
            var locationOfTownsfolk = new LocationOfTownsfolk(helper, options);
            var showWhenAnimalNeedsPet = new ShowWhenAnimalNeedsPet(helper);
            var showCalendarAndBillboardOnGameMenuButton = new ShowCalendarAndBillboardOnGameMenuButton(helper);
            var showScarecrowAndSprinklerRange = new ShowItemEffectRanges(helper);
            var experienceBar = new ExperienceBar(helper);
            var showItemHoverInformation = new ShowItemHoverInformation(helper);
            var shopHarvestPrices = new ShopHarvestPrices(helper);
            var showQueenOfSauceIcon = new ShowQueenOfSauceIcon(helper);
            var showTravelingMerchant = new ShowTravelingMerchant(helper);
            var showRainyDayIcon = new ShowRainyDayIcon(helper);
            var showCropAndBarrelTime = new ShowCropAndBarrelTime(helper);
            var showToolUpgradeStatus = new ShowToolUpgradeStatus(helper);
            var showRobinBuildingStatusIcon = new ShowRobinBuildingStatusIcon(helper);
            var showSeasonalBerry = new ShowSeasonalBerry(helper);
            var showTodaysGift = new ShowTodaysGifts(helper);
            var showOasisClothes = new ShowOasisClothes(helper);

            _elementsToDispose = new List<IDisposable>()
            {
                luckOfDay,
                showBirthdayIcon,
                showAccurateHearts,
                locationOfTownsfolk,
                showWhenAnimalNeedsPet,
                showCalendarAndBillboardOnGameMenuButton,
                showCropAndBarrelTime,
                experienceBar,
                showItemHoverInformation,
                showTravelingMerchant,
                showRainyDayIcon,
                shopHarvestPrices,
                showQueenOfSauceIcon,
                showToolUpgradeStatus,
                showRobinBuildingStatusIcon,
                showSeasonalBerry,
                showOasisClothes
            };

            int whichOption = 1;
            Version thisVersion = Assembly.GetAssembly(this.GetType()).GetName().Version;
            _optionsElements.Add(new ModOptionsElement("UI Info Suite 2 v" + thisVersion.Major + "." + thisVersion.Minor + "." + thisVersion.Build));

            var luckIcon = new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowLuckIcon)), whichOption++, luckOfDay.ToggleOption, () => options.ShowLuckIcon, v => options.ShowLuckIcon = v);
            _optionsElements.Add(luckIcon);
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowExactValue)), whichOption++, luckOfDay.ToggleShowExactValueOption, () => options.ShowExactValue, v => options.ShowExactValue = v, luckIcon));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowLevelUpAnimation)), whichOption++, experienceBar.ToggleLevelUpAnimation, () => options.ShowLevelUpAnimation, v => options.ShowLevelUpAnimation = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowExperienceBar)), whichOption++, experienceBar.ToggleShowExperienceBar, () => options.ShowExperienceBar, v => options.ShowExperienceBar = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.AllowExperienceBarToFadeOut)), whichOption++, experienceBar.ToggleExperienceBarFade, () => options.AllowExperienceBarToFadeOut, v => options.AllowExperienceBarToFadeOut = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowExperienceGain)), whichOption++, experienceBar.ToggleShowExperienceGain, () => options.ShowExperienceGain, v => options.ShowExperienceGain = v));
            if (!_helper.ModRegistry.IsLoaded("Bouhm.NPCMapLocations"))
                _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowLocationOfTownsPeople)), whichOption++, locationOfTownsfolk.ToggleShowNPCLocationsOnMap, () => options.ShowLocationOfTownsPeople, v => options.ShowLocationOfTownsPeople = v));
            var birthdayIcon = new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowBirthdayIcon)), whichOption++, showBirthdayIcon.ToggleOption, () => options.ShowBirthdayIcon, v => options.ShowBirthdayIcon = v);
            _optionsElements.Add(birthdayIcon);
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.HideBirthdayIfFullFriendShip)), whichOption++, showBirthdayIcon.ToggleDisableOnMaxFriendshipOption, () => options.HideBirthdayIfFullFriendShip, v => options.HideBirthdayIfFullFriendShip = v, birthdayIcon));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowHeartFills)), whichOption++, showAccurateHearts.ToggleOption, () => options.ShowHeartFills, v => options.ShowHeartFills = v));
            var animalPetIcon = new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowAnimalsNeedPets)), whichOption++, showWhenAnimalNeedsPet.ToggleOption, () => options.ShowAnimalsNeedPets, v => options.ShowAnimalsNeedPets = v);
            _optionsElements.Add(animalPetIcon);
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.HideAnimalPetOnMaxFriendship)), whichOption++, showWhenAnimalNeedsPet.ToggleDisableOnMaxFriendshipOption, () => options.HideAnimalPetOnMaxFriendship, v => options.HideAnimalPetOnMaxFriendship = v, animalPetIcon));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.DisplayCalendarAndBillboard)), whichOption++, showCalendarAndBillboardOnGameMenuButton.ToggleOption, () => options.DisplayCalendarAndBillboard, v => options.DisplayCalendarAndBillboard = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowCropAndBarrelTooltip)), whichOption++, showCropAndBarrelTime.ToggleOption, () => options.ShowCropAndBarrelTooltip, v => options.ShowCropAndBarrelTooltip = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowItemEffectRanges)), whichOption++, showScarecrowAndSprinklerRange.ToggleOption, () => options.ShowItemEffectRanges, v => options.ShowItemEffectRanges = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowExtraItemInformation)), whichOption++, showItemHoverInformation.ToggleOption, () => options.ShowExtraItemInformation, v => options.ShowExtraItemInformation = v));
            var travellingMerchantIcon = new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowTravelingMerchant)), whichOption++, showTravelingMerchant.ToggleOption, () => options.ShowTravelingMerchant, v => options.ShowTravelingMerchant = v);
            _optionsElements.Add(travellingMerchantIcon);
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.HideMerchantWhenVisited)), whichOption++, showTravelingMerchant.ToggleHideWhenVisitedOption, () => options.HideMerchantWhenVisited, v => options.HideMerchantWhenVisited = v, travellingMerchantIcon));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowRainyDay)), whichOption++, showRainyDayIcon.ToggleOption, () => options.ShowRainyDay, v => options.ShowRainyDay = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowHarvestPricesInShop)), whichOption++, shopHarvestPrices.ToggleOption, () => options.ShowHarvestPricesInShop, v => options.ShowHarvestPricesInShop = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowWhenNewRecipesAreAvailable)), whichOption++, showQueenOfSauceIcon.ToggleOption, () => options.ShowWhenNewRecipesAreAvailable, v => options.ShowWhenNewRecipesAreAvailable = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowToolUpgradeStatus)), whichOption++, showToolUpgradeStatus.ToggleOption, () => options.ShowToolUpgradeStatus, v => options.ShowToolUpgradeStatus = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowRobinBuildingStatusIcon)), whichOption++, showRobinBuildingStatusIcon.ToggleOption, () => options.ShowRobinBuildingStatusIcon, v => options.ShowRobinBuildingStatusIcon = v));
            var seasonalBerryIcon = new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowSeasonalBerry)), whichOption++, showSeasonalBerry.ToggleOption, () => options.ShowSeasonalBerry, v => options.ShowSeasonalBerry = v);
            _optionsElements.Add(seasonalBerryIcon);
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowSeasonalBerryHazelnut)), whichOption++, showSeasonalBerry.ToggleHazelnutOption, () => options.ShowSeasonalBerryHazelnut, v => options.ShowSeasonalBerryHazelnut = v, seasonalBerryIcon));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowTodaysGifts)), whichOption++, showTodaysGift.ToggleOption, () => options.ShowTodaysGifts, v => options.ShowTodaysGifts = v));
            var showOasisClothesIcon = new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowOasisClothes)), whichOption++, showOasisClothes.ToggleOption, () => options.ShowOasisClothes, v => options.ShowOasisClothes = v);
            _optionsElements.Add(showOasisClothesIcon);
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowOasisClothesAll)), whichOption++, showOasisClothes.ToggleShowAllClothes, () => options.ShowOasisClothesAll, v => options.ShowOasisClothesAll = v, showOasisClothesIcon));
        }


        public void Dispose()
        {
            foreach (var item in _elementsToDispose)
                item.Dispose();
        }

        private void OnButtonLeftClicked(object sender, EventArgs e)
        {
            // Do not activate when an action is being remapped
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.readyToClose())
            {
                gameMenu.currentTab = _modOptionsTabPageNumber;
                Game1.playSound("smallSelect");
            }
        }

        // Early because it is called during Display.RenderingActiveMenu instead of later during Display.MenuChanged,
        private void EarlyOnMenuChanged(IClickableMenu? oldMenu, IClickableMenu? newMenu)
        {
            if (_showPersonalConfigButton)
            {
                // Remove from old menu
                if (oldMenu is GameMenu oldGameMenu)
                {
                    if (_modOptionsPage != null)
                    {
                        oldGameMenu.pages.Remove(_modOptionsPage);
                        _modOptionsPage = null;
                    }
                    if (_modOptionsPageButton != null)
                    {
                        _modOptionsPageButton.OnLeftClicked -= OnButtonLeftClicked;
                        _modOptionsPageButton = null;
                    }
                }

                // Add to new menu
                if (newMenu is GameMenu newGameMenu)
                {
                    // Both modOptions variables require Game1.activeClickableMenu to not be null.
                    if (_modOptionsPage == null)
                        _modOptionsPage = new ModOptionsPage(_optionsElements, _helper.Events);
                    if (_modOptionsPageButton == null)
                        _modOptionsPageButton = new ModOptionsPageButton(_helper.Events);

                    _modOptionsPageButton.OnLeftClicked += OnButtonLeftClicked;
                    List<IClickableMenu> tabPages = newGameMenu.pages;
                    _modOptionsTabPageNumber = tabPages.Count;
                    tabPages.Add(_modOptionsPage);
                }
            }
        }

        private void OnRenderingMenu(object sender, RenderingActiveMenuEventArgs e)
        {
            if (_showPersonalConfigButton)
            {
                // Trigger the "EarlyOnMenuChanged" event
                if (_lastMenu.Value != Game1.activeClickableMenu)
                {
                    EarlyOnMenuChanged(_lastMenu.Value, Game1.activeClickableMenu);
                    _lastMenu.Value = Game1.activeClickableMenu;
                }
                if (Game1.activeClickableMenu is GameMenu gameMenu)
                {
                    // Draw our tab icon behind the menu even if it is dimmed by the menu's transparent background,
                    // so that it still displays during transitions eg. when a letter is viewed in the collections tab
                    DrawButton(gameMenu);
                }
            }
        }

        private void OnRenderedMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (_showPersonalConfigButton
                && Game1.activeClickableMenu is GameMenu gameMenu
                // But don't render when the map is displayed...
                && !(gameMenu.currentTab == GameMenu.mapTab
                    // ...or when a letter is opened in the collection's page
                    || gameMenu.GetCurrentPage() is CollectionsPage cPage && cPage.letterviewerSubMenu != null
                ))
            {
                DrawButton(gameMenu);

                // Draw the game menu's hover text again so it displays above our tab
                if (!gameMenu.hoverText.Equals(""))
                    IClickableMenu.drawHoverText(Game1.spriteBatch, gameMenu.hoverText, Game1.smallFont);
            }
        }

        private void OnWindowClientSizeChanged(object sender, EventArgs e)
        {
            if (_showPersonalConfigButton)
            {
                _windowResizing = true;
                GameRunner.instance.ExecuteForInstances((Game1 instance) => {
                    if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == _modOptionsTabPageNumber)
                    {
                        // Temporarily change all open mod options pages to the game's options page
                        // because the GameMenu is recreated when the window is resized, before we can add
                        // our mod options page to GameMenu#pages.
                        gameMenu.currentTab = GameMenu.optionsTab;
                        _instancesWithOptionsPageOpen.Add(instance.instanceId);
                    }
                });
            }
        }

        private void OnWindowResized(object sender, EventArgs e)
        {
            if (_windowResizing) {
                _windowResizing = false;
                GameRunner.instance.ExecuteForInstances((Game1 instance) => {
                    if (_instancesWithOptionsPageOpen.Remove(instance.instanceId))
                    {
                        if (Game1.activeClickableMenu is GameMenu gameMenu)
                        {
                            gameMenu.currentTab = _modOptionsTabPageNumber;
                        }
                    }
                });
            }
        }

        private void DrawButton(GameMenu gameMenu)
        {
            _modOptionsPageButton.yPositionOnScreen = gameMenu.yPositionOnScreen + (gameMenu.currentTab == _modOptionsTabPageNumber ? 24 : 16);
            _modOptionsPageButton.draw(Game1.spriteBatch);
        }
    }
}
