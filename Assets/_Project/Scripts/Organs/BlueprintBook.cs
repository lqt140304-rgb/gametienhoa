using System.Collections.Generic;

namespace Evolve.Organs
{
    /// <summary>Organs the slime has learned (by attaching or absorbing). Kept when the organ dies or is lost.</summary>
    public class BlueprintBook
    {
        readonly HashSet<string> names = new HashSet<string>();

        public IReadOnlyCollection<string> Names => names;

        /// <summary>True if the blueprint was new.</summary>
        public bool Learn(string organName) => !string.IsNullOrEmpty(organName) && names.Add(organName);

        public bool Knows(string organName) => !string.IsNullOrEmpty(organName) && names.Contains(organName);

        public void Clear() => names.Clear();
    }
}
