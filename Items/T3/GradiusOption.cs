using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Chen.ClassicItems
{
    public class GradiusOption : Item<GradiusOption>
    {
        public override string displayName => "Gradius' Option";
        public override ItemTier itemTier => ItemTier.Tier3;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Utility });

        protected override string NewLangName(string langid = null) => displayName;

        protected override string NewLangPickup(string langid = null) => $"Deploy an Option drone, an ultimate weapon from the Gradius Federation.";

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
        }

        protected override void UnloadBehavior()
        {
            On.RoR2.CharacterBody.OnInventoryChanged -= CharacterBody_OnInventoryChanged;
        }

        private void CharacterBody_OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            if (GetCount(self) > 0)
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
                    LoopAllMinionOwnerships(self.master, (minion) =>
                    {
                        OptionTracker minionOptionTracker = minion.GetComponent<OptionTracker>();
                        if (minionOptionTracker) DestroyOption(optionTracker, oldCount);
                    });
                }
            }
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