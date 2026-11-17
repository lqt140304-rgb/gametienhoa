param([string]$Source = "$PSScriptRoot/../Assets/_Project/Scripts/Abilities/SkillBar.cs")
$ErrorActionPreference='Stop'
# Compile the actual SkillBar source with a small fake engine to exercise Sync/Assign/Clear.
$stubs=@'
namespace UnityEngine {
 public class GameObject {}
 public class MonoBehaviour { public GameObject gameObject=new GameObject(); public T GetComponent<T>() where T:new()=>new T(); public void StartCoroutine(System.Collections.IEnumerator e){} }
 public class RequireComponent:System.Attribute { public RequireComponent(System.Type a,System.Type b){} }
 public static class Time { public static float time; }
 public static class Mathf { public static float Max(float a,float b)=>System.Math.Max(a,b);public static float Clamp01(float a)=>System.Math.Max(0,System.Math.Min(1,a)); }
 public class WaitForSeconds {public WaitForSeconds(float s){} }
}
namespace Evolve.Organs {
 public class OrganSO {public Evolve.Abilities.AbilitySO GrantedAbility;public int AbilityUnlockLevel=1;}
 public class OrganHost {public System.Collections.Generic.List<OrganSO> OrganList=new System.Collections.Generic.List<OrganSO>(); public bool Has(OrganSO o)=>OrganList.Contains(o);public int GetLevel(OrganSO o)=>2;}
}
namespace Evolve.Survival {
 public class SurvivalStats {public State State=new State();}
 public class State {public bool IsDead;public float Energy=100;public void TrySpendEnergy(float v){} }
 public static class KeyPressed {public static bool Digit(int i)=>false;}
 public static class SurvivalHUD {public static bool BlocksInput(UnityEngine.GameObject o)=>false;}
}
namespace Evolve.Abilities {
 public class AbilitySO {public string DisplayName="Test";public float EnergyCost;public float CooldownFor(int l)=>5;public bool Activate(UnityEngine.GameObject o,int l)=>true;}
 public class BurstDashAbility:AbilitySO{} public class ChargeAbility:AbilitySO{}
}
namespace Evolve.Visuals {
 public enum GelMotion{Dash,Release}
 public static class SlimeGelAnimator {public static void Play(UnityEngine.GameObject o,GelMotion m){} public static float WindUp(GelMotion m)=>0;}
}
public static class CheckSkillBar {
 public static void Run(){
 var host=new Evolve.Organs.OrganHost();var bar=new Evolve.Abilities.SkillBar();var organ=new Evolve.Organs.OrganSO{GrantedAbility=new Evolve.Abilities.AbilitySO()};host.OrganList.Add(organ);
 var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
 typeof(Evolve.Abilities.SkillBar).GetField("host",flags).SetValue(bar,host);
 var sync=typeof(Evolve.Abilities.SkillBar).GetMethod("Sync",flags);sync.Invoke(bar,null);
 if(bar.GetSlot(0)!=organ)throw new System.Exception("New skill did not auto-equip");
 bar.Clear(0);sync.Invoke(bar,null);if(bar.SlotOf(organ)>=0)throw new System.Exception("Cleared skill was auto-equipped again");
 bar.Assign(3,organ);sync.Invoke(bar,null);if(bar.GetSlot(3)!=organ)throw new System.Exception("Manual re-equip failed");
 host.OrganList.Clear();sync.Invoke(bar,null);if(bar.GetSlot(3)!=null)throw new System.Exception("Detached organ left a skill");
 host.OrganList.Add(organ);sync.Invoke(bar,null);if(bar.SlotOf(organ)<0)throw new System.Exception("Newly attached organ should auto-equip");
 }
}
'@
Add-Type -TypeDefinition ((Get-Content -Raw $Source)+"`n"+$stubs)
[CheckSkillBar]::Run()
Write-Output 'PASS: auto-equip, persistent unequip, manual equip, detach and reattach (fake-engine regression)'
