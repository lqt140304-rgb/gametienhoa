using UnityEngine;

namespace Evolve.Civilization
{
    /// <summary>
    /// Marks the house of one treefolk class: when the player is within 30 m (and the camera faces it) the class name and its
    /// role float above the building, like a signboard.
    /// </summary>
    public class ClassHouseSign : MonoBehaviour
    {
        public TreefolkClass Class;
        public float ShowDistance = 30f;
        public float Height = 5.5f;
        static GUIStyle nameStyle, roleStyle;

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 head = transform.position + Vector3.up * Height;
            if ((head - cam.transform.position).sqrMagnitude > ShowDistance * ShowDistance) return;
            Vector3 s = cam.WorldToScreenPoint(head);
            if (s.z <= 0f) return;
            if (nameStyle == null)
            {
                nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.88f, 0.55f) } };
                roleStyle = new GUIStyle(nameStyle) { fontSize = 13, fontStyle = FontStyle.Normal, normal = { textColor = new Color(0.92f, 0.9f, 0.82f) } };
            }
            float y = Screen.height - s.y;
            GUI.Label(new Rect(s.x - 150f, y - 22f, 300f, 26f), TreefolkClasses.Name(Class), nameStyle);
            GUI.Label(new Rect(s.x - 150f, y + 2f, 300f, 20f), TreefolkClasses.Role(Class), roleStyle);
        }
    }
}
