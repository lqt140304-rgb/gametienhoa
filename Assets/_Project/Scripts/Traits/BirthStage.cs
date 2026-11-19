using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Evolve.Traits
{
    /// <summary>
    /// The separate stage of the birth scene (spec 2026-11-20-birth-scene-system-collapse-design 5.1): a camera, a post-processing Volume and a world-space
    /// canvas far away from the world (y = 20000), so the player sees only the scene, never the slime or the cave. The camera clears to a solid
    /// colour and sees nothing beyond 300 m. The Volume is on its own layer and only this camera reads that layer, so the game's brightness and
    /// sight volumes do not touch it. Create returns null if anything goes wrong (the scene is then skipped, never left black).
    /// </summary>
    internal sealed class BirthStage
    {
        public const int PostLayer = 31;
        const float Distance = 10f, FieldOfView = 40f;

        public GameObject Root;
        public Camera Cam;
        /// <summary>The camera renders into this; the scene draws it over the whole screen with IMGUI so it also covers the game's IMGUI HUD.</summary>
        public RenderTexture Rt;
        public RectTransform Canvas;
        public Vector2 CanvasSize;
        public Bloom Bloom;
        public ChromaticAberration Chroma;
        public LensDistortion Lens;
        public FilmGrain Grain;
        public Vignette Vig;
        VolumeProfile profile;

        public static BirthStage Create()
        {
            var stage = new BirthStage();
            try
            {
                stage.Build();
                return stage;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Cảnh ra đời: không dựng được sân khấu, bỏ qua cảnh. " + e.Message);
                stage.Destroy();
                return null;
            }
        }

        void Build()
        {
            Root = new GameObject("Birth Stage");
            Root.transform.position = new Vector3(0f, 20000f, 0f);

            var camGo = new GameObject("Birth Camera");
            camGo.transform.SetParent(Root.transform, false);
            Cam = camGo.AddComponent<Camera>();
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = BirthUiKit.Deep;
            Cam.fieldOfView = FieldOfView;
            Cam.nearClipPlane = 0.1f;
            Cam.farClipPlane = 300f;
            Cam.depth = 100f;                         // above every other camera
            Rt = new RenderTexture(Mathf.Max(16, Screen.width), Mathf.Max(16, Screen.height), 24, RenderTextureFormat.ARGB32) { name = "Birth Stage RT" };
            Rt.Create();
            Cam.targetTexture = Rt;
            var data = Cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.volumeLayerMask = 1 << PostLayer;    // only the scene's own Volume
            data.volumeTrigger = Cam.transform;

            // the canvas fills the camera's view at Distance
            float height = 2f * Distance * Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            CanvasSize = new Vector2(1080f * aspect, 1080f);
            var canvasGo = new GameObject("Birth Canvas", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(Root.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Cam;
            canvas.overrideSorting = true; canvas.sortingOrder = 100;   // the UI is above the slime sprites, so the fade-out covers everything
            Canvas = canvasGo.GetComponent<RectTransform>();
            Canvas.sizeDelta = CanvasSize;
            Canvas.localPosition = new Vector3(0f, 0f, Distance);
            Canvas.localScale = Vector3.one * (height / 1080f);

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            Bloom = profile.Add<Bloom>(false);
            Bloom.threshold.Override(0.7f); Bloom.intensity.Override(0.8f); Bloom.scatter.Override(0.7f);
            Chroma = profile.Add<ChromaticAberration>(false);
            Chroma.intensity.Override(0.1f);
            Lens = profile.Add<LensDistortion>(false);
            Lens.intensity.Override(0f);
            Grain = profile.Add<FilmGrain>(false);
            Grain.type.Override(FilmGrainLookup.Medium3); Grain.intensity.Override(0.2f); Grain.response.Override(0.8f);
            Vig = profile.Add<Vignette>(false);
            Vig.intensity.Override(0.3f); Vig.smoothness.Override(0.5f);

            var volGo = new GameObject("Birth Volume");
            volGo.transform.SetParent(Root.transform, false);
            volGo.layer = PostLayer;
            var volume = volGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1000f;
            volume.sharedProfile = profile;
        }

        public void Destroy()
        {
            if (Cam != null) Cam.targetTexture = null;
            if (Root != null) Object.Destroy(Root);
            if (profile != null) Object.Destroy(profile);
            if (Rt != null) { Rt.Release(); Object.Destroy(Rt); }
            Root = null; Cam = null; Canvas = null; profile = null; Rt = null;
        }
    }
}
