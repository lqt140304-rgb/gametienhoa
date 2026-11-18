using System;
using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>One choice of flaw(s) offered with a core: one flaw, or a pair for heavy cores.</summary>
    [Serializable]
    public struct FlawOption
    {
        public string[] FlawIds;
    }

    /// <summary>A core (Lõi): a passive rule of the body that runs by itself. Three flaw options go with it.</summary>
    [CreateAssetMenu(menuName = "Evolve/Core", fileName = "Core")]
    public class CoreSO : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [Tooltip("One short line for the card face.")] public string Summary;
        [TextArea] public string Detail;
        public Color OrbColor = Color.cyan;
        [Tooltip("Exactly three options; a heavy core offers pairs of two flaws.")] public FlawOption[] Options = new FlawOption[3];
        public bool Heavy;
        [Tooltip("False while the effect is not coded yet: the card is shown but nothing happens.")]
        public bool Implemented;
    }
}
