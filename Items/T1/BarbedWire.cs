﻿using RoR2;
using UnityEngine;
using BepInEx.Configuration;
using static ThinkInvisible.ClassicItems.MiscUtil;
using static ThinkInvisible.ClassicItems.ClassicItemsPlugin.MasterItemList;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections.ObjectModel;
using R2API;

namespace ThinkInvisible.ClassicItems
{
    public class BarbedWire : ItemBoilerplate {
        public override string itemCodeName{get;} = "BarbedWire";

        private ConfigEntry<float> cfgBaseRadius;
        private ConfigEntry<float> cfgStackRadius;
        private ConfigEntry<float> cfgBaseDmg;
        private ConfigEntry<float> cfgStackDmg;
        private ConfigEntry<bool> cfgOneOnly;

        public float baseRadius {get; private set;}
        public float stackRadius {get; private set;}
        public float baseDmg {get; private set;}
        public float stackDmg {get; private set;}
		public bool oneOnly {get; private set;}

		internal static GameObject barbedWardPrefab;

        protected override void SetupConfigInner(ConfigFile cfl) {
            cfgBaseRadius = cfl.Bind(new ConfigDefinition("Items." + itemCodeName, "BaseRadius"), 5f, new ConfigDescription(
                "AoE radius for the first stack of Barbed Wire.",
                new AcceptableValueRange<float>(0f, float.MaxValue)));
            cfgStackRadius = cfl.Bind(new ConfigDefinition("Items." + itemCodeName, "StackRadius"), 1f, new ConfigDescription(
                "AoE radius to add per additional stack of Barbed Wire.",
                new AcceptableValueRange<float>(0f, float.MaxValue)));
            cfgBaseDmg = cfl.Bind(new ConfigDefinition("Items." + itemCodeName, "BaseDmg"), 0.5f, new ConfigDescription(
                "AoE damage/sec (as fraction of owner base damage) for the first stack of Barbed Wire.",
                new AcceptableValueRange<float>(0f, float.MaxValue)));
            cfgStackDmg = cfl.Bind(new ConfigDefinition("Items." + itemCodeName, "StackDmg"), 0.15f, new ConfigDescription(
                "AoE damage/sec (as fraction of owner base damage) per additional stack of Barbed Wire.",
                new AcceptableValueRange<float>(0f, float.MaxValue)));
			cfgOneOnly = cfl.Bind(new ConfigDefinition("Items." + itemCodeName, "OneOnly"), true, new ConfigDescription(
                "If true, Barbed Wire only affects one target at most. If false, Barbed Wire affects every target in range."));

            baseRadius = cfgBaseRadius.Value;
            stackRadius = cfgStackRadius.Value;
            baseDmg = cfgBaseDmg.Value;
            stackDmg = cfgStackDmg.Value;
			oneOnly = cfgOneOnly.Value;
        }
        
        protected override void SetupAttributesInner() {
            modelPathName = "barbedwirecard.prefab";
            iconPathName = "barbedwire_icon.png";
            RegLang("Barbed Wire",
            	"Hurt nearby enemies.",
            	"Deal <style=cIsDamage>" + pct(baseDmg) + "</style> <style=cStack>(+" + pct(stackDmg) + " per stack)</style> <style=cIsDamage>damage/sec</style> to enemies within <style=cIsDamage>" + baseRadius.ToString("N1") + " m</style> <style=cStack>(+ " + stackRadius.ToString("N2") + " per stack)</style>",
            	"A relic of times long past (ClassicItems mod)");
            _itemTags = new List<ItemTag>{ItemTag.Damage};
            itemTier = ItemTier.Tier1;
        }

