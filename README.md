# Chen's Classic Items

## Description

An extension mod for **ThinkInvis.ClassicItems**!

This mod also adds items (and artifacts) from Risk of Rain 1 that did not make it to Risk of Rain 2.

For this mod's API, the documentation can be found in the [wiki](https://github.com/cheeeeeeeeeen/RoR2-ClassicItems/wiki/xeu4S0yNjk4zZlj9T72lMw) or at [GitHub Pages](https://cheeeeeeeeeen.github.io/RoR2-ClassicItems/).

## Installation

Use **r2modman** mod manager to install this mod.

## Current Additions
### Artifact
- **Distortion** : Lock a random skill per interval. Both active and passive skills can be locked.
- **Origin** : Summon the Imp Army to destroy you. Imp Overlords will drop a Pearl or Irradiant Pearl once defeated.
- **Spirit** : Characters (both players and enemies) run faster at lower health.
### Equipment
- **Drone Repair Kit** : Heals all drones you own as well as applying a regeneration buff.
- **Instant Minefield** : Drop mines that are quickly fully armed upon landing.
### Tier 1
- **Mortar Tube** : Launch a mortar. Classic.
### Tier 2
- **Arms Race** : All drones you own will launch a missile or a mortar, or both.
- **Dead Man's Foot** : Throw a mine upon taking *serious damage* that inflicts poison upon exploding.
- **Panic Mines** : Throw a mine upon taking *serious damage* that inflicts damage. 
### Tier 3
- **AtG Missile Mk. 2** : AtG Missile Mk. 1 but better.
- **Thallium** : Chance to inflict poison on enemies based on their own damage.

## Contact
- Issue Page: https://github.com/cheeeeeeeeeen/RoR2-ClassicItems/issues
- Email: `blancfaye7@gmail.com`
- Discord: `Chen#1218`
- RoR2 Modding Server: https://discord.com/invite/5MbXZvd
- Give a tip through Ko-fi: https://ko-fi.com/cheeeeeeeeeen

## More Information

Check out the original ClassicItems made by **ThinkInvisible**.
- Thunderstore: https://thunderstore.io/package/ThinkInvis/ClassicItems/
- GitHub: https://github.com/ThinkInvis/RoR2-ClassicItems

**Kirbsuke#0352** made the Artifact of Spirit icon.
- Contact: `kirbydamaster@gmail.com` or through Discord.

**manaboi#4887** made the Artifact of Origin icon. Artifact of Distortion icon's basis was from them and was improvised.
- Contact: Through Discord.

**Aromatic Sunday#2929** did offer to make icons, and thus included as an alternate version.

## Changelog

**2.3.1**
- Add a config option about the Imp Vanguard's Spawn position. It now defaults to spawn like the Imp Soldiers.
- Refactor the Spawn Queue of the Imps spawned by the Artifact of Origin.
- Integrate newer helpers from ChensHelpers.

**2.3.0**
- Implement API for Drone Repair Kit to support custom drones.
- Allow Drone Repair Kit to use TILER2 helpers instead of hooking in RecalculateStats.
- Recompile for some breaking changes and a fatal bug found in ChensHelpers.
- Fully document the mod for interested mod creators.
- Fix a bug in Distortion about not changing the skills in seconds but in hours.

**2.2.13**
- Integrate ChensHelpers mod.
- There should be no notable change except for better documentation and cleaner code. Potential undiscovered bugs could be fixed.

**2.2.12**
- Fix a huge and nasty bug found in the Artifact of Origin when played in multiplayer.

**2.2.11**
- Fix a "fatal" bug that might make it hard to report bugs due to logging not working.
- Generate documentation for API. Wiki created for this as well.

**2.2.10**
- Minor update. Fix documentation of the publicized method.
- Fix grammar errors found in Arms race description.
- Also fix an error in the manifest.

**2.2.9**
- Fix a bug regarding compatibility code with EnemyItemDisplays if it is not installed.
- Publicize the TriggerArtillery method in Arms Race so that other mods may call it for their custom drones.
- Add a check for Arms Race regarding Proc Chain Masks to avoid infinite procs.

**2.2.8**
- Register custom cloned projectiles to avoid errors.
- Fix a possible bug with Arms Race where in the mortar from drones will not launch if Mortar Tube is disabled in config.

