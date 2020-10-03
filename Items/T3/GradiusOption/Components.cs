using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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
                    ClassicItemsPlugin._logger.LogDebug($"OptionBehavior.Update: Lost owner or ot. Destroying this option.");
                    if (NetworkServer.active)
                    {
                        NetworkServer.Destroy(gameObject);
                        Destroy(gameObject);
                    }
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
            if (!init && masterOptionTracker)
            {
                if (previousPosition != t.position)
                {
                    flightPath.Insert(0, t.position);
                    if (flightPath.Count > masterOptionTracker.optionItemCount * distanceInterval)
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
            if (!masterOptionTracker)
            {
                characterBody = gameObject.GetComponent<CharacterBody>();
                if (characterBody)
                {
                    characterMaster = characterBody.master;
                    if (characterMaster)
                    {
                        masterCharacterMaster = characterMaster.minionOwnership.ownerMaster;
                        if (masterCharacterMaster)
                        {
                            masterOptionTracker = masterCharacterMaster.gameObject.GetComponent<OptionMasterTracker>();
                            if (masterOptionTracker) ClassicItemsPlugin._logger.LogDebug("In Initialization of OptionTracker: masterOptionTracker is set.");
                            else ClassicItemsPlugin._logger.LogWarning("In Initialization of OptionTracker: masterOptionTracker is NULL.");
                        }
                        else ClassicItemsPlugin._logger.LogWarning("In Initialization of OptionTracker: masterCharacterMaster does not exist!");
                    }
                    else ClassicItemsPlugin._logger.LogWarning("In Initialization of OptionTracker: characterMaster does not exist!");
                }
                else ClassicItemsPlugin._logger.LogWarning("In Initialization of OptionTracker: characterBody does not exist!");
            }
            if (init && masterOptionTracker.optionItemCount > 0)
            {
                init = false;
                previousPosition = t.position;
                ManageFlightPath(1);
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

        private void ManageFlightPath(int difference)
        {
            ClassicItemsPlugin._logger.LogDebug($"ManageFlightPath: difference is {difference}");
            int flightPathCap = masterOptionTracker.optionItemCount * distanceInterval;
            ClassicItemsPlugin._logger.LogDebug($"ManageFlightPath: Pre-computed flightPathCap = {flightPathCap}");
            if (difference > 0)
            {
                while (flightPath.Count < flightPathCap)
                {
                    ClassicItemsPlugin._logger.LogDebug($"ManageFlightPath: Inserting entry!");
                    flightPath.Add(t.position);
                }
            }
            else if (difference < 0)
            {
                while (flightPath.Count >= flightPathCap)
                {
                    ClassicItemsPlugin._logger.LogDebug($"ManageFlightPath: Removing entry.");
                    flightPath.RemoveAt(flightPath.Count - 1);
                }
            }
        }

        public static OptionTracker GetOrCreateComponent(GameObject me, int interval = 20)
        {
            OptionTracker tracker = me.GetComponent<OptionTracker>() ?? me.AddComponent<OptionTracker>();
            tracker.distanceInterval = interval;
            return tracker;
        }

        public static OptionTracker GetOrCreateComponent(CharacterBody me, int interval = 20)
        {
            return GetOrCreateComponent(me.gameObject, interval);
        }
    }

    public class OptionMasterTracker : MonoBehaviour
    {
        public int optionItemCount = 0;
        public List<Tuple<NetworkInstanceId, short, bool>> netIds { get; private set; } = new List<Tuple<NetworkInstanceId, short, bool>>();

        private float syncSeconds = 1f;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void FixedUpdate()
        {
            if (NetworkServer.active && netIds.Count > 0)
            {
                ClassicItemsPlugin._logger.LogDebug($"Server Sync Attempt: New netIds found. Trying to sync.");
                for (int i = 0; i < netIds.Count;)
                {
                    StartCoroutine(QueueSending(netIds[i].Item1, netIds[i].Item2, netIds[i].Item3));
                    netIds.RemoveAt(i);
                }
            }
        }

        private IEnumerator QueueSending(NetworkInstanceId netId, short numbering, bool bodyOrMaster)
        {
            yield return new WaitForSeconds(syncSeconds);
            new SpawnOptionsForClients(netId, numbering, bodyOrMaster).Send(NetworkDestination.Clients);
            ClassicItemsPlugin._logger.LogDebug($"Server Sync Attempt: Sent data <{netId}, {numbering}, {bodyOrMaster}>");
        }

        public static OptionMasterTracker GetOrCreateComponent(CharacterMaster me, float syncSeconds = 1f)
        {
            return GetOrCreateComponent(me.gameObject, syncSeconds);
        }

        public static OptionMasterTracker GetOrCreateComponent(GameObject me, float syncSeconds = 1f)
        {
            OptionMasterTracker tracker = me.GetComponent<OptionMasterTracker>() ?? me.AddComponent<OptionMasterTracker>();
            tracker.syncSeconds = syncSeconds;
            return tracker;
        }

        public static void SpawnOption(GameObject owner, int itemCount, int interval = 20)
        {
            OptionTracker ownerOptionTracker = OptionTracker.GetOrCreateComponent(owner, interval);
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
