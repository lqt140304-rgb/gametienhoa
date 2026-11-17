using System;
using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Player;
using Evolve.Survival;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Evolve.Net
{
    /// <summary>
    /// Co-op 0.4b stage 2 (GAME_DESIGN 6.1): rescue teammates, blood transfer, spectator mode.
    /// A player out of lives is a corpse. Within 120 s a teammate can hold T next to it for 5 s to pull it back at
    /// 1 HP, and keep holding T to give blood (the rescuer pays 3 HP for each 1 HP given, faster and faster).
    /// After 120 s the corpse is lost: its organs drop (each 50% lost) and its owner spectates the living players.
    /// All numbers are prototype values.
    /// Messages ("evolve.rs", reliable): kind, target client id, value. The host forwards them to their target.
    /// </summary>
    public class RescueController : MonoBehaviour
    {
        const string Message = "evolve.rs";
        const byte KindRevive = 1, KindBlood = 2;

        const float Range = 3f;                 // metres between rescuer and corpse
        const float HoldSeconds = 5f;           // hold T to pull the corpse back
        const float CorpseSeconds = 120f;       // time to rescue before the corpse is lost
        const float BloodCost = 3f;             // rescuer pays this much for 1 HP given
        const int BloodMaxHp = 10;              // HP that can be given in one rescue
        const float FirstTick = 5f, LastTick = 0.5f; // seconds per HP, speeding up

        CoopManager coop;
        ThirdPersonCamera cam;

        // corpse side (this machine's player)
        float corpseSince = -1f;
        int spectateIndex = -1; // -1 = own body

        // rescuer side
        RemoteAvatar target;
        float progress, tick, reviveSentAt;
        bool revived;
        int given;
        string prompt = "";

        void Awake() => coop = GetComponent<CoopManager>();

        internal void RegisterMessages(CustomMessagingManager messaging)
        {
            messaging.RegisterNamedMessageHandler(Message, OnMessage);
        }

        void Update()
        {
            NetWorld.CoopActive = coop != null && coop.IsActive;
            prompt = "";
            if (!NetWorld.CoopActive) { StopSpectating(); ResetRescue(); corpseSince = -1f; return; }

            var me = coop.LocalStats;
            if (me == null) return;
            var st = me.State;

            if (st.IsCorpse) { ResetRescue(); UpdateCorpse(me, st); }
            else
            {
                corpseSince = -1f;
                StopSpectating();
                if (!st.IsDead) UpdateRescuer(me, st);
                else ResetRescue();
            }
        }

        // ---------------------------------------------------------------- corpse side

        void UpdateCorpse(SurvivalStats me, SurvivalState st)
        {
            if (corpseSince < 0f) corpseSince = Time.time;
            if (!st.Lost && Time.time - corpseSince >= CorpseSeconds)
            {
                st.Lost = true;
                var interactor = me.GetComponent<OrganInteractor>();
                if (interactor != null) interactor.DropAllOnDeath();
            }

            var alive = AliveRemotes();
            if (KeyPressed.Tab() && !SurvivalHUD.ExternalMenuOpen)
            {
                spectateIndex++;
                if (spectateIndex >= alive.Count) spectateIndex = st.Lost ? 0 : -1;
            }
            if (st.Lost && spectateIndex < 0) spectateIndex = 0;
            if (spectateIndex >= alive.Count) spectateIndex = alive.Count > 0 ? 0 : -1;

            if (cam == null) cam = FindAnyObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.Override = spectateIndex >= 0 && spectateIndex < alive.Count ? alive[spectateIndex].transform : null;
        }

        List<RemoteAvatar> AliveRemotes()
        {
            var list = new List<RemoteAvatar>();
            foreach (var a in coop.Remotes) if (a != null && !a.Dead) list.Add(a);
            return list;
        }

        void StopSpectating()
        {
            spectateIndex = -1;
            if (cam == null) cam = FindAnyObjectByType<ThirdPersonCamera>();
            if (cam != null && cam.Override != null) cam.Override = null;
        }

        // ---------------------------------------------------------------- rescuer side

        void ResetRescue()
        {
            target = null; progress = 0f; tick = 0f; revived = false; given = 0;
        }

        void UpdateRescuer(SurvivalStats me, SurvivalState st)
        {
            bool holding = KeyPressed.THeld() && !SurvivalHUD.BlocksInput(me.gameObject);

            if (target == null)
            {
                target = FindCorpseInRange(me.transform.position);
                if (target != null) { progress = 0f; tick = 0f; revived = false; given = 0; }
            }
            else
            {
                bool gone = target == null || Vector3.Distance(me.transform.position, target.transform.position) > Range + 1f;
                if (gone || target.Lost) { ResetRescue(); return; }
                if (revived && target.Dead && Time.time - reviveSentAt > 2f) { ResetRescue(); return; }
            }
            if (target == null) return;

            if (!revived)
            {
                if (Vector3.Distance(me.transform.position, target.transform.position) > Range) { ResetRescue(); return; }
                prompt = $"Giữ T để cứu {target.DisplayName}";
                if (!holding) { progress = 0f; return; }
                progress += Time.deltaTime;
                prompt = $"Đang cứu {target.DisplayName}... {Mathf.Min(100, Mathf.RoundToInt(progress / HoldSeconds * 100f))}%";
                if (progress >= HoldSeconds)
                {
                    Send(target.ClientId, KindRevive, 0f);
                    revived = true; reviveSentAt = Time.time; tick = 0f;
                }
                return;
            }

            // Revived: keep holding T to give blood.
            if (target.Dead) { prompt = "Đang kéo về..."; return; }
            if (given >= BloodMaxHp || target.Health >= target.MaxHealth) { ResetRescue(); target = null; return; }
            prompt = holding
                ? $"Đang truyền máu cho {target.DisplayName}: {given}/{BloodMaxHp} (mỗi 1 máu tốn {BloodCost:0} của bạn)"
                : $"Giữ T để truyền máu cho {target.DisplayName} ({given}/{BloodMaxHp})";
            if (!holding) return;

            float interval = Mathf.Lerp(FirstTick, LastTick, given / (float)BloodMaxHp);
            tick += Time.deltaTime;
            if (tick < interval) return;
            tick = 0f;
            if (st.Health <= BloodCost + 1f) { prompt = "Bạn quá yếu để truyền thêm máu"; return; }
            st.Drain(BloodCost);
            Send(target.ClientId, KindBlood, 1f);
            given++;
        }

        RemoteAvatar FindCorpseInRange(Vector3 from)
        {
            RemoteAvatar best = null; float bestD = Range;
            foreach (var a in coop.Remotes)
            {
                if (a == null || !a.Dead || a.Lives > 0 || a.Lost) continue;
                float d = Vector3.Distance(from, a.transform.position);
                if (d <= bestD) { bestD = d; best = a; }
            }
            return best;
        }

        // ---------------------------------------------------------------- messages

        void Send(ulong targetId, byte kind, float value)
        {
            var nm = coop.Nm;
            if (nm == null || !nm.IsListening) return;
            var w = new FastBufferWriter(32, Allocator.Temp);
            try
            {
                w.WriteValueSafe(kind);
                w.WriteValueSafe(targetId);
                w.WriteValueSafe(value);
                // A client sends to the host (which forwards); the host sends straight to the target.
                ulong to = nm.IsServer ? targetId : NetworkManager.ServerClientId;
                nm.CustomMessagingManager.SendNamedMessage(Message, to, w, NetworkDelivery.Reliable);
            }
            catch (Exception e) { Debug.LogException(e); }
            finally { w.Dispose(); }
        }

        void OnMessage(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out byte kind);
                reader.ReadValueSafe(out ulong targetId);
                reader.ReadValueSafe(out float value);
                var nm = coop.Nm;
                if (nm == null) return;
                if (nm.IsServer && targetId != nm.LocalClientId) { Send(targetId, kind, value); return; }
                Apply(kind, value);
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        void Apply(byte kind, float value)
        {
            var me = coop.LocalStats;
            if (me == null) return;
            var st = me.State;
            if (kind == KindRevive)
            {
                if (!st.IsCorpse || st.Lost) return;
                st.Revive(1f);
                corpseSince = -1f;
            }
            else if (kind == KindBlood) st.Heal(Mathf.Clamp(value, 0f, 2f));
        }

        // ---------------------------------------------------------------- HUD

        void OnGUI()
        {
            if (!NetWorld.CoopActive) return;
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18, richText = true };
            float w = Screen.width;

            var me = coop.LocalStats;
            if (me != null && me.State.IsCorpse)
            {
                var st = me.State;
                float y = Screen.height / 2f;
                if (st.Lost)
                {
                    GUI.Label(new Rect(0, y, w, 30), "Xác của bạn đã mất. Cơ quan đã rơi ra (mỗi cái 50% bị mất).", style);
                    GUI.Label(new Rect(0, y + 28, w, 30), SpectateLine(), style);
                }
                else
                {
                    float left = Mathf.Max(0f, CorpseSeconds - (Time.time - corpseSince));
                    GUI.Label(new Rect(0, y, w, 30), $"Chờ đồng đội cứu: còn {Mathf.CeilToInt(left)} giây", style);
                    GUI.Label(new Rect(0, y + 28, w, 30), "Cơ quan vẫn nằm trên xác. Hết giờ thì xác mất hẳn và cơ quan rơi ra.", style);
                    GUI.Label(new Rect(0, y + 56, w, 30), SpectateLine(), style);
                }
                return;
            }

            if (prompt.Length == 0) return;
            float cy = Screen.height * 0.62f;
            GUI.Label(new Rect(0, cy, w, 30), prompt, style);
            if (!revived && progress > 0f)
            {
                var bar = new Rect(w / 2f - 120f, cy + 32f, 240f, 10f);
                GUI.Box(bar, GUIContent.none);
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(progress / HoldSeconds), bar.height), Texture2D.whiteTexture);
            }
        }

        string SpectateLine()
        {
            var alive = AliveRemotes();
            if (alive.Count == 0) return "Không còn ai sống để quan sát.";
            if (spectateIndex >= 0 && spectateIndex < alive.Count)
                return $"Đang xem: {alive[spectateIndex].DisplayName}  (Tab: người tiếp theo)";
            return "Tab: xem đồng đội";
        }
    }
}
