using System.Collections.Generic;
using Evolve.Abilities;
using Evolve.Organs;
using Evolve.World;
using UnityEngine;

namespace Evolve.Survival
{
    public partial class SurvivalHUD
    {
        const string FollowPreference = "Evolve.HudFollowPlayer";
        static readonly Dictionary<GameObject, SurvivalHUD> owners = new Dictionary<GameObject, SurvivalHUD>();
        HudMenu menu;
        bool followPlayer, equippedOnly, searchFocused, clearFocus;
        Vector2 organScroll;
        GUIStyle text, small, heading, action, selectedAction, title, center, input;
        Texture2D membrane, disc;
        sealed class GelSkin { public Texture2D Body, Fill; }
        readonly Dictionary<string,GelSkin> gelSkins = new Dictionary<string,GelSkin>();
        GUIStyle panelSkin;
        Vector2 canvas;
        string skillQuery = "";
        int skillPage;
        OrganSO chosenSkill;
        readonly Color ink = new Color32(234,245,250,255);
        readonly Color muted = new Color32(143,174,194,255);
        readonly Color surface = new Color32(17,31,50,247);
        readonly Color cyan = new Color32(128,230,238,255);
        readonly Color coral = new Color32(255,177,167,255);

        public bool MenuOpen => menu != HudMenu.None;
        public static bool HasFor(GameObject owner) => owner != null && owners.TryGetValue(owner, out var hud) && hud != null && hud.isActiveAndEnabled;
        public static bool ExternalMenuOpen;
        public static bool BlocksInput(GameObject owner) => ExternalMenuOpen || Evolve.Visuals.SlimeColorMenu.IsOpen || Evolve.DevTools.GhostMode.PanelOpen || (HasFor(owner) && owners[owner].MenuOpen);

