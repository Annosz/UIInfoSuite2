using StardewModdingAPI;
using StardewModdingAPI.Events;
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
        private List<ModOptionsElement> _optionsElements = new();
        private readonly List<IDisposable> _elementsToDispose;
        private ModOptionsPageButton _modOptionsPageButton;
        private ModOptionsPage _modOptionsPage;
        private readonly IModHelper _helper;
        private readonly bool _showPersonalConfigButton;

        private int _modOptionsTabPageNumber;

        public ModOptionsPageHandler(IModHelper helper, ModOptions options, bool showPersonalConfigButton)
        {
            if (showPersonalConfigButton)
            {
                helper.Events.Display.MenuChanged += ToggleModOptions;
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
            var showHoverCraftableInCollection = new ShowHoverCraftableInCollection(helper);
            var shopHarvestPrices = new ShopHarvestPrices(helper);
            var showQueenOfSauceIcon = new ShowQueenOfSauceIcon(helper);
            var showTravelingMerchant = new ShowTravelingMerchant(helper);
            var showRainyDayIcon = new ShowRainyDayIcon(helper);
            var showCropAndBarrelTime = new ShowCropAndBarrelTime(helper);
            var showToolUpgradeStatus = new ShowToolUpgradeStatus(helper);
            var showRobinBuildingStatusIcon = new ShowRobinBuildingStatusIcon(helper);
            var showSeasonalBerry = new ShowSeasonalBerry(helper);
            var showTodaysGift = new ShowTodaysGifts(helper);

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
                showSeasonalBerry
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
            var BirthdayIcon = new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowBirthdayIcon)), whichOption++, showBirthdayIcon.ToggleOption, () => options.ShowBirthdayIcon, v => options.ShowBirthdayIcon = v);
            _optionsElements.Add(BirthdayIcon);
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.HideBirthdayIfFullFriendShip)), whichOption++, showBirthdayIcon.ToggleDisableOnMaxFriendshipOption, () => options.HideBirthdayIfFullFriendShip, v => options.HideBirthdayIfFullFriendShip = v, BirthdayIcon));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowHeartFills)), whichOption++, showAccurateHearts.ToggleOption, () => options.ShowHeartFills, v => options.ShowHeartFills = v));
            var animalPetIcon = new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowAnimalsNeedPets)), whichOption++, showWhenAnimalNeedsPet.ToggleOption, () => options.ShowAnimalsNeedPets, v => options.ShowAnimalsNeedPets = v);
            _optionsElements.Add(animalPetIcon);
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.HideAnimalPetOnMaxFriendship)), whichOption++, showWhenAnimalNeedsPet.ToggleDisableOnMaxFirendshipOption, () => options.HideAnimalPetOnMaxFriendship, v => options.HideAnimalPetOnMaxFriendship = v, animalPetIcon));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.DisplayCalendarAndBillboard)), whichOption++, showCalendarAndBillboardOnGameMenuButton.ToggleOption, () => options.DisplayCalendarAndBillboard, v => options.DisplayCalendarAndBillboard = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowCropAndBarrelTooltip)), whichOption++, showCropAndBarrelTime.ToggleOption, () => options.ShowCropAndBarrelTooltip, v => options.ShowCropAndBarrelTooltip = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowItemEffectRanges)), whichOption++, showScarecrowAndSprinklerRange.ToggleOption, () => options.ShowItemEffectRanges, v => options.ShowItemEffectRanges = v));
            _optionsElements.Add(new ModOptionsCheckbox(_helper.SafeGetString(nameof(options.ShowExtraItemInformation)), whichOption++, toggleHoverStuff, () => options.ShowExtraItemInformation, v => options.ShowExtraItemInformation = v));
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
        
        
            void toggleHoverStuff(bool isChecked)
            {
                showHoverCraftableInCollection.ToggleOption(isChecked);
                showItemHoverInformation.ToggleOption(isChecked);
            }

        }


        public void Dispose()
        {
            foreach (var item in _elementsToDispose)
                item.Dispose();
        }

        private void OnButtonLeftClicked(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu
                && !GameMenu.forcePreventClose // Do not activate when an action is being remapped
                && gameMenu.GetCurrentPage().readyToClose())
            {
                SetActiveClickableMenuToModOptionsPage();
                Game1.playSound("smallSelect");
            }
        }

        private void ToggleModOptions(object sender, MenuChangedEventArgs e)
        {
            // Remove from old menu
            if (e.OldMenu != null)
            {
                _helper.Events.Display.RenderingActiveMenu -= OnRenderingMenu;
                _helper.Events.Display.RenderedActiveMenu -= OnRenderedMenu;
                if (_modOptionsPageButton != null)
                    _modOptionsPageButton.OnLeftClicked -= OnButtonLeftClicked;

                if (e.OldMenu is GameMenu gameMenu)
                {
                    List<IClickableMenu> tabPages = gameMenu.pages;
                    tabPages.Remove(_modOptionsPage);
                }
            }

            // Add to new menu
            if (e.NewMenu is GameMenu newMenu)
            {
                if (_modOptionsPageButton == null)
                {
                    _modOptionsPage = new ModOptionsPage(_optionsElements, _helper.Events);
                    _modOptionsPageButton = new ModOptionsPageButton(_helper.Events);
                }

                _helper.Events.Display.RenderingActiveMenu += OnRenderingMenu;
                _helper.Events.Display.RenderedActiveMenu += OnRenderedMenu;
                _modOptionsPageButton.OnLeftClicked += OnButtonLeftClicked;
                List<IClickableMenu> tabPages = newMenu.pages;

                _modOptionsTabPageNumber = tabPages.Count;
                tabPages.Add(_modOptionsPage);
            }
        }

        private void SetActiveClickableMenuToModOptionsPage()
        {
            if (Game1.activeClickableMenu is GameMenu menu)
                menu.currentTab = _modOptionsTabPageNumber;
        }

        private void OnRenderingMenu(object sender, RenderingActiveMenuEventArgs e)
        {
            if (_showPersonalConfigButton && Game1.activeClickableMenu is GameMenu gameMenu)
            {
                // Draw our tab icon behind the menu even if it is dimmed by the menu's transparent background,
                // so that it still displays during transitions eg. when a letter is viewed in the collections tab
                DrawButton(gameMenu);
            }
        }

        private void OnRenderedMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu &&
                gameMenu.currentTab != 3 && // Do not render when the map is showing
                                            // Do not render if a letter is open in the collection's page
                !(gameMenu.currentTab == 5 && gameMenu.GetCurrentPage() is CollectionsPage cPage && cPage.letterviewerSubMenu != null) &&
                _showPersonalConfigButton) // Only render when it is enabled in the config.json
            {
                DrawButton(gameMenu);

                // Draw the game menu's hover text again so it displays above our tab
                if (!gameMenu.hoverText.Equals(""))
                    IClickableMenu.drawHoverText(Game1.spriteBatch, gameMenu.hoverText, Game1.smallFont);
            }
        }

        private void DrawButton(GameMenu gameMenu)
        {
            _modOptionsPageButton.yPositionOnScreen = gameMenu.yPositionOnScreen + (gameMenu.currentTab == _modOptionsTabPageNumber ? 24 : 16);
            _modOptionsPageButton.draw(Game1.spriteBatch);
        }
    }
}
