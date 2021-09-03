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
        public const string Version = "1.2.1.0";

        // Loose Dependencies
        public const string CMP = "org.lordashes.plugins.custommini.effect";
        public NGuid baseMiniNGuid = NGuid.Empty;

        // Content directory
        private string dir = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.LastIndexOf("/")) + "/TaleSpire_CustomData/";

        private ConfigEntry<KeyboardShortcut> trigger;

        private ReplicationState pluginState = ReplicationState.idle;

        private string _replicatedContent = null;
        private CreatureBoardAsset _replicatedAsset = null;
        private CreatureGuid _replicatedBaseCreatureId = CreatureGuid.Empty;
        private ReplicatorType _rulerType = ReplicatorType.Idle;
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

            switch (pluginState)
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
                        sequencer = 1 + 1;
                        pluginState = ReplicationState.creatingBase;
                    }
                    break;
                case ReplicationState.creatingBase:
                    if (sequencer == 1)
                    {
                        Debug.Log("Replicator Plugin: Creating Line Replicator Base");
                        CreateMiniBase(_waypoints[0]);
                        sequencer = 20 + 1;
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
                        switch (_rulerType)
                        {
                            case ReplicatorType.Line:
                                CreateCopyMinisLine();
                                break;
                            case ReplicatorType.Circle:
                                CreateCopyMinisCircleArea();
                                break;
                            default:
                                Debug.Log("ReplicatorPlugin: ERROR - No rulerType found...");
                                break;
                        }
                        _rulerType = ReplicatorType.Idle;
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
                if (_replicatedContent == null)
                {
                    GUI.Label(new Rect(10, 30, 1900, 120), "Replicator ["+_rulerType.ToString()+" Mode] Active: (Content Name To Be Prompted)", gs);
                }
                else if (_replicatedContent == null)
                {
                    GUI.Label(new Rect(10, 30, 1900, 120), "Replicator ["+_rulerType.ToString() + " Mode] Active: (Content Name To Be Prompted)", gs);
                }
                else
                {
                    GUI.Label(new Rect(10, 30, 1900, 120), "Replicator ["+_rulerType.ToString() + " Mode] Active: (Replicating '" + _replicatedContent + "')", gs);
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
            Debug.Log("Remote Replication Request: Asset " + _replicatedAsset.Creature.CreatureId);
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
        /// <param name="rulerType">String indicating the ruler type</param>
        private void RulerEvent(Vector3[] waypoints, ReplicatorType rulerType)
        {
            _waypoints = waypoints;
            _rulerType = rulerType;
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
            baseMiniNGuid = FindAsset(Config.Bind("Settings", "Base Mini Name", "Monkey (Snub-nosed)").Value);

            Debug.Log("Replicator Plugin: Base '"+ Config.Bind("Settings", "Base Mini Name", "Monkey (Snub-nosed)").Value + "' NGuid = " + baseMiniNGuid.ToString());

            CreatureDataV1 creatureDataV1 = new CreatureDataV1(baseMiniNGuid);
            creatureDataV1.CreatureId = new CreatureGuid(new Bounce.Unmanaged.NGuid(System.Guid.NewGuid()));

            CreatureDataV2 creatureDataV2 = new CreatureDataV2(creatureDataV1);
            creatureDataV2.CreatureId = creatureDataV1.CreatureId;

            /*
            creatureDataV1.ExplicitlyHidden = true;
            creatureDataV2.ExplicitlyHidden = true;
            creatureDataV1.Flying = false;
            creatureDataV2.Flying = false;
            */

            CreatureManager.CreateAndAddNewCreature(creatureDataV2, position, Quaternion.Euler(0, 0, 0), false, true);

            _replicatedBaseCreatureId = creatureDataV1.CreatureId;
        }

        /// <summary>
        /// Method to request (via StatMessaging) replication of the specified contents along the specified waypoints
        /// </summary>
        private void RequestingCopyMinis()
        {
            Debug.Log("Replicator Plugin:  Find Line Replicator Base...");
            CreatureBoardAsset asset = null;
            CreaturePresenter.TryGetAsset(_replicatedBaseCreatureId, out asset);
            if (asset == null) { Debug.LogWarning("Unable to locate Line Replicator base"); pluginState = ReplicationState.idle; return; }
            asset.name = "Effect:" + asset.Creature.CreatureId + ".0";
            CreatureManager.SetCreatureName(asset.Creature.CreatureId,_rulerType+" Replication of "+_replicatedContent+"<size=0>{}");
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
        private void CreateCopyMinisLine()
        {
            try
            {
                Debug.Log("Replicator Plugin:   Loading assetBundle '" + _replicatedContent + "'...");
                Debug.Log("Replicator Plugin:   Replicating Line...");
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
                            //Debug.Log("Place GO at " + (pnt1 + (delta * m)));
                            GameObject copy = GameObject.Instantiate(assetBundle.LoadAsset<GameObject>(_replicatedContent));
                            copy.name = "Effect:" + _replicatedAsset.Creature.CreatureId + "." + w + "." + m;
                            copy.transform.position = (pnt1 + (delta * m));
                            copy.transform.SetParent(_replicatedAsset.transform);
                            Vector3 dir = (pnt2 - pnt1);
                            float angle = Vector3.Angle(transform.forward, dir) + 90.0f;
                            //Debug.Log("Set Angle to " + angle);
                            copy.transform.localEulerAngles = new Vector3(0f, angle, 0f);
                        }
                    }
                }
                catch (Exception e) { Debug.Log("Exception (Stage2) Placing Mini Copies: " + e); }
                assetBundle.Unload(false);
            }
            catch (Exception x) { Debug.Log("Exception (State1) Placing Mini Copies: " + x); }
        }

        /// <summary>
        /// Method used to process a replication request by copying the specified content along the specified waypoints sphere
        /// </summary>
        private void CreateCopyMinisCircleArea()
        {
            try
            {
                Debug.Log("Replicator Plugin:   Loading assetBundle '" + _replicatedContent + "'...");
                Debug.Log("Replicator Plugin:   Replicating Sphere...");
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
                            Vector3 previousRadialPoint = Vector3.zero;
                            for (int i = 0; i < 360; i += 1)
                            {
                                if (previousRadialPoint == Vector3.zero)
                                {
                                    //Debug.Log("Place GOs at " + (pnt1 + (delta * m)));
                                    GameObject copy = GameObject.Instantiate(assetBundle.LoadAsset<GameObject>(_replicatedContent));
                                    copy.name = "Effect:" + _replicatedAsset.Creature.CreatureId + "." + w + "." + m + "." + i;
                                    copy.transform.position = (pnt1 + (delta * m));
                                    copy.transform.RotateAround(_replicatedAsset.transform.position, Vector3.up, i);
                                    copy.transform.SetParent(_replicatedAsset.transform);
                                    Vector3 dir = (copy.transform.position - _replicatedAsset.transform.position);
                                    float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

                                    //Debug.Log("Set Angle to " + angle);
                                    copy.transform.localEulerAngles = new Vector3(0f, angle, 0f);
                                    previousRadialPoint = copy.transform.position;
                                }
                                else
                                {
                                    GameObject tempGO = new GameObject();
                                    tempGO.transform.position = (pnt1 + (delta * m));
                                    tempGO.transform.RotateAround(_replicatedAsset.transform.position, Vector3.up, i);

                                    if (Vector3.Distance(tempGO.transform.position, previousRadialPoint) >= 1f)
                                    {
                                        //Debug.Log("Place GOs at " + (pnt1 + (delta * m)));
                                        GameObject copy = GameObject.Instantiate(assetBundle.LoadAsset<GameObject>(_replicatedContent));
                                        copy.name = "Effect:" + _replicatedAsset.Creature.CreatureId + "." + w + "." + m + "." + i;
                                        copy.transform.position = tempGO.transform.position;
                                        copy.transform.SetParent(_replicatedAsset.transform);
                                        Vector3 dir = (copy.transform.position - _replicatedAsset.transform.position);
                                        float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

                                        //Debug.Log("Set Angle to " + angle);
                                        copy.transform.localEulerAngles = new Vector3(0f, angle, 0f);
                                        previousRadialPoint = copy.transform.position;
                                    }
                                }
                            }
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
        /// Finds the Nguid of an asset based on a content name
        /// </summary>
        /// <param name="contentName"></param>
        /// <returns></returns>
        private static NGuid FindAsset(string contentName)
        {
            foreach ((AssetDb.DbEntry.EntryKind, List<AssetDb.DbGroup>) kind in AssetDb.GetAllGroups())
            {
                foreach (AssetDb.DbGroup group in kind.Item2)
                {
                    foreach (AssetDb.DbEntry item in group.Entries)
                    {
                        if (item.Name == contentName) { return item.Id; }
                    }
                }
            }
            return NGuid.Empty;
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
                foreach (Vector3 pnt in waypoints)
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