using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.UIElements;

namespace UIInfoSuite2.Options;

/// <summary>
///   A mix between working around <see cref="GameMenu" /> to add our mod options page,
///   and loading the UI elements with their logic.
/// </summary>
internal class ModOptionsPageHandler : IDisposable
{
  // Our mod options tab is positioned approximately above the 11th inventory cell
  private const int downNeighborInInventory = 10;
  private const string optionsTabName = "uiinfosuite2";

  // For the map page workaround
  private readonly PerScreen<bool> _changeToOurTabAfterTick = new();
  private readonly List<IDisposable> _elementsToDispose;

  private readonly IModHelper _helper;

  // For the window resize workaround
  private readonly List<int> _instancesWithOptionsPageOpen = new();
  private readonly PerScreen<IClickableMenu?> _lastMenu = new();

  private readonly PerScreen<int?> _lastMenuTab = new();

  /// <summary>The mod options page added to <see cref="GameMenu.pages" />.</summary>
  private readonly PerScreen<ModOptionsPage?> _modOptionsPage = new();

  private readonly PerScreen<ModOptionsPageButton?> _modOptionsPageButton = new();

  /// <summary>
  ///   The clickable component for the mod options tab used by gamepad navigation.
  ///   <para>We don't add it to <see cref="GameMenu.tabs" /> because it messes up the game's logic.</para>
  /// </summary>
  private readonly PerScreen<ClickableComponent?> _modOptionsTab = new();

  private readonly PerScreen<int?> _modOptionsTabPageNumber = new();

  private readonly List<ModOptionsElement> _optionsElements = new();
  private readonly PerScreen<ModOptionsPageState?> _savedPageState = new();
  private readonly bool _showPersonalConfigButton;

  private bool _addOurTabBeforeTick;
  private bool _windowResizing;

