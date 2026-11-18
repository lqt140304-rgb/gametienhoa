using System.IO;
using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Installs the Blender rocks (tools/blender/out/Rock_N.fbx.b64, text copies of the FBX files because Git LFS is blocked
    /// for the agent's pushes) into Assets/_Project/Models/Underroot. Menu: Evolve -> Install Blender Rocks.
    /// "Dress Lake: Props" calls it automatically when the FBX files are missing.
    /// </summary>
    public static class PropInstaller
    {
        const string Folder = "Assets/_Project/Models/Underroot";
        const string SourceFolder = "tools/blender/out";
        const int Count = 6;
        static readonly string[] LandmarkNames =
        {
            "Log_Hollow", "Mushroom_Giant_0", "Mushroom_Giant_1", "Mushroom_Giant_2",
            "Mushroom_Small_0", "Mushroom_Small_1", "Mushroom_Small_2", "Mushroom_Small_3", "RootArch",
            "Cliff_0", "Cliff_1", "Cliff_2", "Cliff_3", "HangRoot_0", "HangRoot_1", "HangRoot_2", "HangRoot_3",
            "Rib_Straight_0", "Rib_Straight_1", "Rib_Straight_2", "Rib_Bend_0", "Rib_Bend_1", "Rib_Bend_2", "Rib_Fork_0", "Rib_Fork_1",
            "Rib_Tip_0", "Rib_Tip_1", "Rib_Foot_0", "Rib_Foot_1",
            "VaultRoot_0", "VaultRoot_1", "VaultRoot_2", "VaultRoot_3", "VaultCurtain_0", "VaultCurtain_1",
            "VaultPod_0", "VaultPod_1", "VaultPod_2", "VaultStalactite_0", "VaultStalactite_1", "VaultStalactite_2",
            "DragonSkull_0", "DragonSkull_1", "DragonRibcage_0",
            "LumaShroom_0", "LumaShroom_1", "LumaShroom_2", "CrystalCluster_0", "CrystalCluster_1", "CrystalCluster_2",
            "BulbPlant_0", "BulbPlant_1", "RootStub_0", "RootStub_1"
        };

        [MenuItem("Evolve/Install Blender Rocks")]
        public static void InstallMenu()
        {
            int n = EnsureRocks() + EnsureLandmarks();
            Debug.Log(n > 0 ? $"Đã cài {n} file FBX (đá, gỗ, nấm, cung rễ) vào {Folder}." : "Vật thể Blender đã có sẵn (hoặc thiếu file nguồn trong tools/blender/out).");
        }

        /// <summary>Writes any missing Rock_N.fbx from its .b64 text copy. Returns how many files were written.</summary>
        public static int EnsureRocks()
        {
            var names = new string[Count];
            for (int i = 0; i < Count; i++) names[i] = "Rock_" + i;
            return Ensure(names);
        }

        /// <summary>Writes any missing landmark FBX (log, mushrooms, root arch) from its .b64 text copy.</summary>
        public static int EnsureLandmarks() => Ensure(LandmarkNames);

        public static bool LandmarksPresent()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            foreach (var n in LandmarkNames)
                if (!File.Exists(Path.Combine(projectRoot, Folder, n + ".fbx"))) return false;
            return true;
        }

        [MenuItem("Evolve/Reinstall Blender Props (overwrite)")]
        public static void ReinstallMenu()
        {
            var all = new System.Collections.Generic.List<string>(LandmarkNames);
            for (int i = 0; i < Count; i++) all.Add("Rock_" + i);
            int n = Ensure(all.ToArray(), true);
            Debug.Log($"Đã ghi đè {n} file FBX vật thể Blender từ tools/blender/out. Chạy lại Dress Lake: Props và Landmark Props.");
        }

        static int Ensure(string[] names, bool overwrite = false)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            Directory.CreateDirectory(Path.Combine(projectRoot, Folder));
            int written = 0;
            foreach (var name in names)
            {
                string target = Path.Combine(projectRoot, Folder, name + ".fbx");
                if (File.Exists(target) && !overwrite) continue;
                string source = Path.Combine(projectRoot, SourceFolder, name + ".fbx.b64");
                if (!File.Exists(source)) { Debug.LogWarning("Thiếu file nguồn " + source); continue; }
                File.WriteAllBytes(target, System.Convert.FromBase64String(File.ReadAllText(source).Trim()));
                written++;
            }
            if (written > 0) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return written;
        }
    }
}
