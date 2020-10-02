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
        [AutoItemConfig("Set to true for Options/Multiples of Flame Drones to generate a flamethrower sound. WARNING: Turning this on may cause earrape.", AutoItemConfigFlags.None)]
        public bool flamethrowerSoundCopy { get; private set; } = false;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Set to true for Options/Multiples of Gatling Turrets to generate a firing sound. WARNING: Turning this on may cause earrape.", AutoItemConfigFlags.None)]
        public bool gatlingSoundCopy { get; private set; } = false;

        public override bool itemAIB { get; protected set; } = true;

        protected override string NewLangName(string langid = null) => displayName;

        protected override string NewLangPickup(string langid = null) => $"Deploy the Option, an ultimate weapon from the Gradius Federation, for each owned Drone.";

        protected override string NewLangDesc(string langid = null)
        {
            return $"Deploy <style=cIsDamage>1</style> <style=cStack>(+1 for each stack) Option for <style=cIsDamage>each of your owned drone</style>. " +
                   $"Options will copy all the attacks of the drone for {Pct(damageMultiplier, 0)} of the damage dealt.";
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

        private static List<string> DronesList = new List<string>
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
            "Turret1"
        };

        public GradiusOption()
        {
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
            On.RoR2.MasterSummon.Perform += MasterSummon_Perform;
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
            On.RoR2.MasterSummon.Perform -= MasterSummon_Perform;
        }

        private CharacterMaster MasterSummon_Perform(On.RoR2.MasterSummon.orig_Perform orig, MasterSummon self)
        {
            CharacterMaster result = orig(self);
            if (result && FilterDrones(result.name) && NetworkServer.active)
            {
                CharacterBody minionBody = result.GetBody();
                CharacterBody masterBody = result.minionOwnership.ownerMaster.GetBody();
                if (minionBody && masterBody)
                {
                    int currentCount = GetCount(masterBody);
                    for (int t = 1; t <= currentCount; t++)
                    {
                        SpawnOption(masterBody.gameObject, minionBody.gameObject, t);
                    }
                }
            }
            return result;
        }

        private CharacterBody CharacterMaster_SpawnBody(On.RoR2.CharacterMaster.orig_SpawnBody orig, CharacterMaster self, GameObject bodyPrefab, Vector3 position, Quaternion rotation)
        {
            CharacterBody result = orig(self, bodyPrefab, position, rotation);
            //if (result && FilterDrones(result.name) && self.minionOwnership && self.minionOwnership.ownerMaster)
            //{
            //    int currentCount = GetCount(result);
            //    for (int t = 1; t <= currentCount; t++)
            //    {
            //        SpawnOption(self.minionOwnership.ownerMaster.GetBody().gameObject, self.GetBody().gameObject, t);
            //    }
            //}
            return result;
        }

        private void CharacterBody_OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            if (self)
            {
                int newCount = GetCount(self);
                if (self.master && newCount > 0)
                {
                    GameObject gameObject = self.gameObject;
                    OptionTracker optionTracker = gameObject.GetComponent<OptionTracker>() ?? gameObject.AddComponent<OptionTracker>();
                    int oldCount = optionTracker.optionItemCount;

                    if (newCount - oldCount > 0)
                    {
                        ClassicItemsPlugin._logger.LogDebug("Spawning.");
                        LoopAllMinionOwnerships(self.master, (minion) =>
                        {
                            ClassicItemsPlugin._logger.LogDebug($"Looping... OldCount: {oldCount}, NewCount: {newCount}");
                            for (int t = oldCount + 1; t <= newCount; t++)
                            {
                                SpawnOption(gameObject, minion, t);
                            }
                        });
                    }
                    else if (newCount - oldCount < 0)
                    {
                        ClassicItemsPlugin._logger.LogDebug("Destroying.");
                        LoopAllMinionOwnerships(self.master, (minion) =>
                        {
                            OptionTracker minionOptionTracker = minion.GetComponent<OptionTracker>();
                            if (minionOptionTracker)
                            {
                                for (int t = oldCount; t > newCount; t--)
                                {
                                    DestroyOption(minionOptionTracker, t);
                                }
                            }
                        });
                    }
                }
            }
        }

        private void HealBeam_OnEnter(On.EntityStates.Drone.DroneWeapon.HealBeam.orig_OnEnter orig, HealBeam self)
        {
            orig(self);
            FireForAllMinions(self, (option, target) =>
            {
                float healRate = (HealBeam.healCoefficient * self.damageStat / self.duration) * damageMultiplier;
                Ray aimRay = self.GetAimRay();
                Transform transform = option.transform;
                if (NetworkServer.active)
                {
                    if (transform && self.target)
                    {
                        GameObject gameObject = Object.Instantiate(HealBeam.healBeamPrefab, transform);
                        HealBeamController hbc = option.GetComponent<OptionBehavior>().healBeamController = gameObject.GetComponent<HealBeamController>();
                        hbc.healRate = healRate;
                        hbc.target = self.target;
                        hbc.ownership.ownerObject = option.gameObject;
                        NetworkServer.Spawn(gameObject);
                    }
                }
            });
        }

        private void HealBeam_OnExit(On.EntityStates.Drone.DroneWeapon.HealBeam.orig_OnExit orig, HealBeam self)
        {
            orig(self);
            FireForAllMinions(self, (option, target) =>
            {
                OptionBehavior behavior = option.GetComponent<OptionBehavior>();
                if (behavior && behavior.healBeamController)
                {
                    behavior.healBeamController.BreakServer();
                }
            });
        }

        private void StartHealBeam_OnEnter(On.EntityStates.Drone.DroneWeapon.StartHealBeam.orig_OnEnter orig, StartHealBeam self)
        {
            orig(self);
            FireForAllMinions(self, (option, target) =>
            {
                if (NetworkServer.active)
                {
                    if (HealBeamController.GetHealBeamCountForOwner(self.gameObject) >= self.maxSimultaneousBeams)
                    {
                        return;
                    }
                    if (self.targetHurtBox)
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
                }
            });
        }

        private void Flamethrower_OnExit(On.EntityStates.Mage.Weapon.Flamethrower.orig_OnExit orig, MageWeapon.Flamethrower self)
        {
            orig(self);
            if (self.characterBody.name.Contains("FlameDrone") && self.characterBody.master.name.Contains("FlameDrone"))
            {
                FireForAllMinions(self, (option, target) =>
                {
                    if (flamethrowerSoundCopy) Util.PlaySound(MageWeapon.Flamethrower.endAttackSoundString, option);
                    OptionBehavior behavior = option.GetComponent<OptionBehavior>();
                    if (behavior && behavior.flamethrower)
                    {
                        EntityState.Destroy(behavior.flamethrower);
                    }
                });
            }
        }

        private void Flamethrower_FixedUpdate(On.EntityStates.Mage.Weapon.Flamethrower.orig_FixedUpdate orig, MageWeapon.Flamethrower self)
        {
            bool oldBegunFlamethrower = self.hasBegunFlamethrower;
            orig(self);
            if (self.characterBody.name.Contains("FlameDrone") && self.characterBody.master.name.Contains("FlameDrone"))
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
                        }
                    }
                    if (perMinionOldBegunFlamethrower)
                    {
                        behavior.flamethrower.transform.forward = direction;
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
                    RaycastHit raycastHit;
                    if (Physics.Raycast(position, forward, out raycastHit, maxDistance, LayerIndex.world.mask | LayerIndex.entityPrecise.mask))
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
            ClassicItemsPlugin._logger.LogDebug("Starting LoopMinionOwnerships.");
            MinionOwnership[] minionOwnerships = Object.FindObjectsOfType<MinionOwnership>();
            ClassicItemsPlugin._logger.LogDebug("Looping minionOwnerships...");
            foreach (MinionOwnership minionOwnership in minionOwnerships)
            {
                if (minionOwnership && minionOwnership.ownerMaster)
                {
                    ClassicItemsPlugin._logger.LogDebug("Checking if minion is owned by a specific player...");
                    if (minionOwnership.ownerMaster == ownerMaster)
                    {
                        ClassicItemsPlugin._logger.LogDebug("This minion is owned by this specified player.");
                        CharacterMaster minionMaster = minionOwnership.GetComponent<CharacterMaster>();
                        if (minionMaster && FilterDrones(minionMaster.name))
                        {
                            CharacterBody minionBody = minionMaster.GetBody();
                            if (minionBody)
                            {
                                GameObject minion = minionBody.gameObject;
                                actionToRun(minion);
                            }
                            else ClassicItemsPlugin._logger.LogDebug("Minion has no body. Skipping.");
                        }
                        else ClassicItemsPlugin._logger.LogDebug("Minion has no CharacterMaster component. Skipping.");
                    }
                    else ClassicItemsPlugin._logger.LogDebug("Different entity owns this minion. Skip.");
                }
                else ClassicItemsPlugin._logger.LogDebug("minionOwnership or minionOwnership.ownerMaster is null.");
            }
        }

        private void FireForAllMinions(BaseState self, Action<GameObject, GameObject> actionToRun)
        {
            OptionTracker optionTracker = self.characterBody.GetComponent<OptionTracker>();
            if (optionTracker)
            {
                GameObject target = self.characterBody.master.gameObject.GetComponent<BaseAI>().currentEnemy.gameObject;
                if (target)
                {
                    foreach (GameObject option in optionTracker.existingOptions)
                    {
                        actionToRun(option, target);
                    }
                }
            }
        }

        private void SpawnOption(GameObject master, GameObject owner, int itemCount)
        {
            OptionTracker masterOptionTracker = master.GetComponent<OptionTracker>() ?? master.AddComponent<OptionTracker>();
            OptionTracker ownerOptionTracker = owner.GetComponent<OptionTracker>() ?? owner.AddComponent<OptionTracker>();
            masterOptionTracker.optionItemCount = ownerOptionTracker.optionItemCount = itemCount;
            GameObject option = Object.Instantiate(ClassicItemsPlugin.gradiusOptionPrefab, owner.transform.position, owner.transform.rotation);
            OptionBehavior behavior = option.GetComponent<OptionBehavior>();
            behavior.owner = owner;
            behavior.master = master;
            behavior.numbering = ownerOptionTracker.optionItemCount;
            ownerOptionTracker.existingOptions.Add(option);
            NetworkServer.Spawn(option);
        }

        private void DestroyOption(OptionTracker optionTracker, int optionNumber)
        {
            int index = optionTracker.optionItemCount = optionNumber - 1;
            GameObject option = optionTracker.existingOptions[index];
            NetworkServer.Destroy(option);
            optionTracker.existingOptions.RemoveAt(index);
            Object.Destroy(option);
        }

        private bool FilterDrones(string name) => DronesList.Exists((item) => name.Contains(item));
    }

    public class OptionBehavior : MonoBehaviour
    {
        public GameObject owner;
        public GameObject master;
        public int numbering = 0;
        public GameObject flamethrower;
        public HealBeamController healBeamController;

        private Transform t;
        private OptionTracker ot;
        private bool init = true;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void Awake()
        {
            t = gameObject.transform;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void Update()
        {
            if (!init)
            {
                if (owner && ot)
                {
                    t.position = ot.flightPath[numbering * ot.distanceInterval - 1];
                    gameObject.transform.rotation = owner.transform.rotation;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void FixedUpdate()
        {
            if (init && owner && master)
            {
                init = false;
                ot = owner.GetComponent<OptionTracker>();
            }
        }
    }

    public class OptionTracker : MonoBehaviour
    {
        public List<Vector3> flightPath { get; private set; } = new List<Vector3>();
        public List<GameObject> existingOptions { get; private set; } = new List<GameObject>();
        public int distanceInterval { get; private set; } = 20;
        public int optionItemCount = 0;

        private Vector3 previousPosition = new Vector3();
        private bool init = true;
        private int previousOptionItemCount = 0;

        private Transform t;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void Awake()
        {
            t = gameObject.transform;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void Update()
        {
            if (!init)
            {
                if (previousPosition != t.position)
                {
                    flightPath.Insert(0, t.position);
                    if (flightPath.Count > optionItemCount * distanceInterval)
                    {
                        flightPath.RemoveAt(flightPath.Count - 1);
                    }
                }
                previousPosition = t.position;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void FixedUpdate()
        {
            if (init && optionItemCount > 0)
            {
                init = false;
                previousPosition = t.position;
                ManageFlightPath(1);
            }
            else if (!init && optionItemCount > 0)
            {
                int diff = optionItemCount - previousOptionItemCount;
                if (diff > 0 || diff < 0)
                {
                    previousOptionItemCount = optionItemCount;
                    ManageFlightPath(diff);
                }
            }
            else if (!init && optionItemCount <= 0)
            {
                init = true;
                flightPath.Clear();
                previousOptionItemCount = 0;
            }
        }

        private void ManageFlightPath(int difference)
        {
            if (difference > 0)
            {
                int flightPathCap = optionItemCount * distanceInterval;
                while (flightPath.Count < flightPathCap)
                {
                    flightPath.Add(previousPosition);
                }
            }
            else if (difference < 0)
            {
                int flightPathCap = optionItemCount * distanceInterval;
                while (flightPath.Count >= flightPathCap)
                {
                    flightPath.RemoveAt(flightPath.Count - 1);
                }
            }
        }
    }

    public class Flicker : MonoBehaviour
    {
        // Child Objects in Order:
        // 0. sphere1: Light
        // 1. sphere2: Light
        // 2. sphere3: Light
        // 3. sphere4: MeshRenderer, MeshFilter

        private readonly float baseValue = 1f;
        private readonly float amplitude = .25f;
        private readonly float phase = 0f;
        private readonly float frequency = 1f;

        private readonly Light[] lightObjects = new Light[3];
        private readonly float[] originalRange = new float[3];
        private readonly float[] ampMultiplier = new float[4] { 1.2f, 1f, .8f, .4f };
        private Vector3 originalLocalScale;
        private GameObject meshObject;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void Awake()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject child = transform.GetChild(i).gameObject;
                Light childLight = child.GetComponent<Light>();
                switch (child.name)
                {
                    case "sphere1":
                        originalRange[0] = childLight.range;
                        lightObjects[0] = childLight;
                        break;

                    case "sphere2":
                        originalRange[1] = childLight.range;
                        lightObjects[1] = childLight;
                        break;

                    case "sphere3":
                        originalRange[2] = childLight.range;
                        lightObjects[2] = childLight;
                        break;

                    case "sphere4":
                        originalLocalScale = child.transform.localScale;
                        meshObject = child;
                        break;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void Update()
        {
            for (int i = 0; i < lightObjects.Length; i++)
            {
                lightObjects[i].range = originalRange[i] * Wave(ampMultiplier[i]);
            }
            meshObject.transform.localScale = originalLocalScale * Wave(ampMultiplier[3]);
        }

        private float Wave(float ampMultiplier)
        {
            float x = (Time.time + phase) * frequency;
            x -= Mathf.Floor(x);
            float y = Mathf.Sin(x * 2 * Mathf.PI);

            return (y * amplitude * ampMultiplier) + baseValue;
        }
    }
}