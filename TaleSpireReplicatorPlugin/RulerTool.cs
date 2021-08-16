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
        private const string photonRulerName = "PhotonRuler(Clone)";
        public const string lineIndicatorName = "LineIndicator(Clone)";
        public const string sphereIndicatorName = "SphereIndicator(Clone)";
        private string rulerType = "";
        private List<Vector3> waypoints = new List<Vector3>();
        private Action<Vector3[],string> _callback = null;

        public void SubscribeRulerEvents(Action<Vector3[],string> callback)
        {
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
                    if (child.name == lineIndicatorName)
                    {
                        foreach (Transform waypoint in child.transform.Children())
                        {
                            recorded.Add(waypoint.position);
                        }
                        rulerType = lineIndicatorName;
                    }
                    else if(child.name == sphereIndicatorName)
                    {
                        foreach(Transform waypoint in child.transform.Children())
                        {
                            //Debug.Log("XYZ: " + waypoint.position.x + ", " + waypoint.position.y + ", " + waypoint.position.z);
                            recorded.Add(waypoint.position);
                        }
                        rulerType = sphereIndicatorName;
                    }
                }
            }
            waypoints = recorded;
        }

        private void RulerBoardTool_OnCloseRulers()
        {
            Debug.Log("Ruler Event Complete");
            RulerBoardTool.OnCloseRulers -= RulerBoardTool_OnCloseRulers;
            _callback(waypoints.ToArray(),rulerType);
        }
    }
}
