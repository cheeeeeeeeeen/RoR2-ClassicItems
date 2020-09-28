using RoR2;
using RoR2.Projectile;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using static TILER2.MiscUtil;

namespace Chen.ClassicItems
{
    public class GradiusOption : Item<GradiusOption>
    {
        public override string displayName => "Gradius' Option";
        public override ItemTier itemTier => ItemTier.Tier3;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Utility });

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("How far the Option/Multiple entities are from each other. Higher number could have a performance impact.", AutoItemConfigFlags.None, 0, 100)]
        public int distanceInterval { get; private set; } = 12;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Determines the smoothness of the Option/Multiple movement. Higher number means crispier movement. Lower number means slow but smooth.", AutoItemConfigFlags.None, 0f, 1f)]
        public float crispMoveRate { get; private set; } = 0.95f;

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

                OptionTracker ot = gameObject.GetComponent<OptionTracker>() ?? gameObject.AddComponent<OptionTracker>();
                ot.distanceInterval = distanceInterval;
                int oldCount = ot.optionItemCount;
                int newCount = GetCount(self);
                ot.optionItemCount = newCount;

                if (newCount - oldCount > 0)
                {
                    GameObject gradiusOptionPrefab = new GameObject("GradiusOption");
                    MeshFilter mf = gradiusOptionPrefab.AddComponent<MeshFilter>();
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Mesh m = sphere.GetComponent<MeshFilter>().sharedMesh;
                    mf.sharedMesh = m;
                    Object.Destroy(sphere);
                    gradiusOptionPrefab.AddComponent<MeshRenderer>();
                    gradiusOptionPrefab.transform.localScale = new Vector3(.4f, .4f, .4f);
                    gradiusOptionPrefab.AddComponent<NetworkIdentity>();
                    gradiusOptionPrefab.AddComponent<OptionBehavior>();

                    GameObject option = Object.Instantiate(gradiusOptionPrefab, gameObject.transform.position, gameObject.transform.rotation);
                    OptionBehavior behavior = option.GetComponent<OptionBehavior>();
                    behavior.master = behavior.owner = gameObject;
                    behavior.numbering = newCount;
                    ot.existingOptions.Add(option);

                    NetworkServer.Spawn(option);
                }
                else
                {
                    NetworkServer.Destroy(ot.existingOptions[oldCount - 1]);
                    Object.Destroy(ot.existingOptions[oldCount - 1]);
                    ot.existingOptions.RemoveAt(ot.existingOptions.Count - 1);
                }
            }
        }
    }

    public class OptionBehavior : MonoBehaviour
    {
        public GameObject owner;
        public GameObject master;
        public int numbering = 0;
        public float crispMoveRate = .95f;

        Transform t;
        OptionTracker ot;
        bool init = true;

        private void Awake()
        {
            t = gameObject.transform;
        }

        private void Update()
        {
            if (init && owner && master)
            {
                init = false;
                ot = owner.GetComponent<OptionTracker>();
            }
        }

        private void FixedUpdate()
        {
            if (!init)
            {
                t.position = (crispMoveRate * t.position) + ((1f - crispMoveRate) * ot.flightPath[numbering * ot.distanceInterval - 1]);
                gameObject.transform.rotation = owner.transform.rotation;
            }
        }
    }

    public class OptionTracker : MonoBehaviour
    {
        public List<Vector3> flightPath { get; private set; } = new List<Vector3>();
        public List<GameObject> existingOptions { get; private set; } = new List<GameObject>();
        public int optionItemCount = 0;
        public int distanceInterval = 20;

        Vector3 previousPosition = new Vector3();
        bool init = true;
        int previousOptionItemCount = 0;

        Transform t;

        private void Awake()
        {
            t = gameObject.transform;
        }

        private void Update()
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

        private void FixedUpdate()
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