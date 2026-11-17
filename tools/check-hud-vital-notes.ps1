$ErrorActionPreference='Stop'
$source=Get-Content -Raw "$PSScriptRoot/../Assets/_Project/Scripts/Survival/SurvivalHUD.Drawing.cs"
$method=[regex]::Match($source,'(?s)static string VitalNote\(.*?(?=\s*void DrawGameplayHud\()').Value
if(!$method){throw 'VitalNote method not found'}
$method=$method.Replace('static string VitalNote','public static string VitalNote')
$code=@'
using System;
public class SurvivalState {
 public float Health=80,MaxHealth=100,Hunger=80,MaxHunger=100,Thirst=80,MaxThirst=100,Energy=60,MaxEnergy=100,Stress,Temperature=22;
 public float[] Nutrients={80,80,80,80,80};public bool Invulnerable;public bool ThirstEnabled;public bool IsDead=>Health<=0;public float HeatLimit(SurvivalConfig c)=>c.ComfortMax;
}
public class SurvivalConfig {public float DamagePerDegreePerSec=.2f,DehydrationDamagePerSec=3,StarveDamagePerSec=4;public float ComfortMin=5,ComfortMax=35,HealHungerThreshold=.7f,HealThirstThreshold=.5f,HealPerSec=1.5f,StressEnergyStart=80,StressShakeStart=40;}
public enum NutrientKind {Protein,Calcium,Iron,Fat,Vitamin}
public static class NutrientEffects {public static float HealMultiplier(float p)=>p>=40?1:.4f+.6f*Math.Max(0,p)/40;public static float MaxHealthMultiplier(float iron)=>iron>=40?1:.7f+.3f*Math.Max(0,iron)/40;}
public static class VitalCheck {
'@+$method+@'
 public static void Run(){
 var s=new SurvivalState();var c=new SurvivalConfig();
 if(VitalNote(0,s,c)!="Đang hồi phục")throw new Exception("Fed player should heal");
 s.Hunger=0;if(VitalNote(0,s,c)=="Đang hồi phục")throw new Exception("Starving player cannot heal");
 if(VitalNote(1,s,c)!="Cần thức ăn")throw new Exception("No food, no energy regen");
 if(VitalNote(2,s,c)!="Đói lả")throw new Exception("Empty hunger");
 s.Hunger=80;s.ThirstEnabled=true;s.Thirst=0;if(VitalNote(0,s,c)=="Đang hồi phục")throw new Exception("Dehydrated player cannot heal");
 if(VitalNote(3,s,c)!="Kiệt nước")throw new Exception("Empty water");
 s.ThirstEnabled=false;if(VitalNote(3,s,c)!="Không hao nước")throw new Exception("Disabled thirst");
 s.Stress=90;if(VitalNote(4,s,c)!="Hoảng loạn")throw new Exception("High stress");
 s.Nutrients[2]=0;s.Health=70;if(VitalNote(0,s,c)!="Thiếu Huyết Thiết")throw new Exception("Effective health cap");
 s.Temperature=40;if(VitalNote(5,s,c)!="Quá nóng")throw new Exception("Heat warning");
 s.Nutrients[2]=80;s.Health=80;s.Temperature=36;if(VitalNote(0,s,c)!="Đang hồi phục")throw new Exception("Small heat damage must not hide net recovery");
 s.Temperature=50;if(VitalNote(0,s,c)!="Đang mất máu")throw new Exception("Heat exceeds regen");
 s.Invulnerable=true;if(VitalNote(0,s,c)!="Đang hồi phục")throw new Exception("Invulnerable damage ignored");
 }
}
'@
Add-Type -TypeDefinition $code
[VitalCheck]::Run()
Write-Output 'PASS: 13 vital status cases from the actual HUD method'
