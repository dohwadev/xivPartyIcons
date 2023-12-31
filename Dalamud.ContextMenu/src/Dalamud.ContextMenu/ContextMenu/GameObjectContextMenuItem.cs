﻿//------------------------------------------------------------------------------
// <auto-generated>
// Suppress stylecop rules to avoid fixing all these warnings right now.
// </auto-generated>
//------------------------------------------------------------------------------
namespace Dalamud.ContextMenu;

using Dalamud.Game.Text.SeStringHandling;

/// <summary>
/// A custom normal context menu item
/// </summary>
public class GameObjectContextMenuItem : CustomContextMenuItem<DalamudContextMenu.GameObjectContextMenuItemSelectedDelegate> {
    /// <summary>
    /// Create a new custom context menu item.
    /// </summary>
    /// <param name="name">the English name of the item, copied to other languages</param>
    /// <param name="action">the action to perform on click</param>
    /// <param name="useDalamudIndicator">append the dalamud indicator (red D) to the name.</param>
    public GameObjectContextMenuItem(SeString name, DalamudContextMenu.GameObjectContextMenuItemSelectedDelegate action, bool useDalamudIndicator = false) : base(name, action, useDalamudIndicator) {
    }
}