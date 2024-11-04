
# Thrall: Valheim AI Companion Mod

## Overview

Thrall is an AI language model-driven AI NPC companion to your Valheim adventures. This mod allows for dynamic interaction with your companion through voice commands, personality customization, and various gameplay-enhancing features. For example, you can tell the companion to go hunt 10 deer, and change its personality from Elon Musk to Hermione Granger (or anyone you fancy).

## Features

1.  Voice Interaction: Communicate with your companion using voice commands.
    
2.  Customizable Personality: Set your companion's personality to fit your desired roleplay experience.
    
3.  Command System: Issue various commands to your companion for different tasks.
    
4.  Resource Gathering: Your companion can collect resources for you.
    
5.  Combat Support: Engage your companion in battles against enemies.
    

## Youtube Gameplay Demo Video

[https://youtu.be/OVR0dhJw840](https://youtu.be/OVR0dhJw840)

## Youtube Trailer

[Thrall: Valheim AI NPC Mod Trailer](https://www.youtube.com/watch?v=QIr_Jp_D3wU)

## Installation

[Manual Installation]

1.  Ensure you have BepInEx and Jotunn installed for Valheim.
    
2.  Download Thrall from Thunderstore.
    
3.  Extract the mod files into your Valheim's BepInEx/plugins folder.
    
4.  Launch Valheim and enjoy your new companion!
    

[Install using r2modman]

1.  Download and run r2modman from Thunderstore.
    
2.  Find and install Thrall from the online mods section.
    
3.  Ensure BepInEx and Jotunn are installed as dependencies by the mod. If not, manually find and install them from the online mods section.
    
4.  Launch Valheim Modded and enjoy your new companion!
    

## Usage

Thrall shares the same max health and max stamina as the player. When the player eats food, your thrall also gains health from the regen.

### Voice Interaction

To interact with your companion using voice:

1.  [G]: Spawn/Dismiss Companion
    
2.  [T]: Push To Talk | Press and hold [T] to talk, release [T] when done.
    
3.  [H]: Order companion to harvest 20 wood.
    

### Personality Customization

To set your companion's personality:

1.  Open the companion menu when looking at companion (Default Key: Y).
    
2.  Navigate to the "Personality" tab.
    
3.  Enter a text paragraph describing the desired personality traits, background, and behavior of your companion.
    
4.  Click "Apply" to update your companion's personality.
    

### Command System

Use the following voice or chat commands to control your companion:

-   "Follow me": The companion will follow you closely.
    
-   "Patrol area": The companion will patrol and guard the immediate area.
    
-   "Gather [resource]": The companion will collect specified resources in the vicinity.
    
-   "Attack [target]": The companion will engage in combat with the specified target.
    

### Resource Gathering

To have your companion gather resources:

1.  Ensure you're in an area with the desired resources.
    
2.  Issue the command "Gather [quantity] [resource name]" (e.g., "Gather 20 wood", "Gather berries").
    
3.  Your companion will search the area and collect the specified resources.
    

### Combat

Your companion can assist in combat situations:

1.  When enemies are nearby, issue the command "Attack" or specify a target.
    
2.  Your companion will engage in combat using appropriate weapons and tactics.
    
3.  You can also command your companion to defend a specific area or protect you during battles.
    

## Compatibility

This mod is designed for single-player gameplay and may not function correctly when used alongside other mods. This mod is compatible with Valheim version [0.218.20] and requires BepInEx version [5.4.22] or higher, and Jotunn version [2.20.1] or higher.

## Known Issues

-   Companion is unable to craft items or build structures for now
    
-   Known mod conflicts
    

-   MagicPlugin
    
-   DragonRiders
    
-   Epic MMO System (shared keybinds)
    
-   VikingNPC
    

-   Mod does not officially support languages that are not English
    
-   Mod does not officially support multiplayer
    

## Support

For support, bug reports, or feature requests, please join the [Ego Discord community](https://discord.gg/egoai).

## Credits

Developed by Ego Live Inc. Reach out to us on discord: [https://discord.gg/egoai](https://discord.gg/egoai)  
Sahej Hundal (swag_disease on Discord)  
Cody Drake (DetectiveDrake on Discord)  
Connor Brennan (ConnorB on Discord)  
Peggy Wang (@peggy_wang on Twitter)  
Vishnu Hari (@simulacronist on Twitter)

## License

All rights reserved.


# Changelog

All notable changes to Thrall will be documented in this file.

## [1.0.9] - 2024-11-04

### Fixed

- 	Updated Thrall to work with latest version of Valheim

## [1.0.8] - 2024-10-18

### Added

-	More AI voices powered by Cartesia

### Fixed

- 	Unable to use "/" commands in chat

## [1.0.7] - 2024-08-31

### Fixed

- 	Resource database hotfix

## [1.0.6] - 2024-08-30

### Fixed

- 	Combat with long ranged weapons
- 	More bug fixes

### Added

-   New resource gathering algorithm

## [1.0.5] - 2024-08-29

### Fixed

-   NPC unable to pickup items when really far away from player
-	Improvements to resource harvesting system

### Added

-	Equip best weapon for targeted harvesting resource

## [1.0.4] - 2024-08-28

### Fixed

-   NPC picking up 2x item quantity
-   Inventory UI open/close bug fix
-   More bug fixes

## [1.0.3] - 2024-08-27

### Added

-   Ability to change all Thrall related keybinds

### Changed

-   README.md

## [1.0.2] - 2024-08-23

### Added

-   Added text chat communication with Thrall (type in Valheim chat).
-   Ability to change menu keybinds

### Changed

-   Resource harvesting logic (experimental)

### Fixed

-   Logging cleanup