using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LordAshes
{
    public partial class ReplicatorPlugin
    {
        public enum ReplicatorType
        {
            Idle = 0,
            Line = 1,
            Circle = 2 
        }

        private const string photonRulerName = "PhotonRuler(Clone)";
        public Dictionary<string, ReplicatorType> replicatorTypeNames = new Dictionary<string, ReplicatorType>();
        private List<Vector3> waypoints = new List<Vector3>();
        private Action<Vector3[],ReplicatorType> _callback = null;

        public void SubscribeRulerEvents(Action<Vector3[],ReplicatorType> callback)
        {
            replicatorTypeNames.Clear();
            replicatorTypeNames.Add("LineIndicator(Clone)", ReplicatorType.Line);
            replicatorTypeNames.Add("SphereIndicator(Clone)", ReplicatorType.Circle);

            Debug.Log("Subscribing To RulerEvents");
            RulerBoardTool.OnCloseRulers += RulerBoardTool_OnCloseRulers;
            _callback = callback;
        }

        public void UpdateRulerEvents()
        {
            List<Vector3> recorded = new List<Vector3>();
            GameObject photonRuler = GameObject.Find(photonRulerName);
            if (photonRuler != null)
            {
                foreach (Transform child in photonRuler.transform.Children())
                {
                    foreach (KeyValuePair<string,ReplicatorType> repType in replicatorTypeNames)
                    {
                        if (child.name == repType.Key)
                        {
                            foreach (Transform waypoint in child.transform.Children())
                            {
                                recorded.Add(waypoint.position);
                            }
                            _rulerType = repType.Value;
                        }
                    }
                }
            }
            waypoints = recorded;
        }

        private void RulerBoardTool_OnCloseRulers()
        {
            Debug.Log("Ruler Event Complete");
            RulerBoardTool.OnCloseRulers -= RulerBoardTool_OnCloseRulers;
            _callback(waypoints.ToArray(), _rulerType);
        }
    }
}