  public ModOptionsPageHandler(IModHelper helper, ModOptions options, bool showPersonalConfigButton)
  {
    if (showPersonalConfigButton)
    {
      helper.Events.Input.ButtonPressed += OnButtonPressed;
      helper.Events.GameLoop.UpdateTicking += OnUpdateTicking;
      helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
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

    _elementsToDispose = new List<IDisposable>
    {
      luckOfDay,
      showBirthdayIcon,
      showAccurateHearts,
      locationOfTownsfolk,
      showWhenAnimalNeedsPet,
      showCalendarAndBillboardOnGameMenuButton,
      showCropAndBarrelTime,
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

    var whichOption = 1;
    _optionsElements.Add(new ModOptionsElement($"UI Info Suite 2 {GetVersionString(helper)}"));

    var luckIcon = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(options.ShowLuckIcon)),
      whichOption++,
      luckOfDay.ToggleOption,
      () => options.ShowLuckIcon,
      v => options.ShowLuckIcon = v
    );
    _optionsElements.Add(luckIcon);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.ShowExactValue)),
        whichOption++,
        luckOfDay.ToggleShowExactValueOption,
        () => options.ShowExactValue,
        v => options.ShowExactValue = v,
        luckIcon
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.ShowLevelUpAnimation)),
        whichOption++,
        experienceBar.ToggleLevelUpAnimation,
        () => options.ShowLevelUpAnimation,
        v => options.ShowLevelUpAnimation = v
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.ShowExperienceBar)),
        whichOption++,
        experienceBar.ToggleShowExperienceBar,
        () => options.ShowExperienceBar,
        v => options.ShowExperienceBar = v
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.AllowExperienceBarToFadeOut)),
        whichOption++,
        experienceBar.ToggleExperienceBarFade,
        () => options.AllowExperienceBarToFadeOut,
        v => options.AllowExperienceBarToFadeOut = v
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.ShowExperienceGain)),
        whichOption++,
        experienceBar.ToggleShowExperienceGain,
        () => options.ShowExperienceGain,
        v => options.ShowExperienceGain = v
      )
    );
    if (!_helper.ModRegistry.IsLoaded("Bouhm.NPCMapLocations"))
    {
      _optionsElements.Add(
        new ModOptionsCheckbox(
          _helper.SafeGetString(nameof(options.ShowLocationOfTownsPeople)),
          whichOption++,
          locationOfTownsfolk.ToggleShowNPCLocationsOnMap,
          () => options.ShowLocationOfTownsPeople,
          v => options.ShowLocationOfTownsPeople = v
        )
      );
    }

    var birthdayIcon = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(options.ShowBirthdayIcon)),
      whichOption++,
      showBirthdayIcon.ToggleOption,
      () => options.ShowBirthdayIcon,
      v => options.ShowBirthdayIcon = v
    );
    _optionsElements.Add(birthdayIcon);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.HideBirthdayIfFullFriendShip)),
        whichOption++,
        showBirthdayIcon.ToggleDisableOnMaxFriendshipOption,
        () => options.HideBirthdayIfFullFriendShip,
        v => options.HideBirthdayIfFullFriendShip = v,
        birthdayIcon
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.ShowHeartFills)),
        whichOption++,
        showAccurateHearts.ToggleOption,
        () => options.ShowHeartFills,
        v => options.ShowHeartFills = v
      )
    );
    var animalPetIcon = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(options.ShowAnimalsNeedPets)),
      whichOption++,
      showWhenAnimalNeedsPet.ToggleOption,
      () => options.ShowAnimalsNeedPets,
      v => options.ShowAnimalsNeedPets = v
    );
    _optionsElements.Add(animalPetIcon);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.HideAnimalPetOnMaxFriendship)),
        whichOption++,
        showWhenAnimalNeedsPet.ToggleDisableOnMaxFriendshipOption,
        () => options.HideAnimalPetOnMaxFriendship,
        v => options.HideAnimalPetOnMaxFriendship = v,
        animalPetIcon
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.DisplayCalendarAndBillboard)),
        whichOption++,
        showCalendarAndBillboardOnGameMenuButton.ToggleOption,
        () => options.DisplayCalendarAndBillboard,
        v => options.DisplayCalendarAndBillboard = v
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.ShowCropAndBarrelTooltip)),
        whichOption++,
        showCropAndBarrelTime.ToggleOption,
        () => options.ShowCropAndBarrelTooltip,
        v => options.ShowCropAndBarrelTooltip = v
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.ShowItemEffectRanges)),
        whichOption++,
        showScarecrowAndSprinklerRange.ToggleOption,
        () => options.ShowItemEffectRanges,
        v => options.ShowItemEffectRanges = v
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.ShowExtraItemInformation)),
        whichOption++,
        showItemHoverInformation.ToggleOption,
        () => options.ShowExtraItemInformation,
        v => options.ShowExtraItemInformation = v
      )
    );
    var travellingMerchantIcon = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(options.ShowTravelingMerchant)),
      whichOption++,
      showTravelingMerchant.ToggleOption,
      () => options.ShowTravelingMerchant,
      v => options.ShowTravelingMerchant = v
    );
    _optionsElements.Add(travellingMerchantIcon);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.HideMerchantWhenVisited)),
        whichOption++,
        showTravelingMerchant.ToggleHideWhenVisitedOption,
        () => options.HideMerchantWhenVisited,
        v => options.HideMerchantWhenVisited = v,
        travellingMerchantIcon
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.ShowRainyDay)),
        whichOption++,
        showRainyDayIcon.ToggleOption,
        () => options.ShowRainyDay,
        v => options.ShowRainyDay = v
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.ShowHarvestPricesInShop)),
        whichOption++,
        shopHarvestPrices.ToggleOption,
        () => options.ShowHarvestPricesInShop,
        v => options.ShowHarvestPricesInShop = v
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.ShowWhenNewRecipesAreAvailable)),
        whichOption++,
        showQueenOfSauceIcon.ToggleOption,
        () => options.ShowWhenNewRecipesAreAvailable,
        v => options.ShowWhenNewRecipesAreAvailable = v
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.ShowToolUpgradeStatus)),
        whichOption++,
        showToolUpgradeStatus.ToggleOption,
        () => options.ShowToolUpgradeStatus,
        v => options.ShowToolUpgradeStatus = v
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.ShowRobinBuildingStatusIcon)),
        whichOption++,
        showRobinBuildingStatusIcon.ToggleOption,
        () => options.ShowRobinBuildingStatusIcon,
        v => options.ShowRobinBuildingStatusIcon = v
      )
    );
    var seasonalBerryIcon = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(options.ShowSeasonalBerry)),
      whichOption++,
      showSeasonalBerry.ToggleOption,
      () => options.ShowSeasonalBerry,
      v => options.ShowSeasonalBerry = v
    );
    _optionsElements.Add(seasonalBerryIcon);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.ShowSeasonalBerryHazelnut)),
        whichOption++,
        showSeasonalBerry.ToggleHazelnutOption,
        () => options.ShowSeasonalBerryHazelnut,
        v => options.ShowSeasonalBerryHazelnut = v,
        seasonalBerryIcon
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(options.ShowTodaysGifts)),
        whichOption++,
        showTodaysGift.ToggleOption,
        () => options.ShowTodaysGifts,
        v => options.ShowTodaysGifts = v
      )
    );
    var showOasisClothesIcon = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(options.ShowOasisClothes)), 
      whichOption++, 
      showOasisClothes.ToggleOption, 
      () => options.ShowOasisClothes, 
      v => options.ShowOasisClothes = v
      );
      _optionsElements.Add(showOasisClothesIcon);
      _optionsElements.Add(
        new ModOptionsCheckbox(
          _helper.SafeGetString(nameof(options.ShowOasisClothesAll)), 
          whichOption++, 
          showOasisClothes.ToggleShowAllClothes, 
          () => options.ShowOasisClothesAll, 
          v => options.ShowOasisClothesAll = v, 
          showOasisClothesIcon
        )
      );
  }


  public void Dispose()
  {
    foreach (IDisposable item in _elementsToDispose)
    {
      item.Dispose();
    }
  }

  private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
  {
    if (Game1.activeClickableMenu is GameMenu gameMenu)
    {
      // Handle right trigger to switch to our mod options page
      // NB The game does the correct thing for left trigger so we don't need to implement it.
      if (e.Button == SButton.RightTrigger && !e.IsSuppressed())
      {
        if (gameMenu.currentTab + 1 == _modOptionsTabPageNumber.Value && gameMenu.readyToClose())
        {
          ChangeToOurTab(gameMenu);
          _helper.Input.Suppress(SButton.RightTrigger);
        }
      }

      // Based on GameMenu.receiveLeftClick and Game1.updateActiveMenu
      if ((e.Button == SButton.MouseLeft || e.Button == SButton.ControllerA) && !e.IsSuppressed())
      {
        // Workaround when exiting the map page because it calls GameMenu.changeTab and fails
        if (gameMenu.currentTab == GameMenu.mapTab && gameMenu.lastOpenedNonMapTab == _modOptionsTabPageNumber.Value)
        {
          _changeToOurTabAfterTick.Value = true;
          gameMenu.lastOpenedNonMapTab = GameMenu.optionsTab;
          ModEntry.MonitorObject.Log(
            $"{GetType().Name}: The map page is about to close and the menu will switch to our tab, applying workaround"
          );
        }

        if (!gameMenu.invisible && !GameMenu.forcePreventClose)
        {
          const bool uiScale = true;
          if (_modOptionsTab.Value?.containsPoint(Game1.getMouseX(uiScale), Game1.getMouseY(uiScale)) == true &&
              gameMenu.currentTab != _modOptionsTabPageNumber.Value &&
              gameMenu.readyToClose())
          {
            ChangeToOurTab(gameMenu);
          }
        }
      }
    }
  }

  private void OnUpdateTicking(object? sender, EventArgs e)
  {
    // Usually OnUpdateTicked catches the activeClickableMenu as soon as is it modified during the game update,
    // so we always add our tab early enough. This is to handle the window resize workaround.
    if (_addOurTabBeforeTick)
    {
      _addOurTabBeforeTick = false;
      GameRunner.instance.ExecuteForInstances(
        instance =>
        {
          if (_lastMenu.Value != Game1.activeClickableMenu)
          {
            EarlyOnMenuChanged(_lastMenu.Value, Game1.activeClickableMenu);
            _lastMenu.Value = Game1.activeClickableMenu;
          }
        }
      );
      ModEntry.MonitorObject.Log(
        $"{GetType().Name}: Our tab was added back as the final step of the window resize workaround"
      );
    }
  }

  private void OnUpdateTicked(object? sender, EventArgs e)
  {
    var gameMenu = Game1.activeClickableMenu as GameMenu;

    // The map was closed and the last opened tab was ours
    if (_changeToOurTabAfterTick.Value)
    {
      _changeToOurTabAfterTick.Value = false;
      if (gameMenu != null)
      {
        ChangeToOurTab(gameMenu);
        ModEntry.MonitorObject.Log($"{GetType().Name}: Changed back to our tab");
      }
    }

    if (_lastMenu.Value != Game1.activeClickableMenu)
    {
      EarlyOnMenuChanged(_lastMenu.Value, Game1.activeClickableMenu);
      _lastMenu.Value = Game1.activeClickableMenu;
    }

    if (_lastMenuTab.Value != gameMenu?.currentTab)
    {
      OnGameMenuTabChanged(gameMenu);
      _lastMenuTab.Value = gameMenu?.currentTab;
    }
  }

  // Early because it is called during GameLoop.UpdateTicked instead of later during Display.MenuChanged,
  private void EarlyOnMenuChanged(IClickableMenu? oldMenu, IClickableMenu? newMenu)
  {
    // Remove from old menu
    if (oldMenu is GameMenu oldGameMenu)
    {
      if (_modOptionsPage.Value != null)
      {
        oldGameMenu.pages.Remove(_modOptionsPage.Value);
        _modOptionsPage.Value = null;
      }

      if (_modOptionsPageButton.Value != null)
      {
        _modOptionsPageButton.Value = null;
      }

      _modOptionsTabPageNumber.Value = null;
      _modOptionsTab.Value = null;
    }

    // Add to new menu
    if (newMenu is GameMenu newGameMenu)
    {
      // Both modOptions variables require Game1.activeClickableMenu to not be null.
      if (_modOptionsPage.Value == null)
      {
        _modOptionsPage.Value = new ModOptionsPage(_optionsElements, _helper.Events);
      }

      if (_modOptionsPageButton.Value == null)
      {
        _modOptionsPageButton.Value = new ModOptionsPageButton();
        _modOptionsPageButton.Value.xPositionOnScreen = GetButtonXPosition(newGameMenu);
      }

      List<IClickableMenu> tabPages = newGameMenu.pages;
      _modOptionsTabPageNumber.Value = tabPages.Count;
      tabPages.Add(_modOptionsPage.Value);

      // Load last mod options page state
      if (_savedPageState.Value != null)
      {
        _modOptionsPage.Value.LoadState(_savedPageState.Value);
        _savedPageState.Value = null;
      }

      // NB For menu tabs, name is the "id" of the tab and label is the hover text.
      _modOptionsTab.Value = new ClickableComponent(
          new Rectangle(
            GetButtonXPosition(newGameMenu),
            newGameMenu.yPositionOnScreen + IClickableMenu.tabYPositionRelativeToMenuY + 64,
            64,
            64
          ),
          optionsTabName,
          "ui2_mod_options"
        ) // Placeholder label that shouldn't be displayed
        {
          // The exit page tab ID is 12347
          myID = 12348, leftNeighborID = 12347, tryDefaultIfNoDownNeighborExists = true, fullyImmutable = true
        };

      // Do not add our tab to GameMenu.tabs because GameMenu.draw will draw the menu tab incorrectly
      // when our page is the current tab.

      ClickableComponent? exitTab = newGameMenu.tabs.Find(tab => tab.myID == 12347);
      if (exitTab != null)
      {
        exitTab.rightNeighborID = _modOptionsTab.Value.myID;
        AddOurTabToClickableComponents(newGameMenu, _modOptionsTab.Value);
      }
      else
      {
        ModEntry.MonitorObject.LogOnce(
          $"{GetType().Name}: Did not find the ExitPage tab in the new GameMenu.tabs",
          LogLevel.Error
        );
      }
    }
  }

  private void OnGameMenuTabChanged(GameMenu? gameMenu)
  {
    if (gameMenu != null)
    {
      if (_modOptionsTab.Value != null)
      {
        // Update the downNeighborID for our tab
        // Based on GameMenu.setTabNeighborsForCurrentPage
        if (gameMenu.currentTab == GameMenu.inventoryTab)
        {
          _modOptionsTab.Value.downNeighborID = downNeighborInInventory;
        }
        else if (gameMenu.currentTab == GameMenu.exitTab)
        {
          _modOptionsTab.Value.downNeighborID = 535;
        }
        else
        {
          _modOptionsTab.Value.downNeighborID = ClickableComponent.SNAP_TO_DEFAULT;
        }

        AddOurTabToClickableComponents(gameMenu, _modOptionsTab.Value);
      }
    }
  }

  private void OnRenderingMenu(object? sender, RenderingActiveMenuEventArgs e)
  {
    if (Game1.activeClickableMenu is GameMenu gameMenu)
    {
      // Draw our tab icon behind the menu even if it is dimmed by the menu's transparent background,
      // so that it still displays during transitions eg. when a letter is viewed in the collections tab
      DrawButton(gameMenu);
    }
  }

  private void OnRenderedMenu(object? sender, RenderedActiveMenuEventArgs e)
  {
    if (Game1.activeClickableMenu is GameMenu gameMenu
        // But don't render when the map is displayed...
        &&
        !(gameMenu.currentTab == GameMenu.mapTab
          // ...or when a letter is opened in the collection's page
          ||
          (gameMenu.GetCurrentPage() is CollectionsPage cPage && cPage.letterviewerSubMenu != null)))
    {
      DrawButton(gameMenu);

      Tools.DrawMouseCursor();

      // Draw the game menu's hover text again so it displays above our tab
      if (!gameMenu.hoverText.Equals(""))
      {
        IClickableMenu.drawHoverText(Game1.spriteBatch, gameMenu.hoverText, Game1.smallFont);
      }

      // Draw our tab's hover text
      if (_modOptionsTab.Value?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) == true)
      {
        Translation tooltip = _helper.Translation.Get(LanguageKeys.OptionsTabTooltip).Default("UI Info Mod Options");
        IClickableMenu.drawHoverText(Game1.spriteBatch, tooltip, Game1.smallFont);

        if (!gameMenu.hoverText.Equals(""))
        {
          ModEntry.MonitorObject.LogOnce(
            $"{GetType().Name}: Both our mod options tab and the game are displaying hover text",
            LogLevel.Warn
          );
        }
      }
    }
  }

  private void OnWindowClientSizeChanged(object? sender, EventArgs e)
  {
    _windowResizing = true;
    GameRunner.instance.ExecuteForInstances(
      instance =>
      {
        if (Game1.activeClickableMenu is GameMenu gameMenu
            // NB SMAPI seems to use the game's instanceID as the screenID for PerScreen
            &&
            gameMenu.currentTab == _modOptionsTabPageNumber.GetValueForScreen(instance.instanceId))
        {
          // Temporarily change all open mod options pages to the game's options page
          // because the GameMenu is recreated when the window is resized, before we can add
          // our mod options page to GameMenu#pages.
          if (gameMenu.GetCurrentPage() is ModOptionsPage modOptionsPage)
          {
            _savedPageState.Value = new ModOptionsPageState();
            modOptionsPage.SaveState(_savedPageState.Value);
          }

          gameMenu.currentTab = GameMenu.optionsTab;
          _instancesWithOptionsPageOpen.Add(instance.instanceId);
        }
      }
    );
    if (_instancesWithOptionsPageOpen.Count > 0)
    {
      ModEntry.MonitorObject.Log(
        $"{GetType().Name}: The window is being resized while our options page is opened, applying workaround"
      );
    }
  }

  // This seems to be called after Display.Rendered and Update.Ticking ie. between frames
  private void OnWindowResized(object? sender, EventArgs e)
  {
    if (_windowResizing)
    {
      _windowResizing = false;
      if (_instancesWithOptionsPageOpen.Count > 0)
      {
        GameRunner.instance.ExecuteForInstances(
          instance =>
          {
            if (_instancesWithOptionsPageOpen.Remove(instance.instanceId))
            {
              if (Game1.activeClickableMenu is GameMenu gameMenu)
              {
                gameMenu.currentTab = (int)_modOptionsTabPageNumber.GetValueForScreen(instance.instanceId)!;
              }
            }
          }
        );

        ModEntry.MonitorObject.Log($"{GetType().Name}: The window was resized, reverting to our tab");
        _addOurTabBeforeTick = true;
      }
    }
  }

  /// <summary>Based on <see cref="GameMenu.changeTab" /></summary>
  private void ChangeToOurTab(GameMenu gameMenu)
  {
    var modOptionsTabIndex = (int)_modOptionsTabPageNumber.Value!;
    gameMenu.currentTab = modOptionsTabIndex;
    gameMenu.lastOpenedNonMapTab = modOptionsTabIndex;
    gameMenu.initializeUpperRightCloseButton();
    gameMenu.invisible = false;
    Game1.playSound("smallSelect");

    // We don't need to call GameMenu.AddTabsToClickableComponents because populateClickableComponentList already does it for us.
    // However, we must add our mod options tab now because snapToDefaultClickableComponent might use it.
    gameMenu.GetCurrentPage().populateClickableComponentList();
    AddOurTabToClickableComponents(gameMenu, _modOptionsTab.Value!);

    gameMenu.setTabNeighborsForCurrentPage();
    if (Game1.options.SnappyMenus)
    {
      gameMenu.snapToDefaultClickableComponent();
    }
  }

  /// <summary>
  ///   Add the tab to the current menu page's clickable components
  ///   <para>It initializes the component list if needed, and doesn't add the tab if it is already present.</para>
  /// </summary>
  private void AddOurTabToClickableComponents(GameMenu gameMenu, ClickableComponent modOptionsTab)
  {
    IClickableMenu currentPage = gameMenu.GetCurrentPage()!;
    if (currentPage.allClickableComponents == null)
    {
      currentPage.populateClickableComponentList();
    }

    if (!currentPage.allClickableComponents!.Contains(modOptionsTab))
    {
      currentPage.allClickableComponents.Add(modOptionsTab);
    }
  }

  private int GetButtonXPosition(GameMenu gameMenu)
  {
    return gameMenu.xPositionOnScreen + gameMenu.width - 165;
  }

  private void DrawButton(GameMenu gameMenu)
  {
    ModOptionsPageButton button = _modOptionsPageButton.Value!;
    button.yPositionOnScreen = gameMenu.yPositionOnScreen +
                               (gameMenu.currentTab == _modOptionsTabPageNumber.Value ? 24 : 16);
    button.draw(Game1.spriteBatch);
  }

  /// <summary>
  ///   Tries hard to return a version string for the mod like "v2.2.9"
  ///   <para>
  ///     Try to get the version from the SMAPI manifest; then from the assembly in which case it is formatted as v=...;
  ///     then give up and return a default value.
  ///   </para>
  /// </summary>
  private static string GetVersionString(IModHelper helper)
  {
    IModInfo? modInfo = helper.ModRegistry.Get(helper.ModRegistry.ModID);
    if (modInfo != null)
    {
      return $"v{modInfo.Manifest.Version}";
    }

    ModEntry.MonitorObject.LogOnce(
      $"{typeof(ModOptionsPageHandler).Name}: Couldn't retrieve our own mod information",
      LogLevel.Info
    );

    Version? assemblyVersion = Assembly.GetAssembly(typeof(ModEntry))?.GetName().Version;
    if (assemblyVersion != null)
    {
      return $"v={assemblyVersion}";
    }

    ModEntry.MonitorObject.LogOnce(
      $"{typeof(ModOptionsPageHandler).Name}: Couldn't retrieve our own assembly version information"
    );

    return "(unknown version)";
  }
}

/// <summary>Data that is saved and restored when the the game menu is resized</summary>
internal class ModOptionsPageState
{
  public int? currentComponent;
  public int? currentIndex;
}
