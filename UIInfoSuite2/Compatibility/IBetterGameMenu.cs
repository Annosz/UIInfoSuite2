#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

using StardewValley;
using StardewValley.Menus;

namespace Leclair.Stardew.BetterGameMenu;


/// <summary>
/// A page created event is emitted whenever a new page
/// is created for a tab by Better Game Menu.
/// </summary>
public interface IPageCreatedEvent
{

  /// <summary>
  /// The Better Game Menu instance involved in the event. You
  /// can use <see cref="IBetterGameMenuApi.AsMenu(IClickableMenu)"/>
  /// to get a more useful interface for this menu.
  /// </summary>
  IClickableMenu Menu { get; }

  /// <summary>
  /// The id of the tab the page was created for.
  /// </summary>
  string Tab { get; }

  /// <summary>
  /// The id of the provider the page was created with.
  /// </summary>
  string Source { get; }

  /// <summary>
  /// The new page that was just created.
  /// </summary>
  IClickableMenu Page { get; }

  /// <summary>
  /// If the page was previously created and is being replaced,
  /// this will be the old page instance. Otherwise, this will
  /// be <c>null</c>.
  /// </summary>
  IClickableMenu? OldPage { get; }

}


/// <summary>
/// This interface represents a Better Game Menu. 
/// </summary>
public interface IBetterGameMenu
{
  /// <summary>
  /// The <see cref="IClickableMenu"/> instance for this game menu. This is
  /// the same object, but with a different type. This property is included
  /// for convenience due to how API proxying works.
  /// </summary>
  IClickableMenu Menu { get; }

  /// <summary>
  /// Whether or not the menu is currently drawing itself. This is typically
  /// always <c>false</c> except when viewing the <c>Map</c> tab.
  /// </summary>
  bool Invisible { get; set; }

  /// <summary>
  /// A list of ids of the currently visible tabs.
  /// </summary>
  IReadOnlyList<string> VisibleTabs { get; }

  /// <summary>
  /// The id of the currently active tab.
  /// </summary>
  string CurrentTab { get; }

  /// <summary>
  /// The <see cref="IClickableMenu"/> instance for the currently active tab.
  /// This may be <c>null</c> if the page instance for the currently active
  /// tab is still being initialized.
  /// </summary>
  IClickableMenu? CurrentPage { get; }

  /// <summary>
  /// Whether or not the currently displayed page is an error page. Error
  /// pages are used when a tab implementation's GetPageInstance method
  /// throws an exception.
  /// </summary>
  bool CurrentTabHasErrored { get; }

  /// <summary>
  /// Try to get the source for the specific tab.
  /// </summary>
  /// <param name="target">The id of the tab to get the source of.</param>
  /// <param name="source">The unique ID of the mod that registered the
  /// implementation being used, or <c>stardew</c> if the base game's
  /// implementation is being used.</param>
  /// <returns>Whether or not the tab is registered with the system.</returns>
  bool TryGetSource(string target, [NotNullWhen(true)] out string? source);

  /// <summary>
  /// Try to get the <see cref="IClickableMenu"/> instance for a specific tab.
  /// </summary>
  /// <param name="target">The id of the tab to get the page for.</param>
  /// <param name="page">The page instance, if one exists.</param>
  /// <param name="forceCreation">If set to true, an instance will attempt to
  /// be created if one has not already been created.</param>
  /// <returns>Whether or not a page instance for that tab exists.</returns>
  bool TryGetPage(string target, [NotNullWhen(true)] out IClickableMenu? page, bool forceCreation = false);

  /// <summary>
  /// Attempt to change the currently active tab to the target tab.
  /// </summary>
  /// <param name="target">The id of the tab to change to.</param>
  /// <param name="playSound">Whether or not to play a sound.</param>
  /// <returns>Whether or not the tab was changed successfully.</returns>
  bool TryChangeTab(string target, bool playSound = true);

  /// <summary>
  /// Force the menu to recalculate the visible tabs. This will not recreate
  /// <see cref="IClickableMenu"/> instances, but can be used to cause an
  /// inactive tab to be removed, or a previously hidden tab to be added.
  /// This can also be used to update tab decorations if necessary.
  /// </summary>
  /// <param name="target">Optionally, a specific tab to update rather than
  /// updating all tabs.</param>
  void UpdateTabs(string? target = null);

}


/// <summary>
/// This enum is included for reference and has the order value for
/// all the default tabs from the base game. These values are intentionally
/// spaced out to allow for modded tabs to be inserted at specific points.
/// </summary>
public enum BetterGameMenuTabs
{
  Inventory = 0,
  Skills = 20,
  Social = 40,
  Map = 60,
  Crafting = 80,
  Animals = 100,
  Powers = 120,
  Collections = 140,
  Options = 160,
  Exit = 200
}


public interface IBetterGameMenuApi
{

  /// <summary>
  /// A delegate for drawing something onto the screen.
  /// </summary>
  /// <param name="batch">The <see cref="SpriteBatch"/> to draw with.</param>
  /// <param name="bounds">The region where the thing should be drawn.</param>
  public delegate void DrawDelegate(SpriteBatch batch, Rectangle bounds);

  #region Helpers

