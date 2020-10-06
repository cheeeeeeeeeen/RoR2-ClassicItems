using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using static Chen.ClassicItems.SpawnOptionsForClients;
using static Chen.ClassicItems.SyncFlamethrowerEffectForClients;

namespace Chen.ClassicItems
{
    public class OptionBehavior : MonoBehaviour
    {
        public GameObject owner;
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
                else
                {
                    ClassicItemsPlugin._logger.LogWarning($"OptionBehavior.Update: Lost owner or ot. Destroying this Option. numbering = {numbering}");
                    Destroy(gameObject);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void FixedUpdate()
        {
            if (init && owner)
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
        public CharacterMaster masterCharacterMaster { get; private set; }
        public OptionMasterTracker masterOptionTracker { get; private set; }
        public CharacterMaster characterMaster { get; private set; }
        public CharacterBody characterBody { get; private set; }

        public List<Tuple<MessageType, NetworkInstanceId, short, float, Vector3>> netIds { get; private set; } =
            new List<Tuple<MessageType, NetworkInstanceId, short, float, Vector3>>();

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
            if (!init && masterOptionTracker && characterMaster)
            {
                if (previousPosition != t.position || characterMaster.name.Contains("Turret1"))
                {
                    flightPath.Insert(0, t.position);
                    if (flightPath.Count > masterOptionTracker.optionItemCount * distanceInterval) flightPath.RemoveAt(flightPath.Count - 1);
                }
                previousPosition = t.position;
            }
            SyncFlamethrowerEffects();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void FixedUpdate()
        {
            if (!masterOptionTracker)
            {
                characterBody = gameObject.GetComponent<CharacterBody>();
                if (!characterBody)
                {
                    ClassicItemsPlugin._logger.LogWarning("OptionTracker Initialization: characterBody does not exist!");
                    return;
                }
                characterMaster = characterBody.master;
                if (!characterMaster)
                {
                    ClassicItemsPlugin._logger.LogWarning("OptionTracker Initialization: characterMaster does not exist!");
                    return;
                }
                masterCharacterMaster = characterMaster.minionOwnership.ownerMaster;
                if (!masterCharacterMaster)
                {
                    ClassicItemsPlugin._logger.LogWarning("OptionTracker Initialization: masterCharacterMaster does not exist!");
                    return;
                }
                masterOptionTracker = masterCharacterMaster.gameObject.GetComponent<OptionMasterTracker>();
                if (!masterOptionTracker)
                {
                    ClassicItemsPlugin._logger.LogWarning("OptionTracker Initialization: masterOptionTracker is null.");
                    return;
                }
            }
            if (init && masterOptionTracker.optionItemCount > 0)
            {
                init = false;
                previousPosition = t.position;
                ManageFlightPath(masterOptionTracker.optionItemCount);
            }
            else if (!init && masterOptionTracker.optionItemCount > 0)
            {
                int diff = masterOptionTracker.optionItemCount - previousOptionItemCount;
                if (diff > 0 || diff < 0)
                {
                    previousOptionItemCount = masterOptionTracker.optionItemCount;
                    ManageFlightPath(diff);
                }
            }
            else if (!init && masterOptionTracker.optionItemCount <= 0)
            {
                init = true;
                flightPath.Clear();
                previousOptionItemCount = 0;
            }
        }

        private void SyncFlamethrowerEffects()
        {
            if (NetworkServer.active && NetworkUser.AllParticipatingNetworkUsersReady() && netIds.Count > 0)
            {
                Tuple<MessageType, NetworkInstanceId, short, float, Vector3>[] listCopy = new Tuple<MessageType, NetworkInstanceId, short, float, Vector3>[netIds.Count];
                netIds.CopyTo(listCopy);
                netIds.Clear();
                for (int i = 0; i < listCopy.Length; i++)
                {
                    MessageType messageType = listCopy[i].Item1;
                    NetworkInstanceId netId = listCopy[i].Item2;
                    short numbering = listCopy[i].Item3;
                    float duration = listCopy[i].Item4;
                    Vector3 direction = listCopy[i].Item5;
                    new SyncFlamethrowerEffectForClients(messageType, netId, numbering, duration, direction).Send(NetworkDestination.Clients);
                }
            }
        }

        private void ManageFlightPath(int difference)
        {
            int flightPathCap = masterOptionTracker.optionItemCount * distanceInterval;
            if (difference > 0) while (flightPath.Count < flightPathCap) flightPath.Add(t.position);
            else if (difference < 0) while (flightPath.Count >= flightPathCap) flightPath.RemoveAt(flightPath.Count - 1);
        }

        public static OptionTracker GetOrCreateComponent(GameObject me)
        {
            OptionTracker tracker = me.GetComponent<OptionTracker>() ?? me.AddComponent<OptionTracker>();
            return tracker;
        }

        public static OptionTracker GetOrCreateComponent(CharacterBody me)
        {
            return GetOrCreateComponent(me.gameObject);
        }
    }

    public class OptionMasterTracker : MonoBehaviour
    {
        public int optionItemCount = 0;
        public List<Tuple<GameObjectType, NetworkInstanceId, short>> netIds { get; private set; } = new List<Tuple<GameObjectType, NetworkInstanceId, short>>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void FixedUpdate()
        {
            if (NetworkServer.active && NetworkUser.AllParticipatingNetworkUsersReady() && netIds.Count > 0)
            {
                Tuple<GameObjectType, NetworkInstanceId, short>[] listCopy = new Tuple<GameObjectType, NetworkInstanceId, short>[netIds.Count];
                netIds.CopyTo(listCopy);
                netIds.Clear();
                for (int i = 0; i < listCopy.Length; i++)
                {
                    GameObjectType bodyOrMaster = listCopy[i].Item1;
                    NetworkInstanceId netId = listCopy[i].Item2;
                    short numbering = listCopy[i].Item3;
                    new SpawnOptionsForClients(bodyOrMaster, netId, numbering).Send(NetworkDestination.Clients);
                }
            }
        }

        public static OptionMasterTracker GetOrCreateComponent(CharacterMaster me)
        {
            return GetOrCreateComponent(me.gameObject);
        }

        public static OptionMasterTracker GetOrCreateComponent(GameObject me)
        {
            OptionMasterTracker tracker = me.GetComponent<OptionMasterTracker>() ?? me.AddComponent<OptionMasterTracker>();
            return tracker;
        }

        public static void SpawnOption(GameObject owner, int itemCount)
        {
            OptionTracker ownerOptionTracker = OptionTracker.GetOrCreateComponent(owner);
            GameObject option = Instantiate(ClassicItemsPlugin.gradiusOptionPrefab, owner.transform.position, owner.transform.rotation);
            OptionBehavior behavior = option.GetComponent<OptionBehavior>();
            behavior.owner = owner;
            behavior.numbering = itemCount;
            ownerOptionTracker.existingOptions.Add(option);
        }

        public static void DestroyOption(OptionTracker optionTracker, int optionNumber)
        {
            int index = optionNumber - 1;
            GameObject option = optionTracker.existingOptions[index];
            optionTracker.existingOptions.RemoveAt(index);
            Destroy(option);
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