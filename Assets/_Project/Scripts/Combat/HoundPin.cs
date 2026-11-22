using Evolve.Abilities;
using Evolve.Survival;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Evolve.Combat
{
    /// <summary>
    /// (Also the golem's grab: <c>Begin(..., grab: true)</c> holds the player in a squeezing fist, to be shaken off by mashing Space, A or D; if the time runs out it slams them down for 22 damage.)
    /// The hound clamps its jaws on the player after a pounce that landed head first: it hangs on, the player is held still and bitten
    /// all the time, and must tap A and D one after the other to shake it off (<see cref="PinGame"/>). The dog is held (it does not walk or attack
    /// meanwhile); when the player breaks free the dog is thrown back and staggered, when the time runs out it lets go. Runs on the machine that simulates the dog;
    /// only the local player is pinned (a remote avatar just takes the first hit).
    /// </summary>
    public class HoundPin : MonoBehaviour
    {
        public static HoundPin Active { get; private set; }

        public PinGame Game { get; private set; }
        public bool Chomping => Game != null && !Game.Finished;

        CreatureCombat dog;
        SurvivalStats victim;
        Rigidbody dogBody;
        Vector3 offset;
        GUIStyle big, small;

        /// <summary>Start the pin if the player can be pinned (local, alive, not already gripping something, nothing else pinning).</summary>
        public static bool Begin(CreatureCombat dog, SurvivalStats victim, bool grab = false)
        {
            if (dog == null || victim == null || victim.IsRemote || victim.State.IsDead || Active != null || LatchSession.Active != null) return false;
            var pin = dog.gameObject.AddComponent<HoundPin>();
            pin.dog = dog; pin.victim = victim;
            pin.dogBody = dog.GetComponent<Rigidbody>();
            pin.Game = new PinGame(grab ? PinMode.Mash : PinMode.AlternateAD);
            Vector3 flat = dog.transform.position - victim.transform.position; flat.y = 0f;
            // stand back far enough that the camera behind the player is not inside the monster's body (a big golem 0.8 m away swallowed the view)
            var body = dog.GetComponentInChildren<Collider>();
            float reach = body != null ? Mathf.Max(body.bounds.extents.x, body.bounds.extents.z) + 1.2f : 1.2f;
            pin.offset = flat.sqrMagnitude > 1e-4f ? flat.normalized * Mathf.Max(reach, flat.magnitude) : -dog.transform.forward * reach;
            Active = pin;
            return true;
        }

        void Update()
        {
            if (Game == null || Game.Finished) return;
            var k = Keyboard.current;
            if (k == null) return;
            if (Game.Mode == PinMode.Mash)
            {
                if (k.spaceKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) Game.Press(PinKey.A);   // any of them: mash
                return;
            }
            if (k.aKey.wasPressedThisFrame) Game.Press(PinKey.A);
            if (k.dKey.wasPressedThisFrame) Game.Press(PinKey.D);
        }

        void FixedUpdate()
        {
            if (Game == null) return;
            if (victim == null || victim.State.IsDead || dog == null) { End(false); return; }
            float dmg = Game.Tick(Time.fixedDeltaTime);
            victim.State.Damage(dmg * dog.DamageScale);
            if (victim.TryGetComponent(out Evolve.Player.SlimeController sc)) sc.FreezeFor(0.3f);
            CreatureStatus.Stun(dog, 0.25f, false);   // the dog hangs on: it does not walk or attack while it chews
            if (dogBody != null)
            {
                Vector3 want = victim.transform.position + offset;
                want.y = dogBody.position.y;
                dogBody.MovePosition(Vector3.Lerp(dogBody.position, want, 0.4f));
                Vector3 look = victim.transform.position - dogBody.position; look.y = 0f;
                if (look.sqrMagnitude > 1e-4f) dogBody.MoveRotation(Quaternion.LookRotation(look.normalized));
            }
            if (Game.Finished) End(Game.Escaped);
        }

        void End(bool escaped)
        {
            if (Active == this) Active = null;
            if (dog != null)
            {
                dog.Stagger(escaped ? 1.2f : 0.5f);   // shaken off or let go: open to a counter for a moment
                if (!escaped && Game != null && Game.Mode == PinMode.Mash && victim != null && !victim.State.IsDead)   // not shaken off: the golem slams the player down
                {
                    victim.State.Damage(22f * dog.DamageScale);
                    Impulse.Kick(victim.GetComponent<Rigidbody>(), (victim.transform.position - dog.transform.position).normalized * 8f + Vector3.up * 3f);
                    ImpactFx.Thump(victim.transform.position, 0.35f, 0.7f, 25f);
                }
                if (escaped && victim != null) CreatureStatus.Knock(dog, (dog.transform.position - victim.transform.position).normalized * 6f, 0.3f);
            }
            Game = null;
            Destroy(this);
        }

        void OnDestroy() { if (Active == this) Active = null; }

        static void Fill(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        void OnGUI()
        {
            if (Game == null || Game.Finished) return;
            if (big == null)
            {
                big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 26, fontStyle = FontStyle.Bold };
                small = new GUIStyle(big) { fontSize = 15, fontStyle = FontStyle.Normal };
            }
            float w = Mathf.Min(520f, Screen.width * 0.6f), h = 24f, x = (Screen.width - w) * 0.5f, y = Screen.height - 150f;
            Fill(new Rect(x - 3, y - 3, w + 6, h + 6), new Color(0f, 0f, 0f, 0.55f));
            Fill(new Rect(x, y, w, h), new Color(0.15f, 0.15f, 0.18f, 0.9f));
            Fill(new Rect(x, y, w * Game.Progress, h), new Color(0.95f, 0.8f, 0.25f, 0.95f));
            var old = GUI.color; GUI.color = new Color(1f, 0.35f, 0.25f);
            GUI.Label(new Rect(x, y - 44f, w, 36f), Game.Mode == PinMode.Mash ? "BỊ BÓP! ẤN SPACE LIÊN TỤC ĐỂ THOÁT" : "CHÓ ĐANG CẮN! ẤN A RỒI D LIÊN TỤC", big);
            GUI.color = old;
            GUI.Label(new Rect(x, y + h + 4f, w, 22f), Game.Mode == PinMode.Mash ? "Ấn thật nhanh (Space, A hoặc D); thanh tụt rất nhanh, không thoát sẽ bị đập xuống" : "Ấn xen kẽ A và D (ấn một phím nhiều lần không có tác dụng)", small);
        }
    }
}