        void RegisterHud()
        {
            owners[stats.gameObject] = this;
            followPlayer = PlayerPrefs.GetInt(FollowPreference, 0) == 1;
        }
        void OnEnable() { if (stats != null) RegisterHud(); }
        void OnDisable()
        {
            if (stats != null && owners.TryGetValue(stats.gameObject, out var owner) && owner == this) owners.Remove(stats.gameObject);
            menu = HudMenu.None;
            if (interactor != null) interactor.CloseBodyMenu();
        }
        void OnDestroy()
        {
            if (membrane != null) Destroy(membrane);
            if (disc != null) Destroy(disc);
            foreach(var skin in gelSkins.Values) { Destroy(skin.Body); Destroy(skin.Fill); }
            gelSkins.Clear();
        }
        void Update()
        {
            if (stats == null) return;
            if (stats.State.IsDead) { SetMenu(HudMenu.None); return; }
            if (KeyPressed.Escape()) SetMenu(HudMenu.None);
            else if (searchFocused) return;
            else if (KeyPressed.K()) SetMenu(HudLayout.Toggle(menu, HudMenu.Skills));
            else if (KeyPressed.G()) SetMenu(HudLayout.Toggle(menu, HudMenu.Organs));
            else if (KeyPressed.B()) SetMenu(HudLayout.Toggle(menu, HudMenu.Stomach));
            else if (KeyPressed.H()) SetMenu(HudLayout.Toggle(menu, HudMenu.Help));
        }
        void SetMenu(HudMenu next)
        {
            if (stats != null && stats.State.IsDead) next = HudMenu.None;
            if (menu != next) { sculptNotice = ""; searchFocused = false; clearFocus = true; }
            menu = next;
            if (interactor == null) return;
            if (next == HudMenu.Organs) interactor.OpenBodyMenu(); else interactor.CloseBodyMenu();
        }
        void EnsureStyles()
        {
            if (text != null) return;
            membrane = MakeTexture(false); disc = MakeTexture(true);
            panelSkin = new GUIStyle { border = new RectOffset(16, 16, 16, 16) };
            panelSkin.normal.background = membrane;
            text = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            text.normal.textColor = ink;
            small = new GUIStyle(text) { fontSize = 12 }; small.normal.textColor = muted;
            heading = new GUIStyle(text) { fontSize = 24, fontStyle = FontStyle.Bold };
            title = new GUIStyle(heading) { fontSize = 28 };
            center = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter };
            action = new GUIStyle(GUI.skin.button) { fontSize = 13, wordWrap = true, border = new RectOffset(16,16,16,16), padding = new RectOffset(8,8,5,5) };
            action.normal.background = membrane; action.hover.background = membrane; action.active.background = membrane;
            action.normal.textColor = ink; action.hover.textColor = Color.white;
            selectedAction = new GUIStyle(action); selectedAction.normal.textColor = coral;
            input = new GUIStyle(GUI.skin.textField) { fontSize = 13, padding = new RectOffset(10,10,8,8) };
        }
        Texture2D MakeTexture(bool circle)
        {
            const int size = 64;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float distance = circle ? Vector2.Distance(new Vector2(x, y), new Vector2(31.5f,31.5f)) - 30
                    : new Vector2(Mathf.Max(Mathf.Abs(x-31.5f)-17,0),Mathf.Max(Mathf.Abs(y-31.5f)-17,0)).magnitude-14;
                float alpha = Mathf.Clamp01(1-distance);
                float edge = Mathf.Clamp01(1.5f + distance);
                pixels[y*size+x] = new Color(Mathf.Lerp(.75f,1,edge),Mathf.Lerp(.83f,1,edge),1,alpha);
            }
            var texture = new Texture2D(size,size,TextureFormat.RGBA32,false) { name = "Cellular HUD", hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            texture.SetPixels(pixels); texture.Apply(false,true); return texture;
        }
        float UiScale => Mathf.Max(.1f, Mathf.Min(1f, Mathf.Min(Screen.width/960f,Screen.height/600f)));
        static float Frac(float value,float max) => max > 0 ? Mathf.Clamp01(value/max) : 0;
        void Paint(Rect r, Color color, Texture texture = null)
        {
            if (Event.current.type != EventType.Repaint) return;
            var old = GUI.color;
            GUI.color = Color.white;
            if (texture != null)
            {
                GUI.color = color;
                GUI.DrawTexture(r,texture);
            }
            else GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,color,0,Mathf.Min(28,r.height*.38f));
            GUI.color = old;
        }
        void Line(float x,float y,float width) => Paint(new Rect(x,y,width,1),new Color(.45f,.69f,.84f,.18f),Texture2D.whiteTexture);
        void Frame(Rect r,Color fill,Color border,float radius)
        {
            if(Event.current.type!=EventType.Repaint)return;
            var previous=GUI.color;GUI.color=Color.white;
            GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,fill,0,radius);
            GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,border,1,radius);
            GUI.color=previous;
        }
        void Label(Rect r,string value,GUIStyle style = null) => GUI.Label(r,value ?? "",style ?? text);
        bool Button(Rect r,string value,bool selected = false)
        {
            bool hover=GUI.enabled&&r.Contains(Event.current.mousePosition);
            var fill=selected?new Color32(55,37,43,235):new Color32(16,33,52,224);
            if(hover)fill=Color.Lerp(fill,new Color32(52,81,105,240),.35f);
            var edge=selected?new Color32(255,177,167,140):new Color32(158,214,237,70);
            Frame(r,fill,edge,r.height>48?12:9);
            bool pressed=GUI.Button(r,GUIContent.none,GUIStyle.none);
            var label=new GUIStyle(center){fontSize=13,padding=new RectOffset(7,7,4,4)};
            label.normal.textColor=selected?coral:ink;
            GUI.Label(r,value,label);return pressed;
        }
        void WithEnabled(bool enabled, System.Action draw)
        {
            bool old = GUI.enabled; GUI.enabled = old && enabled; draw(); GUI.enabled = old;
        }
        void Gel(Rect r,Color tone,float fill,bool skill=false,float phase=0)
        {
            if(Event.current.type!=EventType.Repaint)return;
            int w=Mathf.RoundToInt(r.width),h=Mathf.RoundToInt(r.height);
            Color32 edge=tone;
            string key=$"{w}:{h}:{edge.r}:{edge.g}:{edge.b}:{skill}";
            if(!gelSkins.TryGetValue(key,out var skin))
            {
                const int pad=6;int tw=w+pad*2,th=h+pad*2;
                var body=new Color[tw*th];var mask=new Color[tw*th];
                for(int iy=0;iy<th;iy++)for(int ix=0;ix<tw;ix++)
                {
                    float x=ix-pad,y=th-1-iy-pad;
                    float radius=skill?(y<h*.5f?(x<w*.5f?w*.42f:w*.49f):(x<w*.5f?w*.46f:w*.39f))
                        :(y<h*.5f?(x<w*.5f?28:25):(x<w*.5f?20:27));
                    radius=Mathf.Min(radius,Mathf.Min(w,h)*.49f);
                    float qx=Mathf.Abs(x-(w-1)*.5f)-(w*.5f-radius),qy=Mathf.Abs(y-(h-1)*.5f)-(h*.5f-radius);
                    float distance=Mathf.Min(Mathf.Max(qx,qy),0)+new Vector2(Mathf.Max(qx,0),Mathf.Max(qy,0)).magnitude-radius;
                    float coverage=Mathf.Clamp01(.5f-distance),u=x/w,v=y/h;
                    float shine=Mathf.Exp(-((u-.24f)*(u-.24f)/.055f+(v-.16f)*(v-.16f)/.018f));
                    float rim=Mathf.Exp(-Mathf.Abs(distance+1.1f)*.85f);
                    float inner=Mathf.Exp(-Mathf.Abs(distance+5)*.35f)*.13f;
                    Color color=Color.Lerp(new Color32(18,43,67,255),new Color32(178,244,255,255),shine*.30f+inner);
                    color=Color.Lerp(color,Color.Lerp(tone,new Color32(196,240,255,255),.45f),rim*.72f);
                    color.a=coverage*(.58f+rim*.25f+shine*.12f);
                    if(distance>0){color=tone;color.a=Mathf.Exp(-distance*.72f)*.09f;}
                    body[iy*tw+ix]=color;mask[iy*tw+ix]=new Color(1,1,1,coverage);
                }
                skin=new GelSkin{Body=new Texture2D(tw,th,TextureFormat.RGBA32,false),Fill=new Texture2D(tw,th,TextureFormat.RGBA32,false)};
                skin.Body.name="HUD gel surface";skin.Fill.name="HUD gel fill";
                foreach(var texture in new[]{skin.Body,skin.Fill}){texture.hideFlags=HideFlags.HideAndDontSave;texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;}
                skin.Body.SetPixels(body);skin.Body.Apply(false,true);skin.Fill.SetPixels(mask);skin.Fill.Apply(false,true);gelSkins.Add(key,skin);
            }
            float wobble=Mathf.Sin(Time.unscaledTime*1.7f+phase)*.55f;
            var drawn=new Rect(r.x-6-wobble*.5f,r.y-6+wobble*.25f,r.width+12+wobble,r.height+12-wobble*.5f);
            var previous=GUI.color;GUI.color=Color.white;GUI.DrawTexture(drawn,skin.Body);
            if(fill>0)
            {
                float fraction=Mathf.Clamp01(fill),filled=r.height*fraction;
                GUI.BeginGroup(new Rect(r.x,r.yMax-filled,r.width,filled));
                var tint=tone;tint.a=.11f;GUI.color=tint;
                GUI.DrawTexture(new Rect(drawn.x-r.x,drawn.y-(r.yMax-filled),drawn.width,drawn.height),skin.Fill);
                GUI.EndGroup();
            }
            GUI.color=previous;
        }
        static string VitalNote(int kind,SurvivalState s,SurvivalConfig cfg)
        {
            if(kind==0)
            {
                if(s.IsDead)return "Đã chết";
                float cap=s.MaxHealth*NutrientEffects.MaxHealthMultiplier(s.Nutrients[(int)NutrientKind.Iron]);
                bool fed=s.Hunger>=s.MaxHunger*cfg.HealHungerThreshold;
                bool watered=!s.ThirstEnabled||s.Thirst>=s.MaxThirst*cfg.HealThirstThreshold;
                float regen=fed&&watered&&s.Hunger>0?cfg.HealPerSec*NutrientEffects.HealMultiplier(s.Nutrients[(int)NutrientKind.Protein]):0;
                float heat=System.Math.Max(0,System.Math.Max(cfg.ComfortMin-s.Temperature,s.Temperature-s.HeatLimit(cfg)))*cfg.DamagePerDegreePerSec;
                float loss=s.Invulnerable?0:heat+(s.ThirstEnabled&&s.Thirst<=0?cfg.DehydrationDamagePerSec:0)+(s.Hunger<=0?cfg.StarveDamagePerSec:0);
                if(s.Health>=cap)return loss>0?"Đang mất máu":cap<s.MaxHealth?"Thiếu Huyết Thiết":"Đầy máu";
                return regen>loss?"Đang hồi phục":loss>regen?"Đang mất máu":regen>0?"Ổn định":"Chưa hồi phục";
            }
            if(kind==1){if(s.Energy>=s.MaxEnergy)return "Đầy năng lượng";return s.Hunger>0?"Hồi khi nghỉ":"Cần thức ăn";}
            if(kind==2)return s.Hunger<=0?"Đói lả":s.Hunger>=s.MaxHunger*cfg.HealHungerThreshold?"Đủ dinh dưỡng":"Đang đói";
            if(kind==3)return !s.ThirstEnabled?"Không hao nước":s.Thirst<=0?"Kiệt nước":s.Thirst<s.MaxThirst*.25f?"Thiếu nước":"Đủ nước";
            if(kind==4)return s.Stress>=cfg.StressEnergyStart?"Hoảng loạn":s.Stress>cfg.StressShakeStart?"Căng thẳng":"Bình tĩnh";
            return s.Temperature<cfg.ComfortMin?"Quá lạnh":s.Temperature>s.HeatLimit(cfg)?"Quá nóng":"Nhiệt độ ổn định";
        }
        void DrawGameplayHud()
        {
            EnsureStyles(); var old = GUI.matrix;
            float scale=UiScale;
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
            canvas=new Vector2(Screen.width/scale,Screen.height/scale);
            var s=stats.State; bool full=KeyPressed.TabHeld();
            var anchor=PlayerAnchor(scale); bool floating=followPlayer && anchor.HasValue && !MenuOpen;
            float width=floating?204:Screen.width>=1550?380:Screen.width>=1150?350:285, height=full?(floating?166:242):72;
            bool nutrients=full&&!floating; // the five nutrient cells need room: not in the small floating HUD
            float x=floating?HudLayout.ClampCenter(anchor.Value.x,width,canvas.x)-width/2:22;
            float y=floating?HudLayout.ClampCenter(anchor.Value.y+75,height,canvas.y)-height/2:canvas.y-height-23;
            float top=y+(nutrients?76:0); // the nutrient strip sits above the water/stress row
            float primaryY=top+(full?94:0);
            float cellWidth=(width-12)/3;
            Vital(new Rect(x,primaryY,cellWidth,72),"♥ Máu",s.Health,s.MaxHealth*NutrientEffects.MaxHealthMultiplier(s.Nutrients[(int)NutrientKind.Iron]),coral,VitalNote(0,s,stats.Config));
            Vital(new Rect(x+cellWidth+6,primaryY,cellWidth,72),"ϟ Năng lượng",s.Energy,s.MaxEnergy,cyan,VitalNote(1,s,stats.Config));
            Vital(new Rect(x+(cellWidth+6)*2,primaryY,cellWidth,72),"♧ Độ no",s.Hunger,s.MaxHunger,new Color32(176,220,178,255),VitalNote(2,s,stats.Config));
            if(nutrients)
            {
                float nw=(width-24)/5f;
                for(int k=0;k<NutrientKinds.Count;k++) NutrientCell(new Rect(x+k*(nw+6),y,nw,70),(NutrientKind)k);
            }
            else if(!full)
            {
                // Without Tab: only a hint when the body lacks a nutrient (the five cells show with Tab).
                var worst=s.LowestNutrient();
                if(HudVisibility.ShowNutrient(s.Nutrient(worst)))
                    Label(new Rect(x,y-26,width+120,22),(s.IsWarning(worst)?NutrientWarnLabels:NutrientLackLabels)[(int)worst],small);
            }
            if(s.DiseaseMask!=0) Label(new Rect(x,y-52,width+160,22),DiseaseLine(s),small);
            if(full)
            {
                Vital(new Rect(x,top,cellWidth,68),s.ThirstEnabled?"◈ Nước":"◈ Nước · nghỉ",s.Thirst,s.MaxThirst,new Color32(130,205,239,255),VitalNote(3,s,stats.Config));
                Vital(new Rect(x+cellWidth+6,top,cellWidth,68),"◎ Căng thẳng",s.Stress,SurvivalState.MaxStress,new Color32(196,179,231,255),VitalNote(4,s,stats.Config));
                Vital(new Rect(x+(cellWidth+6)*2,top,cellWidth,68),"♨ Thân nhiệt",s.Temperature,0,new Color32(239,194,158,255),VitalNote(5,s,stats.Config),"°C");
                Label(new Rect(x,top+71,width+160,23),$"{s.Lives}/{s.MaxLives} mạng  ·  Không khí {s.Ambient:0}°C  ·  {(s.InShelter?"Trong tổ":"Ngoài tổ")}",small);
            }
            float chipWidth=Screen.width>=1550?63:55,chipHeight=Screen.width>=1550?65:60;
            float chipStep=chipWidth+8,totalWidth=chipWidth*7+48;
            float skillCenter=Screen.width>=1550?canvas.x*.52f:canvas.x*.5f+70;
            float sx=skillCenter-totalWidth/2,sy=canvas.y-35-chipHeight;
            var active=caster!=null?caster.Ability:null;
            DrawSkillChip(new Rect(sx,sy,chipWidth,chipHeight),"Q",active!=null?active.DisplayName:null,caster!=null?caster.CooldownRemaining:0,active!=null?active.EnergyCost:0,active);
            for(int slot=0;slot<SkillBar.SlotCount;slot++)
            {
                var o=skills!=null?skills.GetSlot(slot):null; var a=o!=null?skills.AbilityFor(o):null;
                DrawSkillChip(new Rect(sx+(slot+1)*chipStep,sy,chipWidth,chipHeight),(slot+1).ToString(),a!=null?a.DisplayName:null,skills!=null?skills.CooldownRemaining(slot):0,a!=null?a.EnergyCost:0,a);
            }
            var sense=stats.GetComponent<VibrationSense>();
            DrawSkillChip(new Rect(sx+6*chipStep,sy,chipWidth,chipHeight),"C",sense!=null?"Cảm nhận rung":null,sense!=null?sense.CooldownRemaining:0,sense!=null?sense.EnergyCost:0);
            float right=canvas.x-180;
            Label(new Rect(right,canvas.y-112,170,22),"Giữ Tab · Xem thêm",small);
            if(Button(new Rect(right,canvas.y-85,156,29),"K · Kỹ năng")) SetMenu(HudLayout.Toggle(menu,HudMenu.Skills));
            if(Button(new Rect(right,canvas.y-51,156,29),"G · Cơ quan")) SetMenu(HudLayout.Toggle(menu,HudMenu.Organs));
            if(Button(new Rect(canvas.x-136,18,112,28),"Giao diện")) SetMenu(HudLayout.Toggle(menu,HudMenu.Settings));
            if(!string.IsNullOrEmpty(GUI.tooltip)) Label(new Rect(sx-30,sy-33,440,28),GUI.tooltip,small);
            if(skills!=null&&!string.IsNullOrEmpty(skills.FailureReason)&&Time.time-skills.FailureTime<2.5f)
                Label(new Rect(sx-35,sy-62,460,28),skills.FailureReason,small);
            GUI.matrix=old;
        }
        static readonly string[] NutrientLackLabels =
        {
            "Thiếu " + NutrientKinds.SlimeName(NutrientKind.Protein) + " · giữ Tab xem thêm", "Thiếu " + NutrientKinds.SlimeName(NutrientKind.Calcium) + " · giữ Tab xem thêm",
            "Thiếu " + NutrientKinds.SlimeName(NutrientKind.Iron) + " · giữ Tab xem thêm", "Thiếu " + NutrientKinds.SlimeName(NutrientKind.Fat) + " · giữ Tab xem thêm",
            "Thiếu " + NutrientKinds.SlimeName(NutrientKind.Vitamin) + " · giữ Tab xem thêm"
        };
        static readonly string[] NutrientWarnLabels =
        {
            "Thiếu " + NutrientKinds.SlimeName(NutrientKind.Protein) + " ! cơ quan sắp yếu", "Thiếu " + NutrientKinds.SlimeName(NutrientKind.Calcium) + " ! cơ quan sắp yếu",
            "Thiếu " + NutrientKinds.SlimeName(NutrientKind.Iron) + " ! cơ quan sắp yếu", "Thiếu " + NutrientKinds.SlimeName(NutrientKind.Fat) + " ! cơ quan sắp yếu",
            "Thiếu " + NutrientKinds.SlimeName(NutrientKind.Vitamin) + " ! cơ quan sắp yếu"
        };
        GUIStyle nutrientName, nutrientNumber;
        byte diseaseShownMask; string diseaseLine = "";

        /// <summary>The HUD line about diseases; rebuilt only when the set of diseases changes (no string per frame).</summary>
        string DiseaseLine(SurvivalState s)
        {
            byte mask = s.DiseaseMask;
            if (mask != diseaseShownMask || diseaseLine.Length == 0) { diseaseShownMask = mask; diseaseLine = DiseaseRules.Describe(mask); }
            return diseaseLine;
        }

        /// <summary>One of the five nutrient cells (Tab panel): colour of the nutrient, bright edge when it is low.</summary>
        void NutrientCell(Rect r,NutrientKind kind)
        {
            var s=stats.State; float value=s.Nutrient(kind); var color=NutrientKinds.ColorOf(kind);
            var edge=color; edge.a=s.IsWarning(kind)?.95f:.4f;
            Frame(r,new Color32(18,43,67,221),edge,22);
            if(nutrientName==null) nutrientName=new GUIStyle(small){fontSize=10,alignment=TextAnchor.UpperCenter};
            if(nutrientNumber==null) nutrientNumber=new GUIStyle(center){fontSize=17};
            nutrientNumber.normal.textColor=color;
            Label(new Rect(r.x+2,r.y+4,r.width-4,30),NutrientKinds.SlimeName(kind),nutrientName);
            Label(new Rect(r.x,r.y+34,r.width,24),$"{value:0}",nutrientNumber);
            Paint(new Rect(r.x+7,r.yMax-8,(r.width-14)*Frac(value,SurvivalState.MaxNutrient),3),color,Texture2D.whiteTexture);
        }

        void Vital(Rect r,string name,float value,float max,Color color,string note,string unit=null)
        {
            Gel(r,color,unit==null?Frac(value,max):0,false,r.x*.03f);
            var caption=new GUIStyle(small){fontSize=10,wordWrap=false,alignment=TextAnchor.MiddleCenter};
            Label(new Rect(r.x+4,r.y+5,r.width-8,17),name,caption);
            var number=new GUIStyle(center) { fontSize=21,richText=true,wordWrap=false };number.normal.textColor=color;
            string suffix=unit??$"/ {max:0}";
            Label(new Rect(r.x,r.y+21,r.width,28),$"{value:0} <size=10><color=#8fb3cb>{suffix}</color></size>",number);
            var status=new GUIStyle(caption){fontSize=9};status.normal.textColor=new Color32(163,197,216,255);
            Label(new Rect(r.x+3,r.yMax-21,r.width-6,17),note,status);
        }
        void DrawSkillChip(Rect r,string key,string name,float cooldown,float cost,AbilitySO ability=null)
        {
            Gel(r,name!=null?new Color32(163,213,235,255):new Color32(80,113,140,255),0,true,r.x*.04f);
            GUI.Label(new Rect(r.x,r.y-24,r.width,22),new GUIContent(key,name??"Ô trống"),center);
            string icon=name==null?"·":key=="C"?"◎":AbilityMark(ability);
            GUI.Label(r,new GUIContent("",name??"Ô trống"),GUIStyle.none);
            Label(new Rect(r.x,r.y+7,r.width,r.height-14),icon,new GUIStyle(center){fontSize=29});
            if(name!=null&&(cooldown>0||stats.State.Energy<cost))
                Label(new Rect(r.x+2,r.yMax-23,r.width-4,20),cooldown>0?$"{cooldown:0.0}s":"NL!",new GUIStyle(small){alignment=TextAnchor.MiddleCenter});
        }
        Vector2? PlayerAnchor(float scale)
        {
            var camera=Camera.main; if(camera==null)return null;
            var p=camera.WorldToScreenPoint(stats.transform.position);
            if(p.z<=0||p.x<0||p.x>Screen.width||p.y<0||p.y>Screen.height)return null;
            return new Vector2(p.x/scale,(Screen.height-p.y)/scale);
        }
        void DrawOverlayMenu()
        {
            if(clearFocus) { GUI.FocusControl(null); clearFocus=false; }
            if(!MenuOpen)return; EnsureStyles(); var old=GUI.matrix;
            if(menu==HudMenu.Skills||menu==HudMenu.Organs||menu==HudMenu.Stomach)
            {
                float scale=HudCatalog.MenuScale(Screen.width,Screen.height,UiScale);
                GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1240*scale)/2,52,0),Quaternion.identity,Vector3.one*scale);
                Frame(new Rect(0,0,1240,650),surface,new Color32(138,200,220,85),28);
                Label(new Rect(25,16,950,42),menu==HudMenu.Skills?"KỸ NĂNG":menu==HudMenu.Organs?"CƠ THỂ":"TÚI KHÔNG ĐÁY",title);
                if(Button(new Rect(1185,17,32,32),"×")) SetMenu(HudMenu.None);
                if(Button(new Rect(23,67,140,34),"G · Cơ quan",menu==HudMenu.Organs)) SetMenu(HudMenu.Organs);
                if(Button(new Rect(172,67,140,34),"B · Bụng",menu==HudMenu.Stomach)) SetMenu(HudMenu.Stomach);
                if(Button(new Rect(321,67,140,34),"K · Kỹ năng",menu==HudMenu.Skills)) SetMenu(HudMenu.Skills);
                Line(22,110,1196);
                if(menu==HudMenu.Skills)DrawSkillInventory(); else if(menu==HudMenu.Organs)DrawOrgansPanel(); else DrawStomachPanel();
                Line(22,607,1196);
                Label(new Rect(25,618,700,25),menu==HudMenu.Organs?$"Khối {BodyMass:0.#} · máu tối đa {stats.State.MaxHealth:0}":menu==HudMenu.Stomach?"Đâm con mồi: nuốt sống · F: nuốt xác (giữ F nếu xác to)":"Q · Dị năng riêng   /   C · Cảm nhận rung",small);
                Label(new Rect(795,618,340,25),menu==HudMenu.Skills?"Giữ Tab · Xem chi tiết":"Giữ Tab · Xem thêm",small);
                if(Button(new Rect(1125,614,91,28),"Esc · Đóng"))SetMenu(HudMenu.None);
            }
            else
            {
                GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*UiScale);
                if(menu==HudMenu.Settings)DrawSettings();
                else
                {
                    Paint(new Rect(24,60,400,canvas.y-90),surface);
                    GUILayout.BeginArea(new Rect(40,75,368,canvas.y-120));
                    if(GUILayout.Button("Đóng · H / Esc",action))SetMenu(HudMenu.None);
                    organScroll=GUILayout.BeginScrollView(organScroll); GUILayout.Label(Controls,text);
                    GUILayout.EndScrollView();GUILayout.EndArea();
                }
            }
            GUI.matrix=old;
        }
        List<OrganSO> Catalog()
        {
            var result=new List<OrganSO>();
            if(organs==null||skills==null)return result;
            foreach(var o in skills.Unlocked())
                if((!equippedOnly||skills.SlotOf(o)>=0)&&HudCatalog.Matches(skills.AbilityFor(o).DisplayName,o.DisplayName,skillQuery))result.Add(o);
            return result;
        }
        void DrawSkillInventory()
        {
            if(organs==null||skills==null){Label(new Rect(30,140,800,50),"Chưa có kỹ năng.");return;}
            var owned=skills.Unlocked();
            if(chosenSkill==null||!skills.IsUnlocked(chosenSkill))chosenSkill=owned.Count>0?owned[0]:null;
            Label(new Rect(26,126,340,25),"KHO KỸ NĂNG",small);
            GUI.SetNextControlName("CellularSkillSearch");
            var query=GUI.TextField(new Rect(26,158,230,34),skillQuery,input);
            searchFocused=GUI.GetNameOfFocusedControl()=="CellularSkillSearch";
            if(query!=skillQuery){skillQuery=query;skillPage=0;}
            if(Button(new Rect(264,158,112,34),equippedOnly?"Đã gắn":"Tất cả",equippedOnly)){equippedOnly=!equippedOnly;skillPage=0;}
            var list=Catalog(); skillPage=HudCatalog.ClampPage(skillPage,list.Count,8);
            for(int i=0;i<8;i++)
            {
                int index=skillPage*8+i;if(index>=list.Count)break;var o=list[index];
                var r=new Rect(26+(i%2)*179,208+(i/2)*78,171,70);
                int slot=skills.SlotOf(o);
                if(Button(r,$"{skills.AbilityFor(o).DisplayName}\n{(slot<0?"Chưa gắn":$"Ô {slot+1}")}",chosenSkill==o))chosenSkill=o;
            }
            if(list.Count==0)Label(new Rect(28,215,330,65),string.IsNullOrWhiteSpace(skillQuery)?"Chưa học chiêu nào. Nuốt sinh vật có gai, chân... rồi để Giải Mã phân tích.":"Không tìm thấy kỹ năng.",small);
            WithEnabled(skillPage>0,()=>{if(Button(new Rect(88,550,40,30),"←"))skillPage--;});
            Label(new Rect(137,554,130,25),$"{skillPage+1} / {HudCatalog.PageCount(list.Count,8)}  ·  {list.Count} kỹ năng",small);
            WithEnabled(skillPage+1<HudCatalog.PageCount(list.Count,8),()=>{if(Button(new Rect(294,550,40,30),"→"))skillPage++;});
            Paint(new Rect(395,125,1,460),new Color(.5f,.7f,.9f,.16f),Texture2D.whiteTexture);
            if(chosenSkill==null){Label(new Rect(430,180,720,70),"Nuốt sinh vật rồi để Giải Mã học chiêu của nó.");return;}
            var ability=skills.AbilityFor(chosenSkill);int level=skills.LevelFor(chosenSkill);int stars=Mathf.Max(1,skills.StarsOf(chosenSkill));
            Paint(new Rect(427,134,70,70),new Color(.18f,.32f,.43f),disc);
            Label(new Rect(427,147,70,40),AbilityMark(ability),new GUIStyle(heading){alignment=TextAnchor.MiddleCenter});
            Label(new Rect(515,133,680,25),$"{chosenSkill.DisplayName} · {new string('★',stars)}",small);
            Label(new Rect(515,162,680,43),ability.DisplayName,heading);
            if(KeyPressed.TabHeld())DrawSkillCalculation(new Rect(427,224,485,350),ability,level);
            else
            {
                Label(new Rect(427,224,485,115),ability.Description);
                Line(427,348,485);
                Metric(new Rect(427,365,155,65),"NĂNG LƯỢNG",$"{ability.EnergyCost:0.#}");
                Metric(new Rect(592,365,155,65),"HỒI CHIÊU",$"{ability.CooldownFor(level):0.#} giây");
                Metric(new Rect(757,365,155,65),"TRẠNG THÁI",ReadyFor(chosenSkill));
                Label(new Rect(427,454,485,70),"Giữ Tab để xem cách tính và hiệu ứng của kỹ năng.",small);
            }
            Label(new Rect(947,229,260,25),"TRANG BỊ VÀO Ô",small);
            Label(new Rect(947,265,260,65),"Chọn ô để gắn hoặc đổi kỹ năng.",small);
            for(int slot=0;slot<SkillBar.SlotCount;slot++)
            {
                var current=skills.GetSlot(slot);int target=slot;
                string mark=current!=null?AbilityMark(skills.AbilityFor(current)):"+";
                if(Button(new Rect(947+slot*52,342,46,62),$"{mark}\n{slot+1}",current==chosenSkill))skills.Assign(target,chosenSkill);
            }
            int at=skills.SlotOf(chosenSkill);
            Label(new Rect(947,423,260,50),at<0?"Chưa gắn":$"Đang gắn ở ô {at+1}",small);
            WithEnabled(at>=0,()=>{if(Button(new Rect(947,489,245,35),"Gỡ kỹ năng"))skills.Clear(at);});
        }
        string AbilityMark(AbilitySO a)=>a is SpikeBurstAbility?"✦":a is ChargeAbility?"➶":a is BurstDashAbility?"↝":a is SprintAbility?"»":"◇";
        string ReadyFor(OrganSO o)
        {
            int slot=skills.SlotOf(o);
            return slot<0?"Chưa gắn":skills.CooldownRemaining(slot)>0?$"{skills.CooldownRemaining(slot):0.0}s":stats.State.Energy<skills.AbilityFor(o).EnergyCost?"Thiếu năng lượng":"Sẵn sàng";
        }
        void DrawSkillCalculation(Rect r,AbilitySO ability,int level)
        {
            Label(new Rect(r.x,r.y,r.width,22),"CHI TIẾT",small);
            float y=r.y+37;
            if(ability is SpikeBurstAbility spike)
            {
                int count=Mathf.Max(1,spike.SpikeCount)+(level>=3?2:0);
                float power=AbilitySO.Power(level),each=spike.SpikeDamage*power;
                Label(new Rect(r.x,y,r.width,38),$"{HudCatalog.SpikeTotal(spike.SpikeDamage,spike.SpikeCount,level):0.#} sát thương tối đa",heading); y+=52;
                Label(new Rect(r.x,y,r.width,54),$"{spike.SpikeDamage:0.#} × {power:0.##} × {count} gai = {each*count:0.#}");y+=61;
                Label(new Rect(r.x,y,r.width,66),$"Mỗi gai: {each:0.#} sát thương\nTầm: {spike.Range:0.#} m · Góc xòe: {spike.SpreadAngle:0.#}°");y+=77;
                Label(new Rect(r.x,y,r.width,65),"Chỉ gây tổng sát thương trên nếu tất cả gai đều trúng. Giá trị lấy từ kỹ năng và cấp cơ quan đang gắn.",small);y+=73;
            }
            else if(ability is ChargeAbility charge)
            {
                Label(new Rect(r.x,y,r.width,38),"Lao mạnh",heading);y+=48;
                Label(new Rect(r.x,y,r.width,70),$"Lực lao: {charge.Force:0.#} × {AbilitySO.Power(level):0.##} = {charge.Force*AbilitySO.Power(level):0.#}\nHướng lao theo điểm đang ngắm.");y+=86;
                Label(new Rect(r.x,y,r.width,65),"Sát thương va chạm còn phụ thuộc tốc độ và sinh vật bị đâm trúng.",small);y+=80;
            }
            else if(ability is SprintAbility sprint)
            {
                Label(new Rect(r.x,y,r.width,38),"Chạy nước rút",heading);y+=48;
                Label(new Rect(r.x,y,r.width,70),$"Tốc độ ×{sprint.Multiplier:0.#} trong {Evolve.Swallow.SkillRules.SprintSeconds(level):0} giây.\nChân mọc ra khi chạy, thu vào khi hồi chiêu.");y+=86;
                Label(new Rect(r.x,y,r.width,65),"Lên sao để chạy lâu hơn (4 / 5 / 6 giây) và hồi chiêu nhanh hơn ở ★3.",small);y+=80;
            }
            else if(ability is BurstDashAbility dash)
            {
                Label(new Rect(r.x,y,r.width,38),"Lướt nhanh",heading);y+=48;
                Label(new Rect(r.x,y,r.width,70),$"Lực đẩy: {dash.Force:0.#}\nVật cản có thể làm bạn dừng sớm.");y+=86;
            }
            else {Label(new Rect(r.x,y,r.width,90),ability.Description);y+=104;}
            Label(new Rect(r.x,y,r.width,72),$"Tốn {ability.EnergyCost:0.#} năng lượng · Hồi {ability.CooldownFor(level):0.##} giây\nHồi chiêu gốc {ability.Cooldown:0.##} giây{(level>=3?" × 0,75 ở cấp 3":"")}",small);
        }
        void DrawBodyDossier(Rect r)
        {
            var s=stats.State;Label(new Rect(r.x,r.y,r.width,30),"CƠ THỂ & MÔI TRƯỜNG",small);
            string[] names={"Máu","Năng lượng","Độ no","Nước","Căng thẳng","Thân nhiệt","Không khí","Kháng nóng","Số mạng","Trú ẩn","Tốc độ","Phản sát thương"};
            string[] values={$"{s.Health:0}/{s.MaxHealth*NutrientEffects.MaxHealthMultiplier(s.Nutrients[(int)NutrientKind.Iron]):0}",$"{s.Energy:0}/{s.MaxEnergy:0}",$"{s.Hunger:0}/{s.MaxHunger:0}",$"{s.Thirst:0}/{s.MaxThirst:0}",$"{s.Stress:0}",$"{s.Temperature:0}°C",$"{s.Ambient:0}°C",$"+{s.HeatResistance:0.#}°C",$"{s.Lives}/{s.MaxLives}",s.InShelter?"Trong tổ":"Ngoài tổ",$"×{organs.SpeedMultiplier:0.##}",$"{organs.ContactDamage:0.#}"};
            for(int i=0;i<names.Length;i++)Metric(new Rect(r.x+(i%3)*128,r.y+47+(i/3)*77,120,67),names[i],values[i]);
            Label(new Rect(r.x,r.y+374,r.width,60),$"Vùng nhiệt an toàn: {stats.Config.ComfortMin:0}–{s.HeatLimit(stats.Config):0}°C\n{(s.ThirstEnabled?"Đang tiêu hao nước":"Vùng này không tiêu hao nước")}",small);
        }
        void Metric(Rect r,string name,string value)
        {
            Paint(r,new Color(.10f,.18f,.27f,.55f));Label(new Rect(r.x+8,r.y+5,r.width-16,23),name,small);Label(new Rect(r.x+8,r.y+30,r.width-16,30),value);
        }
        void DrawSettings()
        {
            Paint(new Rect(canvas.x-354,56,330,230),surface);
            GUILayout.BeginArea(new Rect(canvas.x-338,70,298,202));GUILayout.Label("Vị trí chỉ số",heading);
            bool fixedMode=GUILayout.Toggle(!followPlayer,"Cố định ở hai góc dưới"),followMode=GUILayout.Toggle(followPlayer,"Đi theo nhân vật");
            bool next=followPlayer;if(fixedMode&&followPlayer)next=false;if(followMode&&!followPlayer)next=true;
            if(next!=followPlayer){followPlayer=next;PlayerPrefs.SetInt(FollowPreference,next?1:0);PlayerPrefs.Save();}
            if(GUILayout.Button("Hướng dẫn · H",action))SetMenu(HudMenu.Help);
            if(GUILayout.Button("Đóng · Esc",action))SetMenu(HudMenu.None);GUILayout.EndArea();
        }
    }
}
