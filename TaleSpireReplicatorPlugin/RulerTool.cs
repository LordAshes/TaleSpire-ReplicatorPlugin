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
        private const string lineIndicatorName = "LineIndicator(Clone)";
        private List<Vector3> waypoints = new List<Vector3>();
        private Action<Vector3[]> _callback = null;

        public void SubscribeRulerEvents(Action<Vector3[]> callback)
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
                        foreach (Transform wayppoint in child.transform.Children())
                        {
                            recorded.Add(wayppoint.position);
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
            _callback(waypoints.ToArray());
        }
    }
}
