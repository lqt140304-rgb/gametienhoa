$ErrorActionPreference='Stop'
$source=Get-Content -Raw "$PSScriptRoot/../Assets/_Project/Scripts/Survival/SurvivalHUD.Drawing.cs"
$method=[regex]::Match($source,'(?s)void Gel\(.*?(?=\s*static string VitalNote\()').Value
if(!$method){throw 'Gel renderer not found'}
$code=@'
using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine {
 public struct Rect {public float x,y,width,height;public float yMax=>y+height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}}
 public struct Vector2 {float x,y;public Vector2(float a,float b){x=a;y=b;}public float magnitude=>(float)Math.Sqrt(x*x+y*y);}
 public struct Color {public float r,g,b,a;public Color(float x,float y,float z,float w=1){r=x;g=y;b=z;a=w;}public static Color white=>new Color(1,1,1);public static Color Lerp(Color x,Color y,float t){t=Mathf.Clamp01(t);return new Color(x.r+(y.r-x.r)*t,x.g+(y.g-x.g)*t,x.b+(y.b-x.b)*t,x.a+(y.a-x.a)*t);}}
 public struct Color32 {public byte r,g,b,a;public Color32(byte x,byte y,byte z,byte w){r=x;g=y;b=z;a=w;}public static implicit operator Color(Color32 c)=>new Color(c.r/255f,c.g/255f,c.b/255f,c.a/255f);public static implicit operator Color32(Color c)=>new Color32((byte)(c.r*255),(byte)(c.g*255),(byte)(c.b*255),(byte)(c.a*255));}
 public static class Mathf {public static float Min(float x,float y)=>Math.Min(x,y);public static float Max(float x,float y)=>Math.Max(x,y);public static int RoundToInt(float x)=>(int)Math.Round(x);public static float Abs(float x)=>Math.Abs(x);public static float Exp(float x)=>(float)Math.Exp(x);public static float Sin(float x)=>(float)Math.Sin(x);public static float Clamp01(float x)=>Math.Max(0,Math.Min(1,x));}
 public enum TextureFormat{RGBA32} public enum HideFlags{HideAndDontSave} public enum FilterMode{Bilinear} public enum TextureWrapMode{Clamp}
 public class Texture2D {public Color[] Pixels;public int Width,Height;public string name;public HideFlags hideFlags;public FilterMode filterMode;public TextureWrapMode wrapMode;public Texture2D(int w,int h,TextureFormat f,bool m){Width=w;Height=h;}public void SetPixels(Color[] p){Pixels=p;}public void Apply(bool a,bool b){}}
 public enum EventType{Repaint,Layout,MouseDown,KeyDown} public class Event {public static Event current=new Event();public EventType type;}
 public static class Time {public static float unscaledTime;}
 public static class GUI {public static Color color=Color.white;public static int Draws,Groups;public static Rect LastRect;public static void DrawTexture(Rect r,Texture2D t){if(Event.current.type!=EventType.Repaint)throw new Exception("Draw outside Repaint");Draws++;LastRect=r;}public static void BeginGroup(Rect r){Groups++;}public static void EndGroup(){Groups--;}}
}
public class GelCheck {
 sealed class GelSkin {public Texture2D Body,Fill;}
 readonly Dictionary<string,GelSkin> gelSkins=new Dictionary<string,GelSkin>();
'@+$method+@'
 public void Run(){
 var r=new Rect(25,600,112,72);Color tone=new Color32(255,177,167,255);
 foreach(var type in new[]{EventType.Layout,EventType.MouseDown,EventType.KeyDown}){Event.current.type=type;Gel(r,tone,.5f);}
 if(gelSkins.Count!=0||GUI.Draws!=0)throw new Exception("Non-repaint allocated or drew");
 Event.current.type=EventType.Repaint;Gel(r,tone,.8f);
 GelSkin skin=null;foreach(var value in gelSkins.Values)skin=value;
 var body=skin.Body;var middle=body.Pixels[(body.Height/2)*body.Width+body.Width/2];
 if(middle.a<=0||middle.a>=.95f)throw new Exception("Gel center must be translucent");
 if(body.Pixels[0].a>=.01f)throw new Exception("Texture corner must remain clear");
 foreach(var c in body.Pixels)if(float.IsNaN(c.a)||c.a<0||c.a>1)throw new Exception("Invalid alpha");
 Gel(r,tone,.2f);if(gelSkins.Count!=1)throw new Exception("Changing stat rebuilt texture");
 Time.unscaledTime=1;Gel(r,tone,.2f);if(Math.Abs(GUI.LastRect.width-124)<.01f)throw new Exception("Gel did not wobble");
 Gel(new Rect(400,600,55,60),tone,0,true);if(gelSkins.Count!=2)throw new Exception("Skill skin not cached separately");
 if(GUI.Groups!=0||GUI.color.a!=1)throw new Exception("GUI state leaked");
 }
}
'@
Add-Type -TypeDefinition $code
(New-Object GelCheck).Run()
Write-Output 'PASS: translucent pixels, clear corners, alpha bounds, Repaint gate, texture reuse, wobble and GUI state'
