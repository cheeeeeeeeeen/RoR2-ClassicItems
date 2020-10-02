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
        [AutoItemConfig("Set to true for Options/Multiples of Flame Drones to generate a flamethrower sound. WARNING: Turning this on may cause earrape.", AutoItemConfigFlags.None)]
        public bool flamethrowerSoundCopy { get; private set; } = false;

        protected override string NewLangName(string langid = null) => displayName;

        protected override string NewLangPickup(string langid = null) => $"Deploy the Option, an ultimate weapon from the Gradius Federation, for each owned Drone.";

        protected override string NewLangDesc(string langid = null)
        {
            return NewLangPickup(langid);
        }

        protected override string NewLangLore(string langid = null) => "An item from a different world (ChensClassicItems)";

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
            if (result && FilterDrones(result.name) && self.minionOwnership && self.minionOwnership.ownerMaster)
            {
                int currentCount = GetCount(result);
                for (int t = 1; t <= currentCount; t++)
                {
                    SpawnOption(self.minionOwnership.ownerMaster.GetBody().gameObject, self.GetBody().gameObject, t);
                }
            }
            return result;
        }

        private void CharacterBody_OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            int newCount = GetCount(self);
            if (self.master && newCount > 0)
            {
                GameObject gameObject = self.gameObject;
                OptionTracker optionTracker = gameObject.GetComponent<OptionTracker>() ?? gameObject.AddComponent<OptionTracker>();
                int oldCount = optionTracker.optionItemCount;

                if (newCount - oldCount > 0)
                {
                    for (int t = oldCount + 1; t <= newCount; t++)
                    {
                        SpawnOption(gameObject, gameObject, t);
                    }
                    LoopAllMinionOwnerships(self.master, (minion) =>
                    {
                        for (int t = oldCount + 1; t <= newCount; t++)
                        {
                            SpawnOption(gameObject, minion, t);
                        }
                    });
                }
                else if (newCount - oldCount < 0)
                {
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

        private void HealBeam_OnEnter(On.EntityStates.Drone.DroneWeapon.HealBeam.orig_OnEnter orig, HealBeam self)
        {
            orig(self);
            FireForAllMinions(self, (option, target) =>
            {
                float healRate = HealBeam.healCoefficient * self.damageStat / self.duration;
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
                            hbc.healRate = self.healRateCoefficient * self.damageStat * self.attackSpeedStat;
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
                            damage = self.tickDamageCoefficient * self.damageStat,
                            force = MageWeapon.Flamethrower.force,
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
                Util.PlaySound(FireGatling.fireGatlingSoundString, option);
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
                        damage = FireGatling.damageCoefficient * self.damageStat,
                        force = FireGatling.force,
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
                        damage = FireTurret.damageCoefficient * self.damageStat,
                        force = FireTurret.force,
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
                        damage = FireMegaTurret.damageCoefficient * self.damageStat,
                        force = FireMegaTurret.force,
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
                                                              self.damageStat * FireMissileBarrage.damageCoefficient, 0f,
                                                              Util.CheckRoll(self.critStat, self.characterBody.master),
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
                                                              self.gameObject, self.damageStat * FireTwinRocket.damageCoefficient,
                                                              FireTwinRocket.force, Util.CheckRoll(self.critStat, self.characterBody.master),
                                                              DamageColorIndex.Default, null, -1f);
                }
            });
        }

        private void LoopAllMinionOwnerships(CharacterMaster ownerMaster, Action<GameObject> actionToRun)
        {
            MinionOwnership[] minionOwnerships = Object.FindObjectsOfType<MinionOwnership>();
            foreach (MinionOwnership minionOwnership in minionOwnerships)
            {
                if (minionOwnership.ownerMaster == ownerMaster)
                {
                    CharacterMaster minionMaster = minionOwnership.GetComponent<CharacterMaster>();
                    if (minionMaster && FilterDrones(minionMaster.name))
                    {
                        GameObject minion = minionMaster.GetBody().gameObject;
                        actionToRun(minion);
                    }
                }
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

        Transform t;
        OptionTracker ot;
        bool init = true;

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

        Vector3 previousPosition = new Vector3();
        bool init = true;
        int previousOptionItemCount = 0;

        Transform t;

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

        readonly float baseValue = 1f;
        readonly float amplitude = .25f;
        readonly float phase = 0f;
        readonly float frequency = 1f;

        readonly Light[] lightObjects = new Light[3];
        readonly float[] originalRange = new float[3];
        readonly float[] ampMultiplier = new float[4] { 1.2f, 1f, .8f, .4f };
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