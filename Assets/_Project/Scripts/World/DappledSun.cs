using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Sways the main (Directional) light a few degrees over about 70 s so the patches of light that fall through the
    /// root roof drift slowly. Only the rotation: the brightness belongs to MonochromeVision (it dims the sun without eyes).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class DappledSun : MonoBehaviour
    {
        float baseElevation, baseYaw;

        void Awake()
        {
            var e = transform.eulerAngles;
            baseElevation = e.x;
            baseYaw = e.y;
        }

        void Update()
        {
            transform.rotation = DappledSunMath.Rotation(baseElevation, baseYaw, Time.time, DappledSunMath.SwayAmplitude, DappledSunMath.SwayPeriod);
        }
    }
}
