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

        [MenuItem("Evolve/Install Blender Rocks")]
        public static void InstallMenu()
        {
            int n = EnsureRocks();
            Debug.Log(n > 0 ? $"Đã cài {n} file FBX đá vào {Folder}." : "Đá Blender đã có sẵn (hoặc thiếu file nguồn trong tools/blender/out).");
        }

        /// <summary>Writes any missing Rock_N.fbx from its .b64 text copy. Returns how many files were written.</summary>
        public static int EnsureRocks()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            Directory.CreateDirectory(Path.Combine(projectRoot, Folder));
            int written = 0;
            for (int i = 0; i < Count; i++)
            {
                string target = Path.Combine(projectRoot, Folder, $"Rock_{i}.fbx");
                if (File.Exists(target)) continue;
                string source = Path.Combine(projectRoot, SourceFolder, $"Rock_{i}.fbx.b64");
                if (!File.Exists(source)) { Debug.LogWarning("Thiếu file nguồn " + source); continue; }
                File.WriteAllBytes(target, System.Convert.FromBase64String(File.ReadAllText(source).Trim()));
                written++;
            }
            if (written > 0) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return written;
        }
    }
}