        protected override void SetupBehaviorInner() {
			var mshPrefab = Resources.Load<GameObject>("Prefabs/NetworkedObjects/MushroomWard");

			var bwPrefabPrefab = new GameObject("BarbedWardAuraPrefabPrefab");
			bwPrefabPrefab.AddComponent<TeamFilter>();
			bwPrefabPrefab.AddComponent<MeshFilter>().mesh = mshPrefab.GetComponentInChildren<MeshFilter>().mesh;
			bwPrefabPrefab.AddComponent<MeshRenderer>().material = UnityEngine.Object.Instantiate(mshPrefab.GetComponentInChildren<MeshRenderer>().material);
			bwPrefabPrefab.GetComponent<MeshRenderer>().material.SetVector("_TintColor",new Vector4(1f,0f,0f,0.5f));
			var bw = bwPrefabPrefab.AddComponent<BarbedWard>();
			bw.rangeIndicator = bwPrefabPrefab.GetComponent<MeshRenderer>().transform;
			bw.interval = 1f;
			barbedWardPrefab = bwPrefabPrefab.InstantiateClone("BarbedWardAuraPrefab");
			UnityEngine.Object.Destroy(bwPrefabPrefab);
			
            On.RoR2.CharacterBody.OnInventoryChanged += On_CBOnInventoryChanged;
        }

        private void On_CBOnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self) {
			orig(self);
			if(!NetworkServer.active) return;
            var cpt = self.GetComponentInChildren<BarbedWard>()?.gameObject;
			var icnt = GetCount(self);
			if(icnt == 0) {
				if(cpt) {
					UnityEngine.Object.Destroy(cpt);
				}
			} else {
				if(!cpt) {
					cpt = UnityEngine.Object.Instantiate(barbedWardPrefab, self.transform);
					cpt.GetComponent<TeamFilter>().teamIndex = self.teamComponent.teamIndex;
					cpt.GetComponent<BarbedWard>().owner = self.gameObject;
					NetworkServer.Spawn(cpt);
				}
				cpt.GetComponent<BarbedWard>().netRadius = baseRadius + (icnt-1) * stackRadius;
				cpt.GetComponent<BarbedWard>().netDamage = (baseDmg + (icnt-1) * stackDmg) * self.damage;
			}
        }
    }

	[RequireComponent(typeof(TeamFilter))]
	public class BarbedWard : NetworkBehaviour {
		[SyncVar]
		float radius;
		public float netRadius {
			get {return radius;}
			set {base.SetSyncVar<float>(value, ref radius, 1u);}
		}

		[SyncVar]
		float damage;
		public float netDamage {
			get {return damage;}
			set {base.SetSyncVar<float>(value, ref damage, 1u);}
		}

		public float interval;
		public Transform rangeIndicator;

		public GameObject owner;
		
		private TeamFilter teamFilter;
		private float rangeIndicatorScaleVelocity;

		private float stopwatch;

		private void Awake() {
			teamFilter = base.GetComponent<TeamFilter>();
		}

		private void Update() {
			float num = Mathf.SmoothDamp(rangeIndicator.localScale.x, radius, ref rangeIndicatorScaleVelocity, 0.2f);
			rangeIndicator.localScale = new Vector3(num, num, num);
		}

		private void FixedUpdate() {
			stopwatch -= Time.fixedDeltaTime;
			if (stopwatch <= 0f) {
				if(NetworkServer.active) {
					stopwatch = interval;
					ServerProc();
				}
			}
		}

		private void OnDestroy() {
			Destroy(rangeIndicator);
		}

		[Server]
		private void ServerProc() {
			var tind = TeamIndex.Monster | TeamIndex.Neutral | TeamIndex.Player;
			tind &= ~teamFilter.teamIndex;
			ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(tind);
			float sqrad = radius * radius;
			foreach(TeamComponent tcpt in teamMembers) {
				if ((tcpt.transform.position - transform.position).sqrMagnitude <= sqrad) {
					HealthComponent component = tcpt.GetComponent<HealthComponent>();
					if (component && damage > 0f) {
						component.TakeDamage(new DamageInfo {
							attacker = owner,
							crit = false,
							procChainMask = default(ProcChainMask),
							damage = damage,
							damageColorIndex = DamageColorIndex.Bleed,
							damageType = DamageType.AOE,
							force = Vector3.zero,
							position = tcpt.transform.position,
							procCoefficient = 1f,
							inflictor = gameObject
						});
						if(barbedWire.oneOnly) break;
					}
				}
			}
		}
	}
}