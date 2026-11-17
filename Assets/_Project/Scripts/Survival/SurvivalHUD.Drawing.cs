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
        GUIStyle panelSkin;
        Vector2 canvas;
        string skillQuery = "";
        int skillPage, organPage;
        OrganSO chosenSkill, chosenOrgan, pendingDrop;
        readonly Color ink = new Color(.87f, .94f, .98f);
        readonly Color muted = new Color(.59f, .73f, .82f);
        readonly Color surface = new Color(.045f, .075f, .13f, .97f);
        readonly Color cyan = new Color(.48f, .84f, .91f);
        readonly Color coral = new Color(.96f, .70f, .63f);

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
        }
        void Update()
        {
            if (stats == null) return;
            if (stats.State.IsDead) { SetMenu(HudMenu.None); return; }
            if (KeyPressed.Escape()) SetMenu(HudMenu.None);
            else if (searchFocused) return;
            else if (KeyPressed.K()) SetMenu(HudLayout.Toggle(menu, HudMenu.Skills));
            else if (KeyPressed.G()) SetMenu(HudLayout.Toggle(menu, HudMenu.Organs));
            else if (KeyPressed.H()) SetMenu(HudLayout.Toggle(menu, HudMenu.Help));
        }
        void SetMenu(HudMenu next)
        {
            if (stats != null && stats.State.IsDead) next = HudMenu.None;
            if (menu != next) { pendingDrop = null; searchFocused = false; clearFocus = true; }
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
        float UiScale => Mathf.Max(.1f, Mathf.Min(1.4f, Mathf.Min(Screen.width/1280f,Screen.height/800f)));
        static float Frac(float value,float max) => max > 0 ? Mathf.Clamp01(value/max) : 0;
        void Paint(Rect r, Color color, Texture texture = null)
        {
            var old = GUI.color; GUI.color = color;
            if (texture != null) GUI.DrawTexture(r,texture);
            else panelSkin.Draw(r,GUIContent.none,false,false,false,false);
            GUI.color = old;
        }
        void Line(float x,float y,float width) => Paint(new Rect(x,y,width,1),new Color(.45f,.69f,.84f,.18f),Texture2D.whiteTexture);
        void Label(Rect r,string value,GUIStyle style = null) => GUI.Label(r,value ?? "",style ?? text);
        bool Button(Rect r,string value,bool selected = false) => GUI.Button(r,value,selected ? selectedAction : action);
        void WithEnabled(bool enabled, System.Action draw)
        {
            bool old = GUI.enabled; GUI.enabled = old && enabled; draw(); GUI.enabled = old;
        }
        void DrawGameplayHud()
        {
            EnsureStyles(); var old = GUI.matrix;
            float scale=UiScale;
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
            canvas=new Vector2(Screen.width/scale,Screen.height/scale);
            var s=stats.State; bool full=KeyPressed.TabHeld();
            var anchor=PlayerAnchor(scale); bool floating=followPlayer && anchor.HasValue && !MenuOpen;
            float width=floating?204:282, height=full?174:80;
            float x=floating?HudLayout.ClampCenter(anchor.Value.x,width,canvas.x)-width/2:22;
            float y=floating?HudLayout.ClampCenter(anchor.Value.y+75,height,canvas.y)-height/2:canvas.y-height-22;
            float primaryY=y+(full?94:0);
            float cellWidth=(width-12)/3;
            Vital(new Rect(x,primaryY,cellWidth,70),"Máu",s.Health,s.MaxHealth,coral);
            Vital(new Rect(x+cellWidth+6,primaryY,cellWidth,70),"Năng lượng",s.Energy,s.MaxEnergy,cyan);
            Vital(new Rect(x+(cellWidth+6)*2,primaryY,cellWidth,70),"Độ no",s.Hunger,s.MaxHunger,new Color(.65f,.85f,.67f));
            if(full)
            {
                Vital(new Rect(x,y,cellWidth,68),s.ThirstEnabled?"Nước":"Nước · nghỉ",s.Thirst,s.MaxThirst,cyan);
                Vital(new Rect(x+cellWidth+6,y,cellWidth,68),"Căng thẳng",s.Stress,SurvivalState.MaxStress,new Color(.76f,.68f,.91f));
                Paint(new Rect(x+(cellWidth+6)*2,y,cellWidth,68),surface);
                Label(new Rect(x+(cellWidth+6)*2+5,y+5,cellWidth-10,20),"Thân nhiệt",small);
                Label(new Rect(x+(cellWidth+6)*2,y+25,cellWidth,30),$"{s.Temperature:0}°C",center);
                Label(new Rect(x,y+71,width+160,23),$"{s.Lives}/{s.MaxLives} mạng  ·  Không khí {s.Ambient:0}°C  ·  {(s.InShelter?"Trong tổ":"Ngoài tổ")}",small);
            }
            float sx=canvas.x/2-187,sy=canvas.y-84;
            var active=caster!=null?caster.Ability:null;
            DrawSkillChip(new Rect(sx,sy,50,58),"Q",active!=null?active.DisplayName:null,caster!=null?caster.CooldownRemaining:0,active!=null?active.EnergyCost:0);
            for(int slot=0;slot<SkillBar.SlotCount;slot++)
            {
                var o=skills!=null?skills.GetSlot(slot):null; var a=o!=null?o.GrantedAbility:null;
                DrawSkillChip(new Rect(sx+(slot+1)*54,sy,50,58),(slot+1).ToString(),a!=null?a.DisplayName:null,skills!=null?skills.CooldownRemaining(slot):0,a!=null?a.EnergyCost:0);
            }
            var sense=stats.GetComponent<VibrationSense>();
            DrawSkillChip(new Rect(sx+324,sy,50,58),"C",sense!=null?"Cảm nhận rung":null,sense!=null?sense.CooldownRemaining:0,sense!=null?sense.EnergyCost:0);
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
        void Vital(Rect r,string name,float value,float max,Color color)
        {
            Paint(r,surface); Label(new Rect(r.x+7,r.y+5,r.width-14,20),name,small);
            var number=new GUIStyle(center) { fontSize=22 }; number.normal.textColor=color;
            Label(new Rect(r.x,r.y+24,r.width,28),$"{value:0}",number);
            Paint(new Rect(r.x+9,r.yMax-9,(r.width-18)*Frac(value,max),3),color,Texture2D.whiteTexture);
        }
        void DrawSkillChip(Rect r,string key,string name,float cooldown,float cost)
        {
            Paint(r,name!=null?surface:new Color(.04f,.08f,.14f,.4f));
            GUI.Label(new Rect(r.x,r.y+3,r.width,23),new GUIContent(key,name??"Ô trống"),center);
            Label(new Rect(r.x+2,r.y+29,r.width-4,23),name==null?"—":cooldown>0?$"{cooldown:0.0}s":stats.State.Energy<cost?"NL!":"Sẵn",new GUIStyle(small){alignment=TextAnchor.MiddleCenter});
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
            if(menu==HudMenu.Skills||menu==HudMenu.Organs)
            {
                float scale=HudCatalog.MenuScale(Screen.width,Screen.height,UiScale);
                GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1240*scale)/2,32,0),Quaternion.identity,Vector3.one*scale);
                Paint(new Rect(0,0,1240,650),surface);
                Label(new Rect(25,16,950,42),menu==HudMenu.Skills?"KỸ NĂNG":"CƠ THỂ",title);
                if(Button(new Rect(1185,17,32,32),"×")) SetMenu(HudMenu.None);
                if(Button(new Rect(23,67,140,34),"G · Cơ quan",menu==HudMenu.Organs)) SetMenu(HudMenu.Organs);
                if(Button(new Rect(172,67,140,34),"K · Kỹ năng",menu==HudMenu.Skills)) SetMenu(HudMenu.Skills);
                Line(22,110,1196);
                if(menu==HudMenu.Skills)DrawSkillInventory(); else if(menu==HudMenu.Organs)DrawOrgansPanel();
                Line(22,607,1196);
                Label(new Rect(25,618,470,25),menu==HudMenu.Organs?$"EXP còn: {stats.State.EvolutionExp:0}":"Q · Dị năng riêng   /   C · Cảm nhận rung",small);
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
            foreach(var o in organs.OrganList)
                if(skills.IsUnlocked(o)&&(!equippedOnly||skills.SlotOf(o)>=0)&&HudCatalog.Matches(o.GrantedAbility.DisplayName,o.DisplayName,skillQuery))result.Add(o);
            return result;
        }
        void DrawSkillInventory()
        {
            if(organs==null||skills==null){Label(new Rect(30,140,800,50),"Chưa có kỹ năng cơ quan.");return;}
            var owned=organs.OrganList;
            if(chosenSkill==null||!skills.IsUnlocked(chosenSkill))chosenSkill=owned.Find(o=>skills.IsUnlocked(o));
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
                if(Button(r,$"{o.GrantedAbility.DisplayName}\n{(slot<0?"Chưa gắn":$"Ô {slot+1}")}",chosenSkill==o))chosenSkill=o;
            }
            if(list.Count==0)Label(new Rect(28,215,330,65),string.IsNullOrWhiteSpace(skillQuery)?"Chưa có kỹ năng phù hợp.":"Không tìm thấy kỹ năng.",small);
            WithEnabled(skillPage>0,()=>{if(Button(new Rect(88,550,40,30),"←"))skillPage--;});
            Label(new Rect(137,554,130,25),$"{skillPage+1} / {HudCatalog.PageCount(list.Count,8)}  ·  {list.Count} kỹ năng",small);
            WithEnabled(skillPage+1<HudCatalog.PageCount(list.Count,8),()=>{if(Button(new Rect(294,550,40,30),"→"))skillPage++;});
            Paint(new Rect(395,125,1,460),new Color(.5f,.7f,.9f,.16f),Texture2D.whiteTexture);
            if(chosenSkill==null){Label(new Rect(430,180,720,70),"Nâng cơ quan có kỹ năng để mở thêm vào kho.");return;}
            var ability=chosenSkill.GrantedAbility;int level=organs.GetLevel(chosenSkill);
            Paint(new Rect(427,134,70,70),new Color(.18f,.32f,.43f),disc);
            Label(new Rect(427,147,70,40),AbilityMark(ability),new GUIStyle(heading){alignment=TextAnchor.MiddleCenter});
            Label(new Rect(515,133,680,25),$"{chosenSkill.DisplayName} · Cấp {level}",small);
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
                string mark=current!=null?AbilityMark(current.GrantedAbility):"+";
                if(Button(new Rect(947+slot*52,342,46,62),$"{mark}\n{slot+1}",current==chosenSkill))skills.Assign(target,chosenSkill);
            }
            int at=skills.SlotOf(chosenSkill);
            Label(new Rect(947,423,260,50),at<0?"Chưa gắn":$"Đang gắn ở ô {at+1}",small);
            WithEnabled(at>=0,()=>{if(Button(new Rect(947,489,245,35),"Gỡ kỹ năng"))skills.Clear(at);});
        }
        string AbilityMark(AbilitySO a)=>a is SpikeBurstAbility?"✦":a is ChargeAbility||a is BurstDashAbility?"↗":"◎";
        string ReadyFor(OrganSO o)
        {
            int slot=skills.SlotOf(o);
            return slot<0?"Chưa gắn":skills.CooldownRemaining(slot)>0?$"{skills.CooldownRemaining(slot):0.0}s":stats.State.Energy<o.GrantedAbility.EnergyCost?"Thiếu năng lượng":"Sẵn sàng";
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
            else if(ability is BurstDashAbility dash)
            {
                Label(new Rect(r.x,y,r.width,38),"Lướt nhanh",heading);y+=48;
                Label(new Rect(r.x,y,r.width,70),$"Lực đẩy: {dash.Force:0.#}\nVật cản có thể làm bạn dừng sớm.");y+=86;
            }
            else {Label(new Rect(r.x,y,r.width,90),ability.Description);y+=104;}
            Label(new Rect(r.x,y,r.width,72),$"Tốn {ability.EnergyCost:0.#} năng lượng · Hồi {ability.CooldownFor(level):0.##} giây\nHồi chiêu gốc {ability.Cooldown:0.##} giây{(level>=3?" × 0,75 ở cấp 3":"")}",small);
        }
        void DrawOrgansPanel()
        {
            if(organs==null||interactor==null){Label(new Rect(30,140,900,50),"Không có dữ liệu cơ quan.");return;}
            var list=organs.OrganList;
            if(chosenOrgan==null||!organs.Has(chosenOrgan)) {chosenOrgan=list.Count>0?list[0]:null;pendingDrop=null;}
            bool full=KeyPressed.TabHeld();organPage=HudCatalog.ClampPage(organPage,list.Count,4);
            Label(new Rect(25,128,190,25),$"CƠ QUAN · {list.Count}",small);
            for(int i=0;i<4;i++)
            {
                int index=organPage*4+i;if(index>=list.Count)break;var o=list[index];int level=organs.GetLevel(o);
                if(Button(new Rect(25,169+i*83,175,74),$"{o.DisplayName}\n{(level==0?"Mầm":$"Cấp {level}")}{(organs.Priority==o?" · Ưu tiên":"")}",chosenOrgan==o)){chosenOrgan=o;pendingDrop=null;}
            }
            WithEnabled(organPage>0,()=>{if(Button(new Rect(25,530,40,30),"←"))organPage--;});
            Label(new Rect(74,534,90,25),$"{organPage+1}/{HudCatalog.PageCount(list.Count,4)}",small);
            WithEnabled(organPage+1<HudCatalog.PageCount(list.Count,4),()=>{if(Button(new Rect(160,530,40,30),"→"))organPage++;});
            if(chosenOrgan==null){Label(new Rect(260,220,750,70),"Chưa có cơ quan. Tìm hoặc săn sinh vật để tiến hóa.");return;}
            float diagramWidth=full?230:450;
            Paint(new Rect(225,146,diagramWidth,418),new Color(.07f,.14f,.23f,.5f));
            Paint(new Rect(240,275,diagramWidth-30,137),new Color(.50f,.57f,.79f,.6f),disc);
            Label(new Rect(240,280,diagramWidth-30,110),"◎",new GUIStyle(heading){alignment=TextAnchor.MiddleCenter,fontSize=54});
            foreach(var node in list)
            {
                float nx=(node.Slot==OrganSlot.Eyes||node.Slot==OrganSlot.Back) ? .70f : .10f;
                float ny=node.Slot==OrganSlot.Eyes||node.Slot==OrganSlot.Skin?215:400;
                if(Button(new Rect(225+nx*(diagramWidth-20),ny,50,42),node.Slot==OrganSlot.Eyes?"◎":node.Slot==OrganSlot.Legs?"↗":node.Slot==OrganSlot.Back?"✦":"◇",chosenOrgan==node))
                {chosenOrgan=node;pendingDrop=null;}
            }
            Label(new Rect(240,452,diagramWidth-30,50),OrganInteractor.SlotName(chosenOrgan.Slot),center);
            float x=full?478:704,w=full?323:490;var organ=chosenOrgan;
            Label(new Rect(x,137,w,25),$"{organ.Tier} · {OrganInteractor.SlotName(organ.Slot)}",small);
            Label(new Rect(x,171,w,65),organ.DisplayName,heading);
            int lv=organs.GetLevel(organ);organs.GetProgress(organ,out float progress,out float cost);
            Label(new Rect(x,245,w,25),organs.IsMaxed(organ)?$"Cấp {lv} · Tối đa":$"{(lv==0?"Mầm":$"Cấp {lv}")} · {progress:0}/{cost:0} EXP",text);
            Paint(new Rect(x,281,w,5),new Color(.15f,.24f,.32f),Texture2D.whiteTexture);
            Paint(new Rect(x,281,w*(organs.IsMaxed(organ)?1:Frac(progress,cost)),5),cyan,Texture2D.whiteTexture);
            if(full)
            {
                Label(new Rect(x,306,w,87),organ.Description,small);
                string effects=$"Chỉ số gốc (chưa tính cấp):\nTốc độ: ×{organ.SpeedMultiplier:0.##}\nKháng nóng: +{organ.HeatResistance:0.#}°C · Phản đòn: {organ.ContactDamage:0.#}";
                Label(new Rect(x,397,w,80),effects,small);
                DrawBodyDossier(new Rect(831,135,382,447));
            }
            else if(organ.GrantedAbility!=null)Label(new Rect(x,307,w,90),$"{organ.GrantedAbility.DisplayName}\nMở ở cấp {organ.AbilityUnlockLevel}",small);
            float actionY=full?495:441;
            WithEnabled(!organs.IsMaxed(organ)&&stats.State.EvolutionExp>0,()=>{if(Button(new Rect(x,actionY,w*.58f,36),lv==0?"Nuôi cơ quan":"Nâng cấp"))organs.Invest(organ);});
            WithEnabled(!organs.IsMaxed(organ),()=>{if(Button(new Rect(x+w*.60f,actionY,w*.40f,36),organs.Priority==organ?"Bỏ ưu tiên":"Ưu tiên",organs.Priority==organ))organs.Priority=organs.Priority==organ?null:organ;});
            if(pendingDrop==organ)
            {
                if(Button(new Rect(x,actionY+47,w*.64f,33),"Tháo · Mất tiến độ nuôi")){pendingDrop=null;interactor.Drop(organ);chosenOrgan=null;}
                if(Button(new Rect(x+w*.67f,actionY+47,w*.33f,33),"Hủy"))pendingDrop=null;
            }
            else if(Button(new Rect(x,actionY+47,w,33),"Tháo cơ quan"))pendingDrop=organ;
        }
        void DrawBodyDossier(Rect r)
        {
            var s=stats.State;Label(new Rect(r.x,r.y,r.width,30),"CƠ THỂ & MÔI TRƯỜNG",small);
            string[] names={"Máu","Năng lượng","Độ no","Nước","Căng thẳng","Thân nhiệt","Không khí","Kháng nóng","Số mạng","Trú ẩn","Tốc độ","Phản sát thương"};
            string[] values={$"{s.Health:0}/{s.MaxHealth:0}",$"{s.Energy:0}/{s.MaxEnergy:0}",$"{s.Hunger:0}/{s.MaxHunger:0}",$"{s.Thirst:0}/{s.MaxThirst:0}",$"{s.Stress:0}",$"{s.Temperature:0}°C",$"{s.Ambient:0}°C",$"+{s.HeatResistance:0.#}°C",$"{s.Lives}/{s.MaxLives}",s.InShelter?"Trong tổ":"Ngoài tổ",$"×{organs.SpeedMultiplier:0.##}",$"{organs.ContactDamage:0.#}"};
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
