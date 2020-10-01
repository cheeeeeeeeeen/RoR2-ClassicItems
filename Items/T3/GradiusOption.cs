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

        protected override string NewLangName(string langid = null) => displayName;

        protected override string NewLangPickup(string langid = null) => $"Deploy the Option, an ultimate weapon from the Gradius Federation, for each owned Drone.";

        protected override string NewLangDesc(string langid = null)
        {
            return NewLangPickup(langid);
        }

        protected override string NewLangLore(string langid = null) => "An item from a different world (ChensClassicItems)";

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
        }

        private CharacterBody CharacterMaster_SpawnBody(On.RoR2.CharacterMaster.orig_SpawnBody orig, CharacterMaster self, GameObject bodyPrefab, Vector3 position, Quaternion rotation)
        {
            CharacterBody result = orig(self, bodyPrefab, position, rotation);
            if (result)
            {
                int currentCount = GetCount(result);
                if (currentCount > 0)
                {
                    GameObject owner;
                    if (self.minionOwnership && self.minionOwnership.ownerMaster) owner = self.minionOwnership.ownerMaster.gameObject;
                    else owner = self.gameObject;
                    SpawnOption(self.gameObject, owner, currentCount);
                }
            }
            return result;
        }

        private void CharacterBody_OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            if (self.master && GetCount(self) > 0)
            {
                GameObject gameObject = self.gameObject;
                OptionTracker optionTracker = gameObject.GetComponent<OptionTracker>() ?? gameObject.AddComponent<OptionTracker>();
                int oldCount = optionTracker.optionItemCount;
                int newCount = GetCount(self);

                if (newCount - oldCount > 0)
                {
                    SpawnOption(gameObject, gameObject, newCount);
                    LoopAllMinionOwnerships(self.master, (minion) =>
                    {
                        SpawnOption(gameObject, minion, newCount);
                    });
                }
                else if (newCount - oldCount < 0)
                {
                    DestroyOption(optionTracker, oldCount);
                    LoopAllMinionOwnerships(self.master, (minion) =>
                    {
                        OptionTracker minionOptionTracker = minion.GetComponent<OptionTracker>();
                        if (minionOptionTracker) DestroyOption(minionOptionTracker, oldCount);
                    });
                }
            }
        }

        private void Flamethrower_OnExit(On.EntityStates.Mage.Weapon.Flamethrower.orig_OnExit orig, MageWeapon.Flamethrower self)
        {
            orig(self);
            if (self.characterBody.name == "FlameDroneBody(Clone)" && self.characterBody.master.name == "FlameDroneMaster(Clone)")
            {
                FireForAllMinions(self, (option, target) =>
                {
                    if (self.stopwatch >= self.entryDuration && !self.hasBegunFlamethrower)
                    {
                        Util.PlaySound(MageWeapon.Flamethrower.startAttackSoundString, option);
                    }
                });
            }
        }

        private void Flamethrower_FixedUpdate(On.EntityStates.Mage.Weapon.Flamethrower.orig_FixedUpdate orig, MageWeapon.Flamethrower self)
        {
            orig(self);
            if (self.characterBody.name == "FlameDroneBody(Clone)" && self.characterBody.master.name == "FlameDroneMaster(Clone)")
            {
                FireForAllMinions(self, (option, target) =>
                {
                    if (self.stopwatch >= self.entryDuration && !self.hasBegunFlamethrower)
                    {
                        Util.PlaySound(MageWeapon.Flamethrower.startAttackSoundString, option);
                    }
                });
            }
        }

        private void Flamethrower_FireGauntlet(On.EntityStates.Mage.Weapon.Flamethrower.orig_FireGauntlet orig, MageWeapon.Flamethrower self, string muzzleString)
        {
            orig(self, muzzleString);
            if (self.characterBody.name == "FlameDroneBody(Clone)" && self.characterBody.master.name == "FlameDroneMaster(Clone)")
            {
                FireForAllMinions(self, (option, target) =>
                {
                    if (self.isAuthority)
                    {
                        new BulletAttack
                        {
                            owner = option,
                            weapon = option,
                            origin = option.transform.position,
                            aimVector = (target.transform.position - option.transform.position).normalized,
                            minSpread = 0f,
                            damage = self.tickDamageCoefficient * self.damageStat,
                            force = MageWeapon.Flamethrower.force,
                            tracerEffectPrefab = FireGatling.tracerEffectPrefab,
                            muzzleName = muzzleString,
                            hitEffectPrefab = MageWeapon.Flamethrower.impactEffectPrefab,
                            isCrit = self.isCrit,
                            radius = MageWeapon.Flamethrower.radius,
                            falloffModel = BulletAttack.FalloffModel.None,
                            stopperMask = LayerIndex.world.mask,
                            procCoefficient = MageWeapon.Flamethrower.procCoefficientPerTick,
                            maxDistance = self.maxDistance,
                            smartCollision = true,
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
                        owner = option,
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
                        owner = option,
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
                        owner = option,
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
                    GameObject minion = minionOwnership.GetComponent<CharacterMaster>().GetBody().gameObject;
                    actionToRun(minion);
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

        private void SpawnOption(GameObject master, GameObject owner, int itemCount, OptionTracker optionTracker = null)
        {
            if (!optionTracker) optionTracker = owner.GetComponent<OptionTracker>() ?? owner.AddComponent<OptionTracker>();
            optionTracker.optionItemCount = itemCount;
            GameObject option = Object.Instantiate(ClassicItemsPlugin.gradiusOptionPrefab, owner.transform.position, owner.transform.rotation);
            OptionBehavior behavior = option.GetComponent<OptionBehavior>();
            behavior.owner = owner;
            behavior.master = master;
            behavior.numbering = optionTracker.optionItemCount;
            optionTracker.existingOptions.Add(option);
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
    }

    public class OptionBehavior : MonoBehaviour
    {
        public GameObject owner;
        public GameObject master;
        public int numbering = 0;
        public Transform flamethrowerTransform;

        Transform t;
        OptionTracker ot;
        TeamComponent tc, otc;
        bool init = true;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void Awake()
        {
            t = gameObject.transform;
            tc = gameObject.GetComponent<TeamComponent>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void Update()
        {
            if (!init)
            {
                t.position = ot.flightPath[numbering * ot.distanceInterval - 1];
                gameObject.transform.rotation = owner.transform.rotation;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void FixedUpdate()
        {
            if (init && owner && master)
            {
                init = false;
                ot = owner.GetComponent<OptionTracker>();
                otc = owner.GetComponent<TeamComponent>();
                tc.teamIndex = otc.teamIndex;
            }
            else if (!init && tc.teamIndex != otc.teamIndex)
            {
                tc.teamIndex = otc.teamIndex;
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
}