using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>
    /// Đại diện một khoang thực tế trong scene (đặt cách xa các khoang khác).
    /// Được MazeWorldManager bật/tắt theo khoang hiện tại và các khoang kề.
    /// </summary>
    public class ChamberInstance : MonoBehaviour
    {
        [SerializeField] int chamberId;
        [SerializeField] string chamberName;
        [SerializeField] ChamberDepth depth;
        [SerializeField] ChamberKind kind;
        [SerializeField] bool keepAlwaysActive;
        [SerializeField] Transform arrivalPoint;
        [SerializeField] List<TunnelPortal> portals = new List<TunnelPortal>();

        public int ChamberId => chamberId;
        public string ChamberName => chamberName;
        public ChamberDepth Depth => depth;
        public ChamberKind Kind => kind;
        public bool KeepAlwaysActive => keepAlwaysActive;
        public Transform ArrivalPoint => arrivalPoint != null ? arrivalPoint : transform;
        public IReadOnlyList<TunnelPortal> Portals => portals;

        public void Configure(int id, string name, ChamberDepth chamberDepth, ChamberKind chamberKind, bool alwaysActive, Transform arrival)
        {
            chamberId = id;
            chamberName = name;
            depth = chamberDepth;
            kind = chamberKind;
            keepAlwaysActive = alwaysActive;
            arrivalPoint = arrival;
        }

        public void RegisterPortal(TunnelPortal portal)
        {
            if (portal != null && !portals.Contains(portal))
                portals.Add(portal);
        }

        public TunnelPortal FindPortalTo(int targetChamberId)
        {
            for (int i = 0; i < portals.Count; i++)
            {
                if (portals[i] != null && portals[i].TargetChamberId == targetChamberId)
                    return portals[i];
            }
            return null;
        }
    }
}
