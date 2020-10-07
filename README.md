# Chen's Classic Items

## Description

An extension mod for **ThinkInvis.ClassicItems**!

This mod also adds items from Risk of Rain 1 that did not make it to Risk of Rain 2.

Not only that, but this mod will also add classic items from different games.

## Installation

Use **r2modman** mod manager to install this mod.

### Current Additions
#### Equipment
- [RoR1] Drone Repair Kit
- [RoR1] Instant Minefield
#### Tier 1
- [RoR1] Mortar Tube
#### Tier 2
- [RoR1] Arms Race
- [RoR1] Dead Man's Foot
- [RoR1] Panic Mines
#### Tier 3
- [RoR1] AtG Missile Mk. 2
- [Gradius] Gradius' Option

### More Information

Check out the original ClassicItems made by ThinkInvisible.

Thunderstore: https://thunderstore.io/package/ThinkInvis/ClassicItems/

GitHub: https://github.com/ThinkInvis/RoR2-ClassicItems

## Changelog

**1.3.0**
- Implement Drone Repair Kit!
- Remove the the sync logging as it apparently caused heavy lag.
- Add a config setting where the Options of Flame Drones will have reduced quality of effects to lessen the stress of processing and syncing.
- Add lore for Instant Minefield.
- Slight adjustments for config options in regards with TILER2 for correctness.

**1.2.1**
- Add logbook entry for Arms Race.
- Fix a terrible sync bug due to error in the modder's part.
- Nerfed base stats of Arms Race because it was too powerful. It is still configurable.

**1.2.0**
- Implement Arms Race!

**1.1.1**
- Change implementation of syncing Options and related effects due to reports of FPS drops.

**1.1.0**
- Implement Mortar Tube!
- Add more ItemStats details for all items.
- Allow Turrets to always update their position.

**1.0.3**
- Add a condition where the host is required to wait for all clients to be ready before sending the sync commands. This ensures that all clients will be synced.
- Remove a bunch of logs that are otherwise useless. Retained only some that may still cause bugs to help in bug reports.
- Improve code.

**1.0.2**
- Fix the Drones with Options hard crashing the game when entering bazaar.
- Fix the Options being duplicated when the player is revived in a stage by any means.
- Fix Multiplayer desync issues regarding the Option Spawning upon item pickup of Gradius' Option.
- Implement a queueing system for syncing to lessen desync and lessen bandwidth usage.
- Improve the code by letting linear behavior into client sided execution to lessen bandwidth usage.
- Allow destruction of Options upon losing the owner.
- Fix the flamethrower effect of Options to sync in multiplayer.
- Add a config for sync time to allow Options to behave properly in Multiplayer at the cost of delay through the queueing system.

**1.0.1**
- Fix some random exceptions found in mines related to animations.
- Add lore and better description for Gradius' Option.
- Allow Instant Minefield mines to only explode when landing.

**1.0.0**
- Implement Gradius' Option!
- Update mod icon to highlight the new item.

**0.2.2**
- Fix buff icon of Dead Man's Foot.
- Fix the exceptions being raised on Dead Man's Foot detonation.

**0.2.1**
- Add Beating Embryo support for Instant Minefield.

**0.2.0**
- Implement Instant Minefield!
- It's filled with mines nowadays.

**0.1.0**
- Implement Dead Man's Foot!
- Improve and clean code.

**0.0.4**
- Fix the items' icons added by ChensClassicItems because they display as white in the Command Menu.

**0.0.3**
- Removed DEBUG mode. Woops. My bad.

**0.0.2**
- Implement Panic Mines!
- Fix grammar errors found on AtG Missile Mk. 2.
- Attach the mod to the original ClassicItems *even closer*.

**0.0.1**
- Initial version. Adds the AtG Missile Mk. 2 item. Configurable.