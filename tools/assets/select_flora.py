"""Lists Poly Haven model candidates for the scanned flora and the download size of each at 1k/2k (FBX + its textures).

Run (needs network to api.polyhaven.com):  python3 tools/assets/select_flora.py [id ...]
Without ids it prints every nature/plants/rocks model; with ids it prints their sizes. The chosen ids go in flora_manifest.json.
"""
import json
import sys
import urllib.request

API = "https://api.polyhaven.com"


def get(path):
    with urllib.request.urlopen(API + path, timeout=30) as r:
        return json.load(r)


def size(asset_id):
    files = get("/files/" + asset_id).get("fbx", {})
    out = {}
    for res in ("1k", "2k"):
        e = files.get(res, {}).get("fbx")
        if e:
            out[res] = round((e["size"] + sum(v["size"] for v in e.get("include", {}).values())) / 1e6, 1)
    return out


def main():
    ids = sys.argv[1:]
    if not ids:
        for k, v in sorted(get("/assets?t=models").items()):
            cats = v.get("categories", [])
            if any(c in cats for c in ("nature", "plants", "rocks", "trees", "ground cover")):
                print(k, "|", v.get("name"), "|", ",".join(cats))
        return
    for i in ids:
        print(i, size(i))


if __name__ == "__main__":
    main()