  /// <summary>
  /// Create a draw delegate that draws the provided texture to the
  /// screen. This supports basic animations if required.
  /// </summary>
  /// <param name="texture">The texture to draw from.</param>
  /// <param name="source">The source rectangle to draw.</param>
  /// <param name="scale">The scale to draw the source at.</param>
  /// <param name="frames">The number of frames to draw.</param>
  /// <param name="frameTime">The amount of time each frame should be displayed.</param>
  DrawDelegate CreateDraw(Texture2D texture, Rectangle source, float scale = 1f, int frames = 1, int frameTime = 16);

  #endregion

  #region Tab Registration

  /// <summary>
  /// Register a new tab with the system. 
  /// </summary>
  /// <param name="id">The id of the tab to add.</param>
  /// <param name="order">The order of this tab relative to other tabs.
  /// See <see cref="BetterGameMenuTabs"/> for an example of existing values.</param>
  /// <param name="getDisplayName">A method that returns the display name of
  /// this tab, to be displayed in a tool-tip to the user.</param>
  /// <param name="getIcon">A method that returns an icon to be displayed
  /// on the tab UI for this tab, expecting both a texture and a Rectangle.</param>
  /// <param name="priority">The priority of the default page instance
  /// provider for this tab. When multiple page instance providers are
  /// registered, and the user hasn't explicitly chosen one, then the
  /// one with the highest priority is used. Please note that a given
  /// mod can only register one provider for any given tab.</param>
  /// <param name="getPageInstance">A method that returns a page instance
  /// for the tab. This should never return a <c>null</c> value.</param>
  /// <param name="getDecoration">A method that returns a decoration for
  /// the tab UI for this tab. This can be used to, for example, add a
  /// sparkle to a tab to indicate that new content is available. The
  /// expected output is either <c>null</c> if no decoration should be
  /// displayed, or a texture, rectangle, number of animation frames
  /// to display, and delay between frame advancements. Please note that
  /// the decoration will be automatically cleared when the user navigates
  /// to the tab.</param>
  /// <param name="getTabVisible">A method that returns whether or not the
  /// tab should be visible in the menu. This is called whenever a menu is
  /// opened, as well as when <see cref="IBetterGameMenu.UpdateTabs(string?)"/>
  /// is called.</param>
  /// <param name="getMenuInvisible">A method that returns the value that the
  /// game menu should set its <see cref="IBetterGameMenu.Invisible"/> flag
  /// to when this is the active tab.</param>
  /// <param name="getWidth">A method that returns a specific width to use when
  /// rendering this tab, in case the page instance requires a different width
  /// than the standard value.</param>
  /// <param name="getHeight">A method that returns a specific height to use
  /// when rendering this tab, in case the page instance requires a different
  /// height than the standard value.</param>
  /// <param name="onResize">A method that is called when the game window is
  /// resized, in addition to the standard <see cref="IClickableMenu.gameWindowSizeChanged(Rectangle, Rectangle)"/>.
  /// This can be used to recreate a menu page if necessary by returning a
  /// new <see cref="IClickableMenu"/> instance. Several menus in the vanilla
  /// game use this logic.</param>
  /// <param name="onClose">A method that is called whenever a page instance
  /// is cleaned up. The standard Game Menu doesn't call <see cref="IClickableMenu.cleanupBeforeExit"/>
  /// of its pages, and only calls <see cref="IClickableMenu.emergencyShutDown"/>
  /// of the currently active tab, and we're keeping that behavior for
  /// compatibility. This method will always be called. This includes calling
  /// it for menus that were replaced by the <c>onResize</c> method.</param>
  void RegisterTab(
    string id,
    int order,
    Func<string> getDisplayName,
    Func<(DrawDelegate DrawMethod, bool DrawBackground)> getIcon,
    int priority,
    Func<IClickableMenu, IClickableMenu> getPageInstance,
    Func<DrawDelegate?>? getDecoration = null,
    Func<bool>? getTabVisible = null,
    Func<bool>? getMenuInvisible = null,
    Func<int, int>? getWidth = null,
    Func<int, int>? getHeight = null,
    Func<(IClickableMenu Menu, IClickableMenu OldPage), IClickableMenu?>? onResize = null,
    Action<IClickableMenu>? onClose = null
  );

  #endregion

  #region Menu Class Access

  /// <summary>
  /// The active screen's current Better Game Menu, if one is open,
  /// else <c>null</c>.
  /// </summary>
  IBetterGameMenu? ActiveMenu { get; }

  /// <summary>
  /// Attempt to cast the provided menu into an <see cref="IBetterGameMenu"/>.
  /// This can be useful if you're working with a menu that isn't currently
  /// assigned to <see cref="Game1.activeClickableMenu"/>.
  /// </summary>
  /// <param name="menu">The menu to attempt to cast</param>
  IBetterGameMenu? AsMenu(IClickableMenu menu);

  #endregion

  #region Menu Events

  public delegate void PageCreatedDelegate(IPageCreatedEvent evt);

  /// <summary>
  /// This event fires whenever a new page instance is created. This can happen
  /// the first time a page is accessed, whenever something calls
  /// <see cref="TryGetPage(string, out IClickableMenu?, bool)"/> with the
  /// <c>forceCreation</c> flag set to true, or when the menu has been resized
  /// and the tab implementation's <c>OnResize</c> method returned a new
  /// page instance.
  /// </summary>
  void OnPageCreated(PageCreatedDelegate handler, EventPriority priority = EventPriority.Normal);

  /// <summary>
  /// Unregister a handler for the PageCreated event.
  /// </summary>
  void OffPageCreated(PageCreatedDelegate handler);

  #endregion

}
