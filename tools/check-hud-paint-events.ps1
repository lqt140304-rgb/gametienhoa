param([string]$Source="$PSScriptRoot/../Assets/_Project/Scripts/Survival/SurvivalHUD.Drawing.cs")
$ErrorActionPreference='Stop'
$sourceText=Get-Content -Raw -LiteralPath $Source
$method=[regex]::Match($sourceText,'(?s)void Paint\(Rect r, Color color, Texture texture = null\).*?(?=\s*void Line\()').Value
if(!$method){throw 'Paint method not found'}
$frame=[regex]::Match($sourceText,'(?s)void Frame\(Rect r,Color fill,Color border,float radius\).*?(?=\s*void Label\()').Value
if(!$frame){throw 'Frame method not found'}
$code=@'
using System;
using UnityEngine;
namespace UnityEngine {
 public struct Rect {public float width,height;}
 public struct Color {public int Value;public static Color white=>new Color();}
 public class Texture {}
 public class Texture2D:Texture {public static Texture2D whiteTexture=new Texture2D();}
 public enum ScaleMode {StretchToFill}
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);}
 public enum EventType {Layout,Repaint,MouseDown,MouseUp,KeyDown,Used}
 public class Event {public static Event current;public EventType type;}
 public class GUIContent {public static GUIContent none=new GUIContent();}
 public static class GUI {public static Color color;public static int TextureDraws;public static void DrawTexture(Rect r,Texture t){if(Event.current.type!=EventType.Repaint)throw new Exception("Texture draw outside Repaint");TextureDraws++;}public static void DrawTexture(Rect r,Texture t,ScaleMode s,bool blend,float aspect,Color c,float border,float radius){DrawTexture(r,t);}}
 public class GUIStyle {public static int Draws;public void Draw(Rect r,GUIContent c,bool a,bool b,bool d,bool e){if(Event.current.type!=EventType.Repaint)throw new Exception("Style.Draw may not be called if it is not a repaint event");Draws++;}}
}
public class PaintHarness {
 GUIStyle panelSkin=new GUIStyle();
'@ + $method + $frame + @'
 public void Check(){
 foreach(EventType t in Enum.GetValues(typeof(EventType)))foreach(bool textured in new[]{false,true}){
 Event.current=new Event{type=t};GUI.color=new Color{Value=17};
 int before=GUIStyle.Draws+GUI.TextureDraws;
 Paint(new Rect(),new Color{Value=99},textured?new Texture():null);
 int expected=t==EventType.Repaint?1:0;
 if(GUIStyle.Draws+GUI.TextureDraws-before!=expected)throw new Exception("Incorrect draw count: "+t);
 if(GUI.color.Value!=17)throw new Exception("GUI tint leaked: "+t);
 before=GUI.TextureDraws;Frame(new Rect(),new Color(),new Color(),26);
 if(GUI.TextureDraws-before!=(t==EventType.Repaint?2:0))throw new Exception("Frame drew outside Repaint");
 if(GUI.color.Value!=17)throw new Exception("Frame tint leaked");
 }
 }
}
'@
Add-Type -TypeDefinition $code
(New-Object PaintHarness).Check()
Write-Output 'PASS: actual Paint method draws only on Repaint; 12 event/texture cases plus rounded frames; tint restored'
