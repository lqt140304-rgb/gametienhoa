using System.Collections.Generic;
using System.IO;
using Evolve.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve -> Install Scanned Assets: downloads the scanned plants, rocks and wood listed in tools/assets/flora_manifest.json from Poly Haven (CC0)
    /// into Assets/_Project/ThirdParty/Scanned (not in git, about 350 MB), then builds each one's semi-real material and prefab. Rerunnable: files
    /// already there are not downloaded again; an asset that fails is reported and the rest carry on.
    /// </summary>
    public static class ScannedAssetInstaller
    {
        const string ManifestPath = "tools/assets/flora_manifest.json";
        const string Api = "https://api.polyhaven.com/files/";

        [MenuItem("Evolve/Install Scanned Assets")]
        public static void Install()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            string manifestFile = Path.Combine(root, ManifestPath);
            if (!File.Exists(manifestFile)) { Debug.LogWarning("Không thấy " + ManifestPath + "."); return; }
            var manifest = FloraManifest.Parse(File.ReadAllText(manifestFile));
            if (manifest.assets.Length == 0) { Debug.LogWarning("Manifest rỗng hoặc hỏng: " + ManifestPath); return; }

            var failed = new List<string>();
            long bytes = 0;
            int done = 0;
            try
            {
                for (int i = 0; i < manifest.assets.Length; i++)
                {
                    var a = manifest.assets[i];
                    if (EditorUtility.DisplayCancelableProgressBar("Tải tài sản quét (Poly Haven)", $"{a.id} ({i + 1}/{manifest.assets.Length})", i / (float)manifest.assets.Length)) break;
                    string json = Get(Api + a.id, out string error);
                    if (json == null) { failed.Add($"{a.id}: {error}"); continue; }
                    var plan = FloraDownload.Plan(json, a, p => File.Exists(Path.Combine(root, p)));
                    if (FloraDownload.PickUrl(json, a, out _) == null) { failed.Add($"{a.id}: không có FBX/glTF {a.resolution}"); continue; }
                    bool ok = true;
                    foreach (var (url, path) in plan)
                    {
                        string target = Path.Combine(root, path);
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        long got = Download(url, target, out string err);
                        if (got < 0) { failed.Add($"{a.id}: {Path.GetFileName(path)}: {err}"); ok = false; break; }
                        bytes += got;
                    }
                    if (ok) done++;
                }
            }
            finally { EditorUtility.ClearProgressBar(); }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            int prefabs = 0;
            foreach (var a in manifest.assets)
            {
                try { if (ScannedMaterials.Build(a) != null) prefabs++; }
                catch (System.Exception e) { failed.Add($"{a.id}: dựng vật liệu lỗi: {e.Message}"); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Tài sản quét: {done}/{manifest.assets.Length} đã đủ file, tải thêm {bytes / 1e6:0.#} MB, {prefabs} prefab bán thực trong {FloraDownload.Root}.");
            if (failed.Count > 0) Debug.LogWarning("Tài sản lỗi (chạy lại menu để thử lại):\n" + string.Join("\n", failed));
        }

        static string Get(string url, out string error)
        {
            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = 60;
                var op = req.SendWebRequest();
                while (!op.isDone) System.Threading.Thread.Sleep(20);
                error = req.result == UnityWebRequest.Result.Success ? null : req.error;
                return error == null ? req.downloadHandler.text : null;
            }
        }

        /// <summary>Bytes written, or -1 on failure (a half-written file is deleted so the next run tries again).</summary>
        static long Download(string url, string target, out string error)
        {
            string temp = target + ".part";
            using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET, new DownloadHandlerFile(temp), null))
            {
                req.timeout = 600;
                var op = req.SendWebRequest();
                while (!op.isDone) System.Threading.Thread.Sleep(20);
                if (req.result != UnityWebRequest.Result.Success)
                {
                    error = req.error;
                    if (File.Exists(temp)) File.Delete(temp);
                    return -1;
                }
            }
            if (File.Exists(target)) File.Delete(target);
            File.Move(temp, target);
            error = null;
            return new FileInfo(target).Length;
        }
    }
}
