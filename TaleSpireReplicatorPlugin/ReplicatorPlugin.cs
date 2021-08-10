using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using Bounce.Unmanaged;
using System.Collections.Generic;
using System.Linq;
using System;
using DataModel;
using Newtonsoft.Json;

namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(FileAccessPlugin.Guid)]
    [BepInDependency(StatMessaging.Guid)]
    public partial class ReplicatorPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "Replicator Plug-In";
        public const string Guid = "org.lordashes.plugins.replicator";
        public const string Version = "1.0.0.0";

        // Loose Dependencies
        public const string CMP = "org.lordashes.plugins.custommini";
        public const string baseMiniNGuid = "5dd58c82-a4a9-4fef-be31-24b50daedecd";

        // Content directory
        private string dir = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.LastIndexOf("/")) + "/TaleSpire_CustomData/";

        private ConfigEntry<KeyboardShortcut> trigger;

        private ReplicationState pluginState = ReplicationState.idle;

        private string _replicatedContent = null;
        private CreatureBoardAsset _replicatedAsset = null;
        private Vector3[] _waypoints = null;
        private int sequencer = 0;

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        public void Awake()
        {
            UnityEngine.Debug.Log("Lord Ashes Replicator Plugin Active.");

            trigger = Config.Bind("Hotkeys", "Open Roll Menu", new KeyboardShortcut(KeyCode.F, KeyCode.LeftControl));

            StatMessaging.Subscribe(ReplicatorPlugin.Guid, ReplicationRequest);

            StateDetection.Initialize(this.GetType());
        }

        /// <summary>
        /// Function for determining if view mode has been toggled and, if so, activating or deactivating Character View mode.
        /// This function is called periodically by TaleSpire.
        /// </summary>
        public void Update()
        {
            if (StrictKeyCheck(trigger.Value))
            {
                SubscribeRulerEvents(RulerEvent);
                CreatureBoardAsset asset = null;
                CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
                _replicatedContent = (asset != null) ? _replicatedContent = LordAshes.StatMessaging.ReadInfo(asset.Creature.CreatureId, CMP) : null;
                sequencer = 1;
                pluginState = ReplicationState.rulerEventStarted;
            }

            switch(pluginState)
            {
                case ReplicationState.idle:
                    break;
                case ReplicationState.rulerEventStarted:
                    if (sequencer == 1) { Debug.Log("Replicator Plugin: Replication Ruler Definition Event Started"); }
                    UpdateRulerEvents();
                    break;
                case ReplicationState.replicationAssetPrompt:
                    if (sequencer == 1) { Debug.Log("Replicator Plugin: Prompt For Replication Content Name"); }
                    break;
                case ReplicationState.rulerEventComplete:
                    if (sequencer == 1)
                    { 
                        Debug.Log("Replicator Plugin: Replication Ruler Definition Event Completed");
                        sequencer = 1+1;
                        pluginState = ReplicationState.creatingBase;
                    }
                    break;
                case ReplicationState.creatingBase:
                    if (sequencer == 1)
                    {
                        Debug.Log("Replicator Plugin: Creating Line Replicator Base"); 
                        CreateMiniBase(_waypoints[0]);
                        sequencer = 20+1;
                        pluginState = ReplicationState.requestingCopies;
                    }
                    break;
                case ReplicationState.requestingCopies:
                    if (sequencer == 1)
                    {
                        Debug.Log("Replicator Plugin: Requesting Replication");
                        RequestingCopyMinis();
                        pluginState = ReplicationState.idle;
                    }
                    break;
                case ReplicationState.creatingCopies:
                    if (sequencer == 1)
                    { 
                        Debug.Log("Replicator Plugin: Processing Replication");
                        CreateCopyMinis();
                        pluginState = ReplicationState.idle;
                    }
                    break;
            }
            if (sequencer != 0) { sequencer--; Debug.Log("Sequence=" + sequencer); }
        }
        public void OnGUI()
        {
            if (pluginState == ReplicationState.rulerEventStarted)
            {
                GUIStyle gs = new GUIStyle() { wordWrap = true, fontSize = 16 };
                gs.normal.textColor = Color.yellow;
                if(_replicatedContent==null)
                {
                    GUI.Label(new Rect(10, 30, 1900, 120), "Line Replicator Active: (Content Name To Be Prompted)", gs);
                }
                else if (_replicatedContent == null)
                {
                    GUI.Label(new Rect(10, 30, 1900, 120), "Line Replicator Active: (Content Name To Be Prompted)", gs);
                }
                else
                {
                    GUI.Label(new Rect(10, 30, 1900, 120), "Line Replicator Active: (Replicating '"+_replicatedContent+"')", gs);
                }
            }
        }

        /// <summary>
        /// Callback method used by the StatMessaging Subscribe to process Replication requests
        /// </summary>
        /// <param name="obj">Changes containing the a ReplicationData object as the value</param>
        private void ReplicationRequest(StatMessaging.Change[] obj)
        {
            Debug.Log("Remote Replication Request");
            if (obj.Length < 1) { return; }
            if (obj.Length > 1) { SystemMessage.DisplayInfoText("Concurrent Line Replication Requests Not Supported.\r\nUsing only first request."); }
            ReplicationData data = JsonConvert.DeserializeObject<ReplicationData>(obj[0].value);
            // Store provided waypoints
            CreaturePresenter.TryGetAsset(obj[0].cid, out _replicatedAsset);
            Debug.Log("Remote Replication Request: Asset "+_replicatedAsset.Creature.CreatureId);
            _replicatedContent = data.content;
            Debug.Log("Remote Replication Request: Content " + _replicatedContent);
            _waypoints = data.GetWaypoints();
            Debug.Log("Remote Replication Request: Waypoints " + _waypoints);
            // Switch plugin mode to create content copies along the waypoints
            sequencer = 1;
            pluginState = ReplicationState.creatingCopies;
        }

        /// <summary>
        /// Callback method used by the Ruler to indicate the ruler tool has been closed 
        /// </summary>
        /// <param name="waypoints">Vector3 array containing the points defining the ruler line</param>
        private void RulerEvent(Vector3[] waypoints)
        {
            _waypoints = waypoints;

            // Check to see if content is been selected (via mini selection)
            if (_replicatedContent != null)
            {
                if (_replicatedContent != "")
                {
                    // Complete the Ruler process if a content source was selected via a selected mini
                    sequencer = 1;
                    pluginState = ReplicationState.rulerEventComplete;
                    return;
                }
            }

            // If a mini was not selected (or does not have a transformation) prompt for the replicated content
            pluginState = ReplicationState.replicationAssetPrompt;

            SystemMessage.AskForTextInput("Replicated Asset...", "Content Name:", "OK", (content) => { _replicatedContent = content; sequencer = 1; pluginState = ReplicationState.rulerEventComplete; }, null, "Cancel", null);
        }

        /// <summary>
        /// Method to spawn a hidden mini to which the replicated line will be attached and remove the creature portion of the base
        /// (Uses predefined mini NGuid as the base. NGuid is defined as a constant at the top of the source code)
        /// </summary>
        /// <param name="position">Vector3 position of the base (i.e. start of line)</param>
        private void CreateMiniBase(Vector3 position)
        {
            CreatureDataV2 cd = new CreatureDataV2();
            cd.BoardAssetIds = new NGuid[] { new NGuid(baseMiniNGuid) };
            cd.ExplicitlyHidden = true;
            cd.Flying = false;
            cd.Stat0 = new CreatureStat(float.MaxValue, float.MaxValue - 1);
            CreatureManager.CreateAndAddNewCreature(cd, position, Quaternion.Euler(0, 0, 0), false, true);
        }

        /// <summary>
        /// Method to request (via StatMessaging) replication of the specified contents along the specified waypoints
        /// </summary>
        private void RequestingCopyMinis()
        {
            Debug.Log("Replicator Plugin:   Find Line Replicator Base...");
            CreatureBoardAsset asset = null;
            foreach (CreatureBoardAsset check in CreaturePresenter.AllCreatureAssets)
            {
                if (check.Creature.Stat0.Max == float.MaxValue && check.Creature.Stat0.Value == (float.MaxValue - 1))
                {
                    CreatureManager.SetCreatureStatByIndex(check.Creature.CreatureId, new CreatureStat(0f, 0f), 0);
                    asset = check;
                }
            }
            if (asset == null) { Debug.LogWarning("Unable to locate Line Replicator base"); pluginState = ReplicationState.idle; return; }
            _replicatedAsset = asset;

            ReplicationData data = new ReplicationData() { content = _replicatedContent };
            data.PutWaypoints(_waypoints);
            StatMessaging.SetInfo(asset.Creature.CreatureId, ReplicatorPlugin.Guid, JsonConvert.SerializeObject(data));

            Debug.Log("Replicator Plugin:   Removing Base Creature...");
            foreach (AssetLoader loader in asset.CreatureLoaders)
            {
                try
                {
                    loader.LoadedAsset.GetComponent<MeshFilter>().mesh.triangles = new int[0];
                }
                catch (Exception) {; }
            }
        }

        /// <summary>
        /// Method used to process a replication request by copying the specified content along the specified waypoints line
        /// </summary>
        private void CreateCopyMinis()
        {
            try
            {
                Debug.Log("Replicator Plugin:   Loading assetBundle '" + _replicatedContent + "'...");
                AssetBundle assetBundle = FileAccessPlugin.AssetBundle.Load(_replicatedContent);
                try
                {
                    for (int w = 1; w < waypoints.Count(); w++)
                    {
                        // Corrects angle on lines going from right to left
                        Vector3 pnt1 = waypoints[w - 1];
                        Vector3 pnt2 = waypoints[w];
                        if (pnt1.x > pnt2.x) { Vector3 pnt3 = pnt1; pnt1 = pnt2; pnt2 = pnt3; }

                        // Create mini copies
                        float distance = Vector3.Distance(pnt1, pnt2);
                        Vector3 delta = (pnt2 - pnt1) / distance;
                        distance = distance + 0.25f;
                        for (int m = 0; m < distance; m++)
                        {
                            Debug.Log("Place GO at " + (pnt1 + (delta * m)));
                            GameObject copy = GameObject.Instantiate(assetBundle.LoadAsset<GameObject>(_replicatedContent));
                            copy.transform.position = (pnt1 + (delta * m));
                            copy.transform.SetParent(_replicatedAsset.transform);
                            Vector3 dir = (pnt2 - pnt1);
                            float angle = Vector3.Angle(transform.forward, dir) + 90.0f;
                            Debug.Log("Set Angle to " + angle);
                            copy.transform.localEulerAngles = new Vector3(0f, angle, 0f);
                            // Debug.Log("Base Rotation " + _replicatedAsset.BaseLoader.transform.eulerAngles);
                            // copy.transform.localEulerAngles = new Vector3(0f, angle - _replicatedAsset.BaseLoader.transform.eulerAngles.y, 0f);
                            // Debug.Log("Copy Rotation " + copy.transform.localEulerAngles);
                        }
                    }
                }
                catch (Exception e) { Debug.Log("Exception (Stage2) Placing Mini Copies: " + e); }
                assetBundle.Unload(false);
            }
            catch (Exception x) { Debug.Log("Exception (State1) Placing Mini Copies: " + x); }
        }

        /// <summary>
        /// Method to properly evaluate shortcut keys. 
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public bool StrictKeyCheck(KeyboardShortcut check)
        {
            if (!check.IsUp()) { return false; }
            foreach (KeyCode modifier in new KeyCode[] { KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftShift, KeyCode.RightShift })
            {
                if (Input.GetKey(modifier) != check.Modifiers.Contains(modifier)) { return false; }
            }
            return true;
        }

        /// <summary>
        /// Class to hold replication data including the content name to be replicated and the waypoints along which the replication should occur
        /// </summary>
        public class ReplicationData
        {
            public string content { get; set; }
            public string[] waypoints { get; set; }

            /// <summary>
            /// Method to store Vector3 waypoints as strings. Needed for JSON serialization (Vector3 cannot be normally serialized)
            /// </summary>
            /// <param name="waypoints">Vector3 array of waypoints to be stored</param>
            public void PutWaypoints(Vector3[] waypoints)
            {
                List<string> strPnts = new List<string>();
                foreach(Vector3 pnt in waypoints)
                {
                    strPnts.Add(pnt.x + "," + pnt.y + "," + pnt.z);
                }
                this.waypoints = strPnts.ToArray();
            }

            /// <summary>
            /// Method to get strings waypoints as Vector3 waypoints. Needed for JSON serialization (Vector3 cannot be normally serialized)
            /// </summary>
            /// <returns></returns>
            public Vector3[] GetWaypoints()
            {
                List<Vector3> v3Pnts = new List<Vector3>();
                foreach (string pnt in this.waypoints)
                {
                    string[] parts = pnt.Split(',');
                    v3Pnts.Add(new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2])));
                }
                return v3Pnts.ToArray();
            }
        }

        /// <summary>
        /// Enum for plugin states
        /// </summary>
        public enum ReplicationState
        {
            idle = 0,
            rulerEventStarted,
            rulerEventComplete,
            replicationAssetPrompt,
            creatingBase,
            requestingCopies,
            creatingCopies,
        }
    }
}