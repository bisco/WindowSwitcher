# WindowSwitcher

A lightweight vertical window switcher for Windows.

WindowSwitcher docks to the left edge of the screen and provides quick access to all open windows. Inspired by sidebar-style launchers and task switchers, it stays out of the way until you hover over it.

![screenshot](docs/screenshot.png)

## Features

* Vertical sidebar docked to the left edge of the screen
* Automatically expands on mouse hover
* Displays window titles and application icons
* Highlights the currently active window
* Switch windows with a single click
* Close windows from the context menu
* Global hotkey (`Ctrl + Shift + Space`)
* Hidden from Alt+Tab
* Reserves screen space using the Windows AppBar API

## Installation

Download the latest release from the Releases page.

Or build from source:

```bash
git clone https://github.com/bisco/WindowSwitcher.git
cd WindowSwitcher
```

Open the solution in Visual Studio and build.

## Usage

* Move the mouse to the left edge of the screen.
* The switcher expands automatically.
* Click a window to activate it.
* Right-click a window to close it.
* Press `Ctrl + Shift + Space` to toggle visibility.

## Why?

Windows provides Alt+Tab and the taskbar, but neither is optimized for users who frequently switch between many windows throughout the day.

WindowSwitcher offers a persistent, space-efficient alternative that is always one mouse movement away.

## Requirements

* Windows 10 / Windows 11
* .NET 8

## License

MIT License
