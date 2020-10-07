using RoR2;
using System;
using System.Collections.Generic;
using ThinkInvisible.ClassicItems;
using TILER2;
using UnityEngine;
using static TILER2.MiscUtil;
using Object = UnityEngine.Object;

namespace Chen.ClassicItems
{
    public class DroneRepairKit : Equipment<DroneRepairKit>
    {
        public override string displayName => "Drone Repair Kit";

        public override float eqpCooldown { get; protected set; } = 45f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Enable regen buff on top of the heal for Drones when this equipment is used.",
                        AutoItemConfigFlags.None)]
        public bool enableRegenBuff { get; private set; } = true;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Type of healing done to Drones. 0 = Percentage. 1 = Fixed amount.", AutoItemConfigFlags.None, 0, 1)]
        public int healType { get; private set; } = 0;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Amount of HP healed per drone. If healing type is Percentage, 1 = 100%; based on max health. " +
                        "If Fixed, the amount here is the HP restored to Drones. Affected by Embryo.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float healthRestoreAmount { get; private set; } = 1f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Type of regen applied Drones. 0 = Percentage. 1 = Fixed amount.", AutoItemConfigFlags.None, 0, 1)]
        public int regenType { get; private set; } = 0;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Amount of regenration per drone. If healing type is Percentage, 1 = 100%; based on BASE max health of the drone. " +
                        "Base Max health is the unmodified max health of the drone. Scales with level. Affected by Embryo." +
                        "If Fixed, the amount here is the HP restored to Drones.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float healthRegenAmount { get; private set; } = .01f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Duration of the regen buff granted by this equipment.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float regenDuration { get; private set; } = 8f;

        protected override string NewLangName(string langid = null) => displayName;

        protected override string NewLangPickup(string langid = null) => "Repair all active drones.";

        protected override string NewLangDesc(string langid = null)
        {
            string desc = "";
            if (healthRestoreAmount > 0 && healthRegenAmount > 0) desc += "Repairs all owned drones,";
            else desc += "Does nothing";
            if (healthRestoreAmount > 0)
            {
                desc += $" restoring their health for <style=cIsHealing>";
                if (healType == 0) desc += $"{Pct(healthRestoreAmount)} of their maximum health";
                else desc += $"{healthRestoreAmount} ";
                desc += "</style>";
            }
            if (enableRegenBuff)
            {
                if (healthRestoreAmount > 0 && healthRegenAmount > 0) desc += " and";
                if (healthRegenAmount > 0)
                {
                    desc += " granting health regeneration of <style=cIsHealing>";
                    if (regenType == 0) desc += $"{Pct(healthRegenAmount)} max health per second";
                    else desc += $"+{healthRegenAmount} hp/s";
                    desc += $"</style>. Regen lasts for {regenDuration} second";
                    if (regenDuration != 1) desc += "s";
                }
            }
            desc += ".";
            return desc;
        }

        protected override string NewLangLore(string langid = null) => "A relic of times long past (ChensClassicItems mod)";

        public static readonly List<string> DronesList = new List<string>
        {
            "BackupDrone",
            "BackupDroneOld",
            "Drone1",
            "Drone2",
            "EmergencyDrone",
            "EquipmentDrone",
            "FlameDrone",
            "MegaDrone",
            "DroneMissile",
            "MissileDrone",
            "Turret1"
        };

        public DroneRepairKit()
        {
            onBehav += () =>
            {
                Embryo.instance.Compat_Register(regIndex);
            };
        }

        protected override void LoadBehavior()
        {
            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
        }

        protected override void UnloadBehavior()
        {
            On.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
        }

        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            if (enableRegenBuff)
            {
                float currentRegen = healthRegenAmount;
                if (regenType == 0) currentRegen *= self.baseMaxHealth + self.levelMaxHealth * (self.level - 1);
                orig(self);
                if (self && self.HasBuff(ClassicItemsPlugin.droneRepairKitRegenBuff))
                {
                    ClassicItemsPlugin._logger.LogMessage(self.GetBuffCount(ClassicItemsPlugin.droneRepairKitRegenBuff));
                    self.regen += currentRegen * self.GetBuffCount(ClassicItemsPlugin.droneRepairKitRegenBuff);
                }
            }
            else orig(self);
        }

        protected override bool OnEquipUseInner(EquipmentSlot slot)
        {
            CharacterBody body = slot.characterBody;
            if (!body) return false;
            CharacterMaster master = body.master;
            if (!master) return false;
            bool embryoProc = instance.CheckEmbryoProc(body);
            LoopAllMinionOwnerships(master, (minionBody) =>
            {
                HealthComponent healthComponent = minionBody.healthComponent;
                if (healthComponent)
                {
                    GameObject minion = minionBody.gameObject;
                    EffectData effectData = new EffectData { origin = minion.transform.position };
                    effectData.SetNetworkedObjectReference(minion);
                    EffectManager.SpawnEffect(Resources.Load<GameObject>("prefabs/effects/MedkitHealEffect"), effectData, true);
                    ApplyHealing(healthComponent, minionBody);
                    if (embryoProc) ApplyHealing(healthComponent, minionBody);
                }
            });
            return true;
        }

        private void LoopAllMinionOwnerships(CharacterMaster ownerMaster, Action<CharacterBody> actionToRun)
        {
            MinionOwnership[] minionOwnerships = Object.FindObjectsOfType<MinionOwnership>();
            foreach (MinionOwnership minionOwnership in minionOwnerships)
            {
                if (minionOwnership && minionOwnership.ownerMaster && minionOwnership.ownerMaster == ownerMaster)
                {
                    CharacterMaster minionMaster = minionOwnership.GetComponent<CharacterMaster>();
                    if (minionMaster && DronesList.Exists((item) => minionMaster.name.Contains(item)))
                    {
                        CharacterBody minionBody = minionMaster.GetBody();
                        if (minionBody) actionToRun(minionBody);
                    }
                }
            }
        }

        private void ApplyHealing(HealthComponent healthComponent, CharacterBody body = null)
        {
            if (!body) body = healthComponent.body;
            if (healType == 0) healthComponent.HealFraction(healthRestoreAmount, default);
            else healthComponent.Heal(healthRestoreAmount, default);
            if (enableRegenBuff) body.AddTimedBuff(ClassicItemsPlugin.droneRepairKitRegenBuff, regenDuration);
        }
    }
}