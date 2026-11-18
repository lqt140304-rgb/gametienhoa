using System;
using System.Collections.Generic;

namespace Evolve.World
{
    /// <summary>
    /// Which files to download for a scanned asset, from the reply of Poly Haven's <c>/files/{id}</c>: the model (FBX preferred, glTF otherwise)
    /// at the manifest's resolution, plus every texture the model includes, laid out as the model expects (textures/... next to it).
    /// </summary>
    public static class FloraDownload
    {
        public const string Root = "Assets/_Project/ThirdParty/Scanned/";

        public static string FolderFor(FloraAsset a) => Root + a.id;

        /// <summary>The model's entry ("url", "include"...) or null when the asset has no FBX or glTF at that resolution.</summary>
        static Dictionary<string, object> ModelEntry(string filesJson, FloraAsset a, out string format)
        {
            format = null;
            if (!(TinyJson.Parse(filesJson) is Dictionary<string, object> files)) return null;
            foreach (var f in new[] { "fbx", "gltf" })
            {
                if (!files.TryGetValue(f, out var byRes) || !(byRes is Dictionary<string, object> res)) continue;
                if (!res.TryGetValue(a.resolution ?? "1k", out var byFmt) || !(byFmt is Dictionary<string, object> fmt)) continue;
                if (fmt.TryGetValue(f, out var e) && e is Dictionary<string, object> entry && entry.ContainsKey("url")) { format = f; return entry; }
            }
            return null;
        }

        /// <summary>URL of the model file, and the file name it is saved under; null when there is no FBX or glTF at that resolution.</summary>
        public static string PickUrl(string filesJson, FloraAsset a, out string fileName)
        {
            fileName = null;
            var entry = ModelEntry(filesJson, a, out var format);
            if (entry == null) return null;
            string url = (string)entry["url"];
            fileName = url.Substring(url.LastIndexOf('/') + 1);
            return url;
        }

        /// <summary>Every (url, project path) still to download for this asset: the model and its included textures; files that already exist are skipped.</summary>
        public static List<(string url, string path)> Plan(string filesJson, FloraAsset a, Func<string, bool> exists)
        {
            var result = new List<(string, string)>();
            var entry = ModelEntry(filesJson, a, out _);
            if (entry == null) return result;
            string folder = FolderFor(a) + "/";
            string url = (string)entry["url"];
            string model = folder + url.Substring(url.LastIndexOf('/') + 1);
            if (exists == null || !exists(model)) result.Add((url, model));
            if (entry.TryGetValue("include", out var inc) && inc is Dictionary<string, object> include)
                foreach (var kv in include)
                    if (kv.Value is Dictionary<string, object> file && file.TryGetValue("url", out var u))
                    {
                        string path = folder + kv.Key;
                        if (exists == null || !exists(path)) result.Add(((string)u, path));
                    }
            return result;
        }
    }
}
