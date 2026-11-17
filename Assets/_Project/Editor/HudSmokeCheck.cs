using System;
using System.Reflection;
using Evolve.Survival;
using Evolve.Organs;
using Evolve.Abilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Batch smoke check. Loads the existing scene without saving any changes.</summary>
[InitializeOnLoad]
public static class HudSmokeCheck
{
    const string Active = "Evolve.HudSmokeCheck";
    static int frames;
    static HudSmokeCheck()
    {
        if (SessionState.GetBool(Active, false)) EditorApplication.update += Tick;
    }

    public static void Run()
    {
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Prototype_01.unity");
        EditorApplication.EnterPlaymode();
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || ++frames < 120) return;
        EditorApplication.update -= Tick;
        int result = 0;
        try
        {
            var hud = UnityEngine.Object.FindAnyObjectByType<SurvivalHUD>();
            Require(hud != null, "Scene contains HUD");
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var stats = (SurvivalStats)typeof(SurvivalHUD).GetField("stats", flags).GetValue(hud);
            Require(SurvivalHUD.HasFor(stats.gameObject), "HUD registered for its player");
            var set = typeof(SurvivalHUD).GetMethod("SetMenu", flags);
            set.Invoke(hud, new object[] { HudMenu.Skills });
            Require(SurvivalHUD.BlocksInput(stats.gameObject), "Skills block gameplay input");
            Require(!stats.GetComponent<OrganInteractor>().BodyMenuOpen, "Skills close organ menu");
            set.Invoke(hud, new object[] { HudMenu.Organs });
            Require(stats.GetComponent<OrganInteractor>().BodyMenuOpen, "Organs open host menu");
            set.Invoke(hud, new object[] { HudMenu.None });
            Require(!SurvivalHUD.BlocksInput(stats.gameObject), "Close restores gameplay input");
            Require(!stats.GetComponent<OrganInteractor>().BodyMenuOpen, "Close clears organ state");
            Require(stats.GetComponent<SkillBar>() != null, "Existing scene supports five skill slots");
            set.Invoke(hud, new object[] { HudMenu.Skills });
            stats.State.Health = 0;
            typeof(SurvivalHUD).GetMethod("Update", flags).Invoke(hud, null);
            Require(!hud.MenuOpen, "Death closes menus");
            Debug.Log("HUD_SMOKE_PASS: 9 scene/menu/lifecycle checks");
        }
        catch (Exception e) { Debug.LogException(e); result = 1; }
        SessionState.SetBool(Active, false);
        EditorApplication.Exit(result);
    }

    static void Require(bool value, string message)
    {
        if (!value) throw new Exception("HUD smoke: " + message);
    }
}
