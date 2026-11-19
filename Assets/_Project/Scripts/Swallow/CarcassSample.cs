using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Swallow
{
    /// <summary>What a carcass holds for Giải Mã: the dead creature's genes, its body mass and name.</summary>
    public class CarcassSample : MonoBehaviour
    {
        public List<Gene> Genes = new List<Gene>();
        public float Mass = 15f;
        public string Label = "Xác";
        /// <summary>Best quality the sample can still have (a spat-out item keeps the quality it had).</summary>
        public float QualityCap = 1f;
    }
}
