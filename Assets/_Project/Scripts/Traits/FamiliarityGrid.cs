using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>
    /// Nhân Quen Đất: the ground is cut into 4 m cells; every time the player enters a cell its familiarity grows, and it fades
    /// again when they stay away. A familiar cell speeds the player up by at most 15%. Standing still in a cell never adds anything.
    /// </summary>
    public sealed class FamiliarityGrid
    {
        struct Cell { public float Score; public float Time; }

        readonly Dictionary<(int, int), Cell> cells = new Dictionary<(int, int), Cell>();
        (int, int)? last;

        static (int, int) CellOf(Vector2 p) =>
            ((int)Math.Floor(p.x / TraitTuning.QuenDatCell), (int)Math.Floor(p.y / TraitTuning.QuenDatCell));

        float Current(Cell c, float time) => Math.Max(0f, c.Score - Math.Max(0f, time - c.Time) * TraitTuning.QuenDatDecayPerSecond);

        /// <summary>Call every frame with the player's ground position; only entering a different cell than the last one counts.</summary>
        public void Visit(Vector2 worldXZ, float time)
        {
            var key = CellOf(worldXZ);
            if (last == key) return;
            last = key;
            cells.TryGetValue(key, out var cell);
            float now = Current(cell, time);
            cells[key] = new Cell { Score = Math.Min(1f, now + TraitTuning.QuenDatGain), Time = time };
        }

        /// <summary>0 (never walked here, or forgotten) .. 1 (walked here many times recently).</summary>
        public float Familiarity(Vector2 worldXZ, float time) =>
            cells.TryGetValue(CellOf(worldXZ), out var cell) ? Math.Min(1f, Current(cell, time)) : 0f;

        /// <summary>1 .. 1 + QuenDatMaxBonus.</summary>
        public float SpeedMultiplier(Vector2 worldXZ, float time) => 1f + TraitTuning.QuenDatMaxBonus * Familiarity(worldXZ, time);

        public void Clear() { cells.Clear(); last = null; }
    }
}
