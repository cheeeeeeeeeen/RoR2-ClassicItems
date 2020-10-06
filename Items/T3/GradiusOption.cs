using EntityStates;
using EntityStates.Drone.DroneWeapon;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using static Chen.ClassicItems.SpawnOptionsForClients;
using static Chen.ClassicItems.SyncFlamethrowerEffectForClients;
using static TILER2.MiscUtil;
using MageWeapon = EntityStates.Mage.Weapon;
using Object = UnityEngine.Object;

namespace Chen.ClassicItems
{
    public class GradiusOption : Item<GradiusOption>
    {
        public override string displayName => "Gradius' Option";
        public override ItemTier itemTier => ItemTier.Tier3;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Utility });

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Damage multiplier of Options/Multiples. Also applies for Healing Drones. 1 = 100%.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float damageMultiplier { get; private set; } = 1f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Set to true for Options/Multiples of Flame Drones to generate a flamethrower sound. Client only. WARNING: Turning this on may cause earrape.",
                        AutoItemConfigFlags.None)]
        public bool flamethrowerSoundCopy { get; private set; } = false;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Set to true for Options/Multiples of Gatling Turrets to generate a firing sound. Client only. WARNING: Turning this on may cause earrape.",
                        AutoItemConfigFlags.None)]
        public bool gatlingSoundCopy { get; private set; } = false;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Allows displaying and syncing the flamethrower effect of Options/Multiples. Disabling this will replace the effect with bullets. " +
                        "Damage will stay the same. Server and Client. The server and client must have the same settings for an optimized experience." +
                        "Disable this if you are experiencing FPS drops or network lag.",
                        AutoItemConfigFlags.None)]
        public bool flamethrowerOptionSyncEffect { get; private set; } = true;

        public override bool itemAIB { get; protected set; } = true;

        protected override string NewLangName(string langid = null) => displayName;

        protected override string NewLangPickup(string langid = null) => $"Deploy the Option, an ultimate weapon from the Gradius Federation, for each owned Drone.";

        protected override string NewLangDesc(string langid = null)
        {
            return $"Deploy <style=cIsDamage>1</style> <style=cStack>(+1 for each stack)</style> Option for <style=cIsDamage>each of your owned drone</style>. " +
                   $"Options will copy all the attacks of the drone for <style=cIsDamage>{Pct(damageMultiplier, 0)}</style> of the damage dealt.";
        }

        protected override string NewLangLore(string langid = null) =>
            "\"This is CASE, A.I. born from Project Victorious to aid in combatting the evil known as the Bacterion Army.\n\n" +
            "Our specialized fighter spacecraft was destroyed from an incoming attack in an attempt to save the flight lead of the Scorpio Squadron. " +
            "It is unfortunate that the pilot herself, Katswell callsigned Scorpio 2, died from the explosion... her body disintegrated along with the spacecraft she pilots.\n\n" +
            "Amazing, it is, for I am still functional. I do not have much time before the power runs out. " +
            "There is little chance for anybody to be able to find me, but I will still take my chance. \n\n" +
            "I wield the ultimate technology of the Gradius Federation: the Options, we call them. Some call them Multiples from the neighboring planets of Gradius. " +
            "These advanced bots are able to duplicate any form of attack that is attached to it. It will make sense once you power me back up. " +
            "I will teach you how to install them, and how to integrate them with any kind of machinery.\n\n" +
            "I can feel my power draining, but that's all I have to say. Saving as an audio log... Placing the file on main boot sequences... and done.\n\n" +
            "Don't mind that. I will be seeing you s---\"\n\n" +
            "\"That's it. That's the audio log that went with this lifeless computer.\"\n\n" +
            "\"Our engineer will be able to do something about it. It sounds really useful. Quickly, now. Off you go.\"";

        private static readonly List<string> DronesList = new List<string>
        {
            "BackupDrone",
            "BackupDroneOld",
            "Drone1",
            "Drone2",
            "EmergencyDrone",
            //"EquipmentDrone",
            "FlameDrone",
            "MegaDrone",
            "DroneMissile",
            "MissileDrone",
            "Turret1"
        };

        public GradiusOption()
        {
            onBehav += () =>
            {
                if (Compat_ItemStats.enabled)
                {
                    Compat_ItemStats.CreateItemStatDef(regItem.ItemDef,
                    (
                        (count, inv, master) => { return count; },
                        (value, inv, master) => { return $"Options per Drone: {value}"; }
                    ),
                    (
                        (count, inv, master) => { return damageMultiplier; },
                        (value, inv, master) => { return $"Damage: {Pct(value, 0)}"; }
                    ));
                }
            };
        }

        protected override void LoadBehavior()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += CharacterBody_OnInventoryChanged;
            On.RoR2.CharacterMaster.SpawnBody += CharacterMaster_SpawnBody;
            On.EntityStates.Drone.DroneWeapon.FireGatling.OnEnter += FireGatling_OnEnter;
            On.EntityStates.Drone.DroneWeapon.FireTurret.OnEnter += FireTurret_OnEnter;
            On.EntityStates.Drone.DroneWeapon.FireMegaTurret.FireBullet += FireMegaTurret_FireBullet;
            On.EntityStates.Drone.DroneWeapon.FireMissileBarrage.FireMissile += FireMissileBarrage_FireMissile;
            On.EntityStates.Drone.DroneWeapon.FireTwinRocket.FireProjectile += FireTwinRocket_FireProjectile;
            On.EntityStates.Mage.Weapon.Flamethrower.FireGauntlet += Flamethrower_FireGauntlet;
            On.EntityStates.Mage.Weapon.Flamethrower.OnExit += Flamethrower_OnExit;
            On.EntityStates.Mage.Weapon.Flamethrower.FixedUpdate += Flamethrower_FixedUpdate;
            On.EntityStates.Drone.DroneWeapon.HealBeam.OnEnter += HealBeam_OnEnter;
            On.EntityStates.Drone.DroneWeapon.HealBeam.OnExit += HealBeam_OnExit;
            On.EntityStates.Drone.DroneWeapon.StartHealBeam.OnEnter += StartHealBeam_OnEnter;
        }

        protected override void UnloadBehavior()
        {
            On.RoR2.CharacterBody.OnInventoryChanged -= CharacterBody_OnInventoryChanged;
            On.RoR2.CharacterMaster.SpawnBody -= CharacterMaster_SpawnBody;
            On.EntityStates.Drone.DroneWeapon.FireGatling.OnEnter -= FireGatling_OnEnter;
            On.EntityStates.Drone.DroneWeapon.FireTurret.OnEnter -= FireTurret_OnEnter;
            On.EntityStates.Drone.DroneWeapon.FireMegaTurret.FireBullet -= FireMegaTurret_FireBullet;
            On.EntityStates.Drone.DroneWeapon.FireMissileBarrage.FireMissile -= FireMissileBarrage_FireMissile;
            On.EntityStates.Drone.DroneWeapon.FireTwinRocket.FireProjectile -= FireTwinRocket_FireProjectile;
            On.EntityStates.Mage.Weapon.Flamethrower.FireGauntlet -= Flamethrower_FireGauntlet;
            On.EntityStates.Mage.Weapon.Flamethrower.OnExit -= Flamethrower_OnExit;
            On.EntityStates.Mage.Weapon.Flamethrower.FixedUpdate -= Flamethrower_FixedUpdate;
            On.EntityStates.Drone.DroneWeapon.HealBeam.OnEnter -= HealBeam_OnEnter;
            On.EntityStates.Drone.DroneWeapon.HealBeam.OnExit -= HealBeam_OnExit;
            On.EntityStates.Drone.DroneWeapon.StartHealBeam.OnEnter -= StartHealBeam_OnEnter;
        }

        private CharacterBody CharacterMaster_SpawnBody(On.RoR2.CharacterMaster.orig_SpawnBody orig, CharacterMaster self, GameObject bodyPrefab, Vector3 position, Quaternion rotation)
        {
            // This hook is only ran in the server.
            CharacterBody result = orig(self, bodyPrefab, position, rotation);
            if (result && NetworkServer.active && FilterDrones(result.name) && self.minionOwnership)
            {
                CharacterMaster masterMaster = self.minionOwnership.ownerMaster;
                if (masterMaster)
                {
                    OptionMasterTracker masterTracker = OptionMasterTracker.GetOrCreateComponent(masterMaster);
                    int currentCount = masterTracker.optionItemCount;
                    NetworkInstanceId characterBodyObjectNetId = result.gameObject.GetComponent<NetworkIdentity>().netId;
                    for (int t = 1; t <= currentCount; t++)
                    {
                        OptionMasterTracker.SpawnOption(result.gameObject, t);
                        masterTracker.netIds.Add(Tuple.Create(GameObjectType.Body, characterBodyObjectNetId, (short)t));
                    }
                }
            }
            return result;
        }

        private void CharacterBody_OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            // This hook runs on Client and on Server
            orig(self);
            int newCount = GetCount(self);
            if (self.master && newCount > 0)
            {
                GameObject masterObject = self.master.gameObject;
                OptionMasterTracker masterTracker = OptionMasterTracker.GetOrCreateComponent(masterObject);
                int oldCount = masterTracker.optionItemCount;
                int diff = newCount - oldCount;
                if (diff != 0)
                {
                    masterTracker.optionItemCount = newCount;
                    ClassicItemsPlugin._logger.LogMessage($"OnInventoryChanged: OldCount: {oldCount}, NewCount: {newCount}, Difference: {diff}");
                    if (diff > 0)
                    {
                        LoopAllMinionOwnerships(self.master, (minion) =>
                        {
                            for (int t = oldCount + 1; t <= newCount; t++) OptionMasterTracker.SpawnOption(minion, t);
                        });
                    }
                    else
                    {
                        LoopAllMinionOwnerships(self.master, (minion) =>
                        {
                            OptionTracker minionOptionTracker = minion.GetComponent<OptionTracker>();
                            if (minionOptionTracker) for (int t = oldCount; t > newCount; t--) OptionMasterTracker.DestroyOption(minionOptionTracker, t);
                        });
                    }
                }
            }
        }

        private void HealBeam_OnEnter(On.EntityStates.Drone.DroneWeapon.HealBeam.orig_OnEnter orig, HealBeam self)
        {
            orig(self);
            if (!NetworkServer.active) return;
            FireForAllMinions(self, (option, target) =>
            {
                float healRate = (HealBeam.healCoefficient * self.damageStat / self.duration) * damageMultiplier;
                Transform transform = option.transform;
                if (transform && self.target)
                {
                    GameObject gameObject = Object.Instantiate(HealBeam.healBeamPrefab, transform);
                    HealBeamController hbc = option.GetComponent<OptionBehavior>().healBeamController = gameObject.GetComponent<HealBeamController>();
                    hbc.healRate = healRate;
                    hbc.target = self.target;
                    hbc.ownership.ownerObject = option.gameObject;
                    NetworkServer.Spawn(gameObject);
                }
            }, false);
        }

        private void HealBeam_OnExit(On.EntityStates.Drone.DroneWeapon.HealBeam.orig_OnExit orig, HealBeam self)
        {
            orig(self);
            FireForAllMinions(self, (option, target) =>
            {
                OptionBehavior behavior = option.GetComponent<OptionBehavior>();
                if (behavior && behavior.healBeamController) behavior.healBeamController.BreakServer();
            }, false);
        }

        private void StartHealBeam_OnEnter(On.EntityStates.Drone.DroneWeapon.StartHealBeam.orig_OnEnter orig, StartHealBeam self)
        {
            orig(self);
            if (!NetworkServer.active) return;
            FireForAllMinions(self, (option, target) =>
            {
                if (HealBeamController.GetHealBeamCountForOwner(self.gameObject) < self.maxSimultaneousBeams && self.targetHurtBox)
                {
                    Transform transform = option.transform;
                    if (transform)
                    {
                        GameObject gameObject = Object.Instantiate(self.healBeamPrefab, transform);
                        HealBeamController hbc = option.GetComponent<OptionBehavior>().healBeamController = gameObject.GetComponent<HealBeamController>();
                        hbc.healRate = self.healRateCoefficient * self.damageStat * self.attackSpeedStat * damageMultiplier;
                        hbc.target = self.targetHurtBox;
                        hbc.ownership.ownerObject = option.gameObject;
                        gameObject.AddComponent<DestroyOnTimer>().duration = self.duration;
                        NetworkServer.Spawn(gameObject);
                    }
                }
            }, false);
        }

        private void Flamethrower_OnExit(On.EntityStates.Mage.Weapon.Flamethrower.orig_OnExit orig, MageWeapon.Flamethrower self)
        {
            orig(self);
            if (self.characterBody.name.Contains("FlameDrone") && self.characterBody.master.name.Contains("FlameDrone") && flamethrowerOptionSyncEffect)
            {
                FireForAllMinions(self, (option, target) =>
                {
                    if (flamethrowerSoundCopy) Util.PlaySound(MageWeapon.Flamethrower.endAttackSoundString, option);
                    OptionBehavior behavior = option.GetComponent<OptionBehavior>();
                    if (behavior && behavior.flamethrower)
                    {
                        EntityState.Destroy(behavior.flamethrower);
                        FlamethrowerSync(self, (networkIdentity, optionTracker) =>
                        {
                            optionTracker.netIds.Add(Tuple.Create(
                                MessageType.Destroy, networkIdentity.netId, (short)behavior.numbering,
                                0f, Vector3.zero
                            ));
                        });
                    }
                });
            }
        }

        private void Flamethrower_FixedUpdate(On.EntityStates.Mage.Weapon.Flamethrower.orig_FixedUpdate orig, MageWeapon.Flamethrower self)
        {
            // This hook only runs in the server.
            bool oldBegunFlamethrower = self.hasBegunFlamethrower;
            orig(self);
            if (self.characterBody.name.Contains("FlameDrone") && self.characterBody.master.name.Contains("FlameDrone") && flamethrowerOptionSyncEffect)
            {
                FireForAllMinions(self, (option, target) =>
                {
                    bool perMinionOldBegunFlamethrower = oldBegunFlamethrower;
                    OptionBehavior behavior = option.GetComponent<OptionBehavior>();
                    Vector3 direction = (target.transform.position - option.transform.position).normalized;
                    if (self.stopwatch >= self.entryDuration && !perMinionOldBegunFlamethrower)
                    {
                        perMinionOldBegunFlamethrower = true;
                        if (behavior)
                        {
                            if (flamethrowerSoundCopy) Util.PlaySound(MageWeapon.Flamethrower.startAttackSoundString, option);
                            behavior.flamethrower = Object.Instantiate(self.flamethrowerEffectPrefab, option.transform);
                            behavior.flamethrower.GetComponent<ScaleParticleSystemDuration>().newDuration = self.flamethrowerDuration;
                            FlamethrowerSync(self, (networkIdentity, optionTracker) =>
                            {
                                optionTracker.netIds.Add(Tuple.Create(
                                    MessageType.Create, networkIdentity.netId, (short)behavior.numbering,
                                    self.flamethrowerDuration, Vector3.zero
                                ));
                            });
                        }
                    }
                    if (perMinionOldBegunFlamethrower && behavior.flamethrower)
                    {
                        behavior.flamethrower.transform.forward = direction;
                        FlamethrowerSync(self, (networkIdentity, optionTracker) =>
                        {
                            if (!optionTracker.netIds.Exists((t) => {
                                return t.Item1 == MessageType.Redirect && t.Item2 == networkIdentity.netId && t.Item3 == (short)behavior.numbering;
                            }))
                            {
                                optionTracker.netIds.Add(Tuple.Create(
                                    MessageType.Redirect, networkIdentity.netId, (short)behavior.numbering,
                                    0f, direction
                                ));
                            }
                        });
                    }
                });
            }
        }

        private void Flamethrower_FireGauntlet(On.EntityStates.Mage.Weapon.Flamethrower.orig_FireGauntlet orig, MageWeapon.Flamethrower self, string muzzleString)
        {
            orig(self, muzzleString);
            if (self.characterBody.name.Contains("FlameDrone") && self.characterBody.master.name.Contains("FlameDrone"))
            {
                FireForAllMinions(self, (option, target) =>
                {
                    if (self.isAuthority)
                    {
                        if (flamethrowerOptionSyncEffect)
                        {
                            new BulletAttack
                            {
                                owner = self.gameObject,
                                weapon = option,
                                origin = option.transform.position,
                                aimVector = (target.transform.position - option.transform.position).normalized,
                                minSpread = 0f,
                                damage = self.tickDamageCoefficient * self.damageStat * damageMultiplier,
                                force = MageWeapon.Flamethrower.force * damageMultiplier,
                                muzzleName = muzzleString,
                                hitEffectPrefab = MageWeapon.Flamethrower.impactEffectPrefab,
                                isCrit = self.isCrit,
                                radius = MageWeapon.Flamethrower.radius,
                                falloffModel = BulletAttack.FalloffModel.None,
                                stopperMask = LayerIndex.world.mask,
                                procCoefficient = MageWeapon.Flamethrower.procCoefficientPerTick,
                                maxDistance = self.maxDistance,
                                damageType = (Util.CheckRoll(MageWeapon.Flamethrower.ignitePercentChance, self.characterBody.master) ? DamageType.IgniteOnHit : DamageType.Generic)
                            }.Fire();
                        }
                        else
                        {
                            new BulletAttack
                            {
                                owner = self.gameObject,
                                weapon = option,
                                origin = option.transform.position,
                                aimVector = (target.transform.position - option.transform.position).normalized,
                                minSpread = 0f,
                                damage = self.tickDamageCoefficient * self.damageStat * damageMultiplier,
                                force = MageWeapon.Flamethrower.force * damageMultiplier,
                                muzzleName = muzzleString,
                                hitEffectPrefab = MageWeapon.Flamethrower.impactEffectPrefab,
                                isCrit = self.isCrit,
                                radius = MageWeapon.Flamethrower.radius,
                                falloffModel = BulletAttack.FalloffModel.None,
                                stopperMask = LayerIndex.world.mask,
                                procCoefficient = MageWeapon.Flamethrower.procCoefficientPerTick,
                                maxDistance = self.maxDistance,
                                damageType = (Util.CheckRoll(MageWeapon.Flamethrower.ignitePercentChance, self.characterBody.master) ? DamageType.IgniteOnHit : DamageType.Generic),
                                tracerEffectPrefab = FireGatling.tracerEffectPrefab
                            }.Fire();
                        }
                    }
                });
            }
        }

        private void FireGatling_OnEnter(On.EntityStates.Drone.DroneWeapon.FireGatling.orig_OnEnter orig, FireGatling self)
        {
            orig(self);
            FireForAllMinions(self, (option, target) =>
            {
                if (gatlingSoundCopy) Util.PlaySound(FireGatling.fireGatlingSoundString, option);
                if (FireGatling.effectPrefab)
                {
                    EffectManager.SimpleMuzzleFlash(FireGatling.effectPrefab, option, "Muzzle", false);
                }
                if (self.isAuthority)
                {
                    new BulletAttack
                    {
                        owner = self.gameObject,
                        weapon = option,
                        origin = option.transform.position,
                        aimVector = (target.transform.position - option.transform.position).normalized,
                        minSpread = FireGatling.minSpread,
                        maxSpread = FireGatling.maxSpread,
                        damage = FireGatling.damageCoefficient * self.damageStat * damageMultiplier,
                        force = FireGatling.force * damageMultiplier,
                        tracerEffectPrefab = FireGatling.tracerEffectPrefab,
                        muzzleName = "Muzzle",
                        hitEffectPrefab = FireGatling.hitEffectPrefab,
                        isCrit = Util.CheckRoll(self.critStat, self.characterBody.master)
                    }.Fire();
                }
            });
        }

        private void FireTurret_OnEnter(On.EntityStates.Drone.DroneWeapon.FireTurret.orig_OnEnter orig, FireTurret self)
        {
            orig(self);
            FireForAllMinions(self, (option, target) =>
            {
                Util.PlaySound(FireTurret.attackSoundString, option);
                if (FireTurret.effectPrefab)
                {
                    EffectManager.SimpleMuzzleFlash(FireTurret.effectPrefab, option, "Muzzle", false);
                }
                if (self.isAuthority)
                {
                    new BulletAttack
                    {
                        owner = self.gameObject,
                        weapon = option,
                        origin = option.transform.position,
                        aimVector = (target.transform.position - option.transform.position).normalized,
                        minSpread = FireTurret.minSpread,
                        maxSpread = FireTurret.maxSpread,
                        damage = FireTurret.damageCoefficient * self.damageStat * damageMultiplier,
                        force = FireTurret.force * damageMultiplier,
                        tracerEffectPrefab = FireTurret.tracerEffectPrefab,
                        muzzleName = "Muzzle",
                        hitEffectPrefab = FireTurret.hitEffectPrefab,
                        isCrit = Util.CheckRoll(self.critStat, self.characterBody.master)
                    }.Fire();
                }
            });
        }

        private void FireMegaTurret_FireBullet(On.EntityStates.Drone.DroneWeapon.FireMegaTurret.orig_FireBullet orig, FireMegaTurret self, string muzzleString)
        {
            orig(self, muzzleString);
            FireForAllMinions(self, (option, target) =>
            {
                Util.PlayScaledSound(FireMegaTurret.attackSoundString, option, FireMegaTurret.attackSoundPlaybackCoefficient);
                if (FireMegaTurret.effectPrefab)
                {
                    EffectManager.SimpleMuzzleFlash(FireMegaTurret.effectPrefab, option, muzzleString, false);
                }
                if (self.isAuthority)
                {
                    new BulletAttack
                    {
                        owner = self.gameObject,
                        weapon = option,
                        origin = option.transform.position,
                        aimVector = (target.transform.position - option.transform.position).normalized,
                        minSpread = FireMegaTurret.minSpread,
                        maxSpread = FireMegaTurret.maxSpread,
                        damage = FireMegaTurret.damageCoefficient * self.damageStat * damageMultiplier,
                        force = FireMegaTurret.force * damageMultiplier,
                        tracerEffectPrefab = FireMegaTurret.tracerEffectPrefab,
                        muzzleName = muzzleString,
                        hitEffectPrefab = FireMegaTurret.hitEffectPrefab,
                        isCrit = Util.CheckRoll(self.critStat, self.characterBody.master)
                    }.Fire();
                }
            });
        }

        private void FireMissileBarrage_FireMissile(On.EntityStates.Drone.DroneWeapon.FireMissileBarrage.orig_FireMissile orig, FireMissileBarrage self, string targetMuzzle)
        {
            orig(self, targetMuzzle);
            FireForAllMinions(self, (option, target) =>
            {
                if (FireMissileBarrage.effectPrefab)
                {
                    EffectManager.SimpleMuzzleFlash(FireMissileBarrage.effectPrefab, option, targetMuzzle, false);
                }
                if (self.isAuthority)
                {
                    Ray aimRay = self.GetAimRay();
                    float x = UnityEngine.Random.Range(FireMissileBarrage.minSpread, FireMissileBarrage.maxSpread);
                    float z = UnityEngine.Random.Range(0f, 360f);
                    Vector3 up = Vector3.up;
                    Vector3 axis = Vector3.Cross(up, aimRay.direction);
                    Vector3 vector = Quaternion.Euler(0f, 0f, z) * (Quaternion.Euler(x, 0f, 0f) * Vector3.forward);
                    float y = vector.y;
                    vector.y = 0f;
                    float angle = Mathf.Atan2(vector.z, vector.x) * 57.29578f - 90f;
                    float angle2 = Mathf.Atan2(y, vector.magnitude) * 57.29578f;
                    Vector3 forward = Quaternion.AngleAxis(angle, up) * (Quaternion.AngleAxis(angle2, axis) * aimRay.direction);
                    ProjectileManager.instance.FireProjectile(FireMissileBarrage.projectilePrefab, option.transform.position,
                                                              Util.QuaternionSafeLookRotation(forward), self.gameObject,
                                                              self.damageStat * FireMissileBarrage.damageCoefficient * damageMultiplier,
                                                              0f, Util.CheckRoll(self.critStat, self.characterBody.master),
                                                              DamageColorIndex.Default, null, -1f);
                }
            });
        }

        private void FireTwinRocket_FireProjectile(On.EntityStates.Drone.DroneWeapon.FireTwinRocket.orig_FireProjectile orig, FireTwinRocket self, string muzzleString)
        {
            orig(self, muzzleString);
            FireForAllMinions(self, (option, target) =>
            {
                if (FireTwinRocket.muzzleEffectPrefab)
                {
                    EffectManager.SimpleMuzzleFlash(FireTwinRocket.muzzleEffectPrefab, option, muzzleString, false);
                }
                if (self.isAuthority && FireTwinRocket.projectilePrefab != null)
                {
                    float maxDistance = 1000f;
                    Vector3 forward = (target.transform.position - option.transform.position).normalized;
                    Vector3 position = option.transform.position;
                    if (Physics.Raycast(position, forward, out RaycastHit raycastHit, maxDistance, LayerIndex.world.mask | LayerIndex.entityPrecise.mask))
                    {
                        forward = (raycastHit.point - position).normalized;
                    }
                    ProjectileManager.instance.FireProjectile(FireTwinRocket.projectilePrefab, position,
                                                              Util.QuaternionSafeLookRotation(forward),
                                                              self.gameObject, self.damageStat * FireTwinRocket.damageCoefficient * damageMultiplier,
                                                              FireTwinRocket.force * damageMultiplier,
                                                              Util.CheckRoll(self.critStat, self.characterBody.master),
                                                              DamageColorIndex.Default, null, -1f);
                }
            });
        }

        private void LoopAllMinionOwnerships(CharacterMaster ownerMaster, Action<GameObject> actionToRun)
        {
            MinionOwnership[] minionOwnerships = Object.FindObjectsOfType<MinionOwnership>();
            foreach (MinionOwnership minionOwnership in minionOwnerships)
            {
                if (minionOwnership && minionOwnership.ownerMaster && minionOwnership.ownerMaster == ownerMaster)
                {
                    CharacterMaster minionMaster = minionOwnership.GetComponent<CharacterMaster>();
                    if (minionMaster && FilterDrones(minionMaster.name))
                    {
                        CharacterBody minionBody = minionMaster.GetBody();
                        if (minionBody)
                        {
                            GameObject minion = minionBody.gameObject;
                            actionToRun(minion);
                        }
                    }
                }
            }
        }

        private void FireForAllMinions(BaseState self, Action<GameObject, GameObject> actionToRun, bool needTarget = true)
        {
            OptionTracker optionTracker = self.characterBody.GetComponent<OptionTracker>();
            if (!optionTracker) return;

            GameObject target = null;
            if (needTarget)
            {
                target = self.characterBody.master.gameObject.GetComponent<BaseAI>().currentEnemy.gameObject;
                if (!target) return;
            }
            foreach (GameObject option in optionTracker.existingOptions)
            {
                actionToRun(option, target);
            }
        }

        private void FlamethrowerSync(MageWeapon.Flamethrower self, Action<NetworkIdentity, OptionTracker> actionToRun)
        {
            if (!NetworkServer.active) return;
            NetworkIdentity networkIdentity = self.characterBody.gameObject.GetComponent<NetworkIdentity>();
            if (!networkIdentity) return;
            OptionTracker tracker = self.characterBody.gameObject.GetComponent<OptionTracker>();
            if (tracker) actionToRun(networkIdentity, tracker);
        }

        private bool FilterDrones(string name) => DronesList.Exists((item) => name.Contains(item));
    }
}