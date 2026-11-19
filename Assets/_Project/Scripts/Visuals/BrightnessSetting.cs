using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Evolve.Visuals
{
    /// <summary>
    /// A display setting for the player: F10 opens a small panel with a brightness slider ("Độ sáng") and an LOD switch (LodSetting), [ and ] change it a notch without the panel; it is remembered
    /// (PlayerPrefs). It adds a global post-processing volume that raises exposure and lifts the shadows (BrightnessMath), above every other volume, touching only
    /// those two settings (the vision system keeps the saturation). Created by itself when the game starts.
    /// </summary>
    public class BrightnessSetting : MonoBehaviour
    {
        const string PrefKey = "evolve.brightness";
        float brightness = BrightnessMath.Default;
        bool panel;
        Volume volume;
        ColorAdjustments color;
        LiftGammaGain lgg;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindAnyObjectByType<BrightnessSetting>() != null) return;
            var go = new GameObject("Brightness Setting");
            go.AddComponent<BrightnessSetting>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            brightness = BrightnessMath.Clamp(PlayerPrefs.GetFloat(PrefKey, BrightnessMath.Default));
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            color = profile.Add<ColorAdjustments>(false);   // false: override nothing but what is set below, so the vision system keeps the saturation (no colour without eyes)
            color.postExposure.overrideState = true;
            lgg = profile.Add<LiftGammaGain>(false);
            lgg.gamma.overrideState = true;
            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;       // above every scene volume
            volume.sharedProfile = profile;
            LodSetting.Apply(LodSetting.Saved);   // LOD is off until the player turns it back on
            Apply();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f10Key.wasPressedThisFrame) panel = !panel;
            if (kb.rightBracketKey.wasPressedThisFrame) Set(brightness + 0.05f);
            if (kb.leftBracketKey.wasPressedThisFrame) Set(brightness - 0.05f);
        }

        void Set(float b)
        {
            brightness = BrightnessMath.Clamp(b);
            PlayerPrefs.SetFloat(PrefKey, brightness);
            Apply();
        }

        void Apply()
        {
            color.postExposure.value = BrightnessMath.PostExposure(brightness);
            lgg.gamma.value = new Vector4(1f, 1f, 1f, BrightnessMath.GammaLift(brightness));
            var cam = Camera.main;
            if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }

        void OnGUI()
        {
            if (!panel) return;
            const float w = 320f, h = 130f;
            var rect = new Rect(Screen.width - w - 16f, 64f, w, h);
            GUI.Box(rect, "Hiển thị  (F10 đóng · [ ] chỉnh sáng nhanh)");
            GUI.Label(new Rect(rect.x + 16f, rect.y + 28f, w - 32f, 20f), "Độ sáng");
            float v = GUI.HorizontalSlider(new Rect(rect.x + 16f, rect.y + 50f, w - 32f, 20f), brightness, 0f, 1f);
            if (!Mathf.Approximately(v, brightness)) Set(v);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 66f, w - 32f, 22f), $"{Mathf.RoundToInt(brightness * 100f)}%   (mặc định {Mathf.RoundToInt(BrightnessMath.Default * 100f)}%)");
            bool lodOff = GUI.Toggle(new Rect(rect.x + 16f, rect.y + 94f, w - 32f, 24f), LodSetting.Off, " Tắt LOD (chi tiết tối đa, nặng máy hơn)");
            if (lodOff != LodSetting.Off) LodSetting.Apply(lodOff);
        }
    }
}
