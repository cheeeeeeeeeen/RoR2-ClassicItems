using Chen.Helpers.CollectionHelpers;
using Chen.Helpers.GeneralHelpers;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using ThinkInvisible.ClassicItems;
using TILER2;
using UnityEngine;
using static TILER2.MiscUtil;
using static TILER2.StatHooks;

namespace Chen.ClassicItems
{
    /// <summary>
    /// Singleton equipment class powered by TILER2 that implements Drone Repair Kit functionality.
    /// </summary>
    public class DroneRepairKit : Equipment_V2<DroneRepairKit>
    {
        /// <summary>
        /// The regen buff associated with the Drone Repair Kit to be given to affected drones.
        /// </summary>
        public static BuffIndex regenBuff { get; private set; }

        private static readonly List<string> DronesList = new List<string>
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override string displayName => "Drone Repair Kit";

        public override float cooldown { get; protected set; } = 45f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Enable regen buff on top of the heal for Drones when this equipment is used.")]
        public bool enableRegenBuff { get; private set; } = true;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Type of healing done to Drones. 0 = Percentage. 1 = Fixed amount.", AutoConfigFlags.None, 0, 1)]
        public int healType { get; private set; } = 0;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Amount of HP healed per drone. If healing type is Percentage, 1 = 100%; based on max health. " +
                    "If Fixed, the amount here is the HP restored to Drones. Affected by Embryo.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float healthRestoreAmount { get; private set; } = 1f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Type of regen applied Drones. 0 = Percentage. 1 = Fixed amount.", AutoConfigFlags.None, 0, 1)]
        public int regenType { get; private set; } = 0;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Amount of regeneration per drone. If healing type is Percentage, 1 = 100%; based on BASE max health of the drone. " +
                    "Base Max health is the unmodified max health of the drone. Scales with level. Affected by Embryo." +
                    "If Fixed, the amount here is the HP restored to Drones.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float healthRegenAmount { get; private set; } = .01f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Duration of the regen buff granted by this equipment.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float regenDuration { get; private set; } = 8f;

        protected override string GetNameString(string langid = null) => displayName;

        protected override string GetPickupString(string langid = null) => "Repair all active drones.";

        protected override string GetDescString(string langid = null)
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

        protected override string GetLoreString(string langid = null) =>
            "Another old item found from random places of the Planet, but our machinist took it and started tinkering with the tools inside it.\n\n" +
            "\"Ah, this is a good find,\" he exclaimed as he left with the toolbox.\n\n" +
            "It looked like nanodrones were inside that box. Ah, for as long as it is put to use, I have no problem with it.\n\n" +
            "\"What are you doing? Wait, are you trying to fuse your own---\"";

        public override void SetupBehavior()
        {
            base.SetupBehavior();
            CustomBuff regenBuffDef = new CustomBuff(new BuffDef
            {
                buffColor = Color.green,
                canStack = true,
                isDebuff = false,
                name = "CCIDroneRepairKit",
                iconPath = "@ChensClassicItems:Assets/ClassicItems/Icons/dronerepairkit_buff_icon.png"
            });
            regenBuff = BuffAPI.Add(regenBuffDef);

            Embryo_V2.instance.Compat_Register(catalogIndex);
        }

        public override void Install()
        {
            base.Install();
            GetStatCoefficients += DroneRepairKit_GetStatCoefficients;
        }

        public override void Uninstall()
        {
            base.Uninstall();
            GetStatCoefficients -= DroneRepairKit_GetStatCoefficients;
        }

        protected override bool PerformEquipmentAction(EquipmentSlot slot)
        {
            CharacterBody body = slot.characterBody;
            if (!body) return false;
            CharacterMaster master = body.master;
            if (!master) return false;
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
                }
            });
            return true;
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        private void DroneRepairKit_GetStatCoefficients(CharacterBody sender, StatHookEventArgs args)
        {
            if (enableRegenBuff)
            {
                float currentRegen = healthRegenAmount;
                if (regenType == 0) currentRegen *= sender.baseMaxHealth + sender.levelMaxHealth * (sender.level - 1);
                if (sender && sender.HasBuff(regenBuff))
                {
                    args.baseRegenAdd += currentRegen * sender.GetBuffCount(regenBuff);
                }
            }
        }

        private void LoopAllMinionOwnerships(CharacterMaster triggerer, Action<CharacterBody> actionToRun)
        {
            triggerer.LoopMinions((minionMaster) =>
            {
                if (minionMaster && DronesList.Exists((item) => minionMaster.name.Contains(item)))
                {
                    CharacterBody minionBody = minionMaster.GetBody();
                    if (minionBody) actionToRun(minionBody);
                }
            });
        }

        private void ApplyHealing(HealthComponent healthComponent, CharacterBody body = null)
        {
            bool embryoProc = instance.CheckEmbryoProc(body);
            float restore = healthRestoreAmount;
            if (embryoProc) restore *= 2f;
            if (!body) body = healthComponent.body;
            if (healType == 0) healthComponent.HealFraction(restore, default);
            else healthComponent.Heal(restore, default);
            if (enableRegenBuff)
            {
                body.AddTimedBuff(regenBuff, regenDuration);
                if (embryoProc) body.AddTimedBuff(regenBuff, regenDuration);
            }
        }

        /// <summary>
        /// Adds a support for a custom drone so that Drone Repair Kit also heals and applies regen to them.
        /// </summary>
        /// <param name="masterName">The CharacterMaster name of the drone.</param>
        /// <returns>True if the drone is supported. False if it is already supported.</returns>
        public bool SupportCustomDrone(string masterName)
        {
            return DronesList.ConditionalAdd(masterName, item => item == masterName);
        }

        /// <summary>
        /// Removes support for a custom drone, thus removing them from Drone Repair Kit's scope.
        /// </summary>
        /// <param name="masterName">The CharacterMaster name of the drone.</param>
        /// <returns>True if the drone is unsupported. False if it is already unsupported.</returns>
        public bool UnsupportCustomDrone(string masterName)
        {
            return DronesList.ConditionalAdd(masterName, item => item == masterName);
        }
    }
}