# Chen's Classic Items

## Description

An extension mod for **ThinkInvis.ClassicItems**!

This mod also adds items (and artifacts) from Risk of Rain 1 that did not make it to Risk of Rain 2.

For this mod's API, the documentation can be found in the **[wiki](https://github.com/cheeeeeeeeeen/RoR2-ClassicItems/wiki/xeu4S0yNjk4zZlj9T72lMw)**.

## Installation

Use **[r2modman](https://thunderstore.io/package/ebkr/r2modman/)** mod manager to install this mod.

If one does not want to use a mod manager, then get the DLL from **[Thunderstore](https://thunderstore.io/package/Chen/ChensClassicItems/)**.

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

*For the full changelog, check this [wiki page](https://github.com/cheeeeeeeeeen/RoR2-ClassicItems/wiki/Changelog)*.