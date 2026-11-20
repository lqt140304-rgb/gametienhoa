using Evolve.Player;
using Evolve.Swallow;
using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>
    /// Giao diện chỗ gắn cho hệ thống kỹ năng (thở nước, bám vách, tiết axit, màng chịu nhiệt, Sóng Dò mạnh).
    /// Hệ kỹ năng sau này chỉ cần cài đặt interface này trên slime hoặc đăng ký vào SlimeCapabilityLookup.
    /// </summary>
    public interface ISlimeCapabilitySource
    {
        SlimeCapabilities GetCapabilities();
    }

    /// <summary>
    /// Bộ đọc khả năng mặc định: đọc khối lượng từ SlimeBody.Mass và quy đổi sang BodySize.ScaleForMass.
    /// Các kỹ năng chưa có trong bước 3 để mặc định false (có thể bật thử trong Inspector).
    /// </summary>
    public class DefaultSlimeCapabilitySource : MonoBehaviour, ISlimeCapabilitySource
    {
        [Header("Kỹ năng mở khóa chướng ngại (chờ gắn hệ kỹ năng)")]
        [SerializeField] bool canBreatheWater;
        [SerializeField] bool canClimbWall;
        [SerializeField] bool canSecreteAcid;
        [SerializeField] bool hasHeatMembrane;
        [SerializeField] bool hasStrongSonar;

        SlimeBody body;

        void Awake() => body = GetComponent<SlimeBody>();

        public SlimeCapabilities GetCapabilities()
        {
            if (body == null) body = GetComponent<SlimeBody>();
            float mass = body != null ? body.Mass : 10f;
            float scale = BodySize.ScaleForMass(mass);
            return new SlimeCapabilities(
                scale,
                canBreatheWater,
                canClimbWall,
                canSecreteAcid,
                hasHeatMembrane,
                hasStrongSonar);
        }
    }

    /// <summary>Tiện ích lấy SlimeCapabilities từ một Collider hoặc GameObject bất kỳ của người chơi.</summary>
    public static class SlimeCapabilityLookup
    {
        public static bool TryGet(Component target, out SlimeCapabilities caps)
        {
            caps = default;
            if (target == null) return false;

            var source = target.GetComponentInParent<ISlimeCapabilitySource>();
            if (source != null)
            {
                caps = source.GetCapabilities();
                return true;
            }

            var body = target.GetComponentInParent<SlimeBody>();
            if (body != null)
            {
                caps = new SlimeCapabilities(BodySize.ScaleForMass(body.Mass));
                return true;
            }

            return false;
        }
    }
}
