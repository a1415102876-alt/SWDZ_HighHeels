# Swdz Highheels

English | [简体中文](./Readme.zh-CN.md) | [日本語](./README.ja.md)

A high-heel and pose-adjustment plugin for **Aicomi / Samabake / Honeycome (IL2CPP + BepInEx 6)**.  
It can automatically apply shoe-based height/ankle/toe settings, and save independent pose presets for different shoes and animations.

## Feature Overview

- Automatically detects shoes and applies presets (height, ankle angle, toe angle)
- Real-time parameter tuning in Edit Mode (GUI)
- Supports pose adjustments (hip offset, thigh angle, knee angle)
- Automatically disables heel and pose extra adjustments when shoes are removed (shoe model hidden/disabled)
- Continuously applies pose adjustments in H scenes

## Requirements

- BepInEx 6 (IL2CPP version)

## Installation

1. Copy the extracted `BepInEx` folder directly into your game root directory.  
   The plugin DLL should be located at:
   `BepInEx/plugins/SwdzHighheels`
2. Launch the game once. The plugin will auto-generate its config directory:
   `BepInEx/plugins/SwdzHighheels/config/`

## Usage

### Open the UI

- Default hotkey: `H`
- You can change it in config (`GUI Key`)

### Basic Workflow

1. Equip shoes and confirm shoe info is detected in the UI
2. Enable `Edit Mode`
3. Adjust:
   - `Height`
   - `Ankle`
   - `Toe`
4. Click `Save Config` to save shoe preset

### Pose Adjustment Workflow

1. In Edit Mode, enable `Enable Pose Adjust`
2. Adjust:
   - `Pose Hip Offset`
   - `Pose Thigh Angle`
   - `Pose Knee Angle`
3. Click `Save Pose Config` to save pose preset

> Pose presets are now loaded by **shoe + vertex count + pose**.  
> The same pose can use different parameters for different shoes.

## Preset File Rules

### Shoe Presets (config root)

- File name: `<shoe_display_name>_<vertex_count>.json`
- Example: `boots1_1196.json`

### Pose Presets (`config/animation`)

- File name: `pose_<composite_key>.json`
- Composite key format: `shoe_name#vertex_count@@pose_name`

## "Currently Loaded Preset" in UI

The UI shows:

- `Shoe Preset`: currently matched shoe preset name
- `Pose Preset`: currently matched pose preset key

If it shows `None`, no preset is matched for the current state.
