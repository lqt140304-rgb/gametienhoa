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
        [SerializeField] int terrainSeed;
        [SerializeField] float chamberRadius = MazeTerrainMath.DefaultChamberRadius;
        [SerializeField] bool hasHighLedge = true;
        [SerializeField] List<float> tunnelAnglesDeg = new List<float>();

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

        public void ConfigureTerrain(int seed, float radius, bool highLedge, List<float> tunnelAngles)
        {
            terrainSeed = seed;
            chamberRadius = radius;
            hasHighLedge = highLedge;
            tunnelAnglesDeg = tunnelAngles ?? new List<float>();
        }

        /// <summary>Tính độ cao mặt đất của khoang tại tọa độ thế giới (trả về null nếu nằm ngoài khoang này).</summary>
        public float? SampleGroundHeight(float worldX, float worldZ)
        {
            if (kind == ChamberKind.BirthCavern) return null;
            float lx = worldX - transform.position.x;
            float lz = worldZ - transform.position.z;
            if (lx * lx + lz * lz > (chamberRadius + 16f) * (chamberRadius + 16f)) return null;
            return transform.position.y + MazeTerrainMath.GroundHeightAt(lx, lz, terrainSeed, chamberRadius, tunnelAnglesDeg, hasHighLedge);
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
