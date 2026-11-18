namespace Evolve.World
{
    /// <summary>Naming rules for props exported from Blender: "Rock_0", "Rock_0_COL" (collider), "Rock_0_LOD1".</summary>
    public static class PropNaming
    {
        const string ColliderSuffix = "_COL";
        const string LodMarker = "_LOD";

        public static bool IsCollider(string name) => !string.IsNullOrEmpty(name) && name.EndsWith(ColliderSuffix);

        public static bool IsLod(string name, out int level)
        {
            level = 0;
            if (string.IsNullOrEmpty(name)) return false;
            int i = name.LastIndexOf(LodMarker);
            if (i < 0) return false;
            return int.TryParse(name.Substring(i + LodMarker.Length), out level) && level > 0;
        }

        public static string BaseName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name ?? "";
            if (IsCollider(name)) return name.Substring(0, name.Length - ColliderSuffix.Length);
            if (IsLod(name, out _)) return name.Substring(0, name.LastIndexOf(LodMarker));
            return name;
        }
    }
}