**2.2.7**
- Another bug fixes regarding Artifacts of Distortion and Origin.
- Distortion skills will lock skills again after being replaced by another skill. Example is Engineer's Thermal Harpoons replacing the 1st and 2nd skill upon use of the 3rd skill.
- Actually use real time in seconds as a timer instead of an integer being incremented by 1.
- Manually generate item sets for Origin instead of it being dependent on Evolution's manager.
- Add listing of items that can be given to enemies in both Evolution and Origin for an easier time to debug when a bug happens regarding banning.
- Actually fix the items given to Imps. Properly give the items from the correct tiers.

**2.2.6**
- Small bug fix regarding the randomness of Imp Soldiers dropping Pearls, for some reason.
- Changed the internal mechanics on how the Pearls drop.
- Only one Imp Overlord per player will drop the Pearl if Artifact of Swarm is enabled.
- Dio's Best Friend revival will no longer duplicate the drop.

**2.2.5**
- Add soft dependency for EnemyItemDisplays so that Origin Imps will also be supported.
- Update the icon for Artifact of Distortion into an official one.

**2.2.4**
- Big optimization in Artifact of Origin.
- Allow Imp Vanguards to be their own monster, a clone of Imp Overlord.
- Add a config to spawn Imps triggered by Origin in intervals. Configurable. This is to avoid frame drops.
- Properly add a name and avatar icons for these Imps.
- Improve Artifact of Spirit by making enemies apply their movement speed changes instantly upon health change.
- Allow movement speed changed from Artifact of Spirit be limited through a threshold to avoid frame drops.
- Update the icon of Artifact of Origin into the official one.

**2.2.3**
- Recolor the Imps spawned from Artifact of Origin.
- Use Xoroshiro128 for randomizing skills in Distortion.
- Spectators will no longer be shown the wrong Distortion Skill for the player they are spectating.

**2.2.2**
- Allow Imp Overlords spawned from Artifact of origin to drop a Pearl or Irradiant Pearl.
- Fix the Imps from Origin of a bug that apparently gives them an Equipment.
- Fix Dead Man's Foot explosion by actually adding an explosion effect.
- Add more config options for Artifact of Origin.
- Add a config for Panic Mines to let it self-destruct when the owner is gone.
- Reduced the default values for the Origin config as the imps are too powerful.
- Update dependency to use the latest ClassicItems.

**2.2.1**
- Fix a bug with the Aritfact of Origin failing to parse bad items.

**2.2.0**
- Implement Artifact of Origin!
- Temporary icons for the new artifact.
- Vastly improve the code for Distortion mechanics.
- Change usage of TILER2 Helpers so that the cards will have proper text.

**2.1.0**
- Implement Artifact of Distortion!
- Temporary icons for the new artifact.
- Include alternate icons for Spirit artifact made by other people.

**2.0.1**
- Update the mod for a missing setup that prevents it from working correctly.

**2.0.0**
- Migrate the code to support new changes of TILER2.

**1.4.5**
- Fix bright mines bug. Replace Color with Color32.
- Update Artifact of Spirit icon.

**1.4.4**
- Fix bugs found with the Artifact of Spirit about misbehaving characters, like zooming off the map.
- Let Artifact of Spirit modify acceleration also to mitigate for their own new speed.

**1.4.3**
- Implement Artifact of Spirit!
- Temporary icon for Spirit artifact for now.
- Recolor the mines for an easier time differentiating which ones are which.

**1.4.2**
- Support Squid Polyps for use with Arms Race. Configurable.
- Squid Polyps are turned off by default, so turn it on in config if you want to buff them.

**1.4.1**
- Fix and improve some descriptions.
- Fix the actual version of the mod.
- Improve the code base and optimize some implementations.
- Add a short lore to Drone Repair Kit.

**1.4.0**
- Implement Thallium!
- Add BetterUI Compats for more descriptions regarding the skills for survivors.
- Update references for dependencies.

**1.3.1**
- Remove Gradius' Option. It is now included in a separate mod.

**1.3.0**
- Implement Drone Repair Kit!
- Remove the sync logging as it apparently caused heavy lag.
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
- Implement a queuing system for syncing to lessen desync and lessen bandwidth usage.
- Improve the code by letting linear behavior into client sided execution to lessen bandwidth usage.
- Allow destruction of Options upon losing the owner.
- Fix the flamethrower effect of Options to sync in multiplayer.
- Add a config for sync time to allow Options to behave properly in Multiplayer at the cost of delay through the queuing system.

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
