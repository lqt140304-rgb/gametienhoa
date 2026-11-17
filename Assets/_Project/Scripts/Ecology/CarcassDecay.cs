using UnityEngine;

namespace Evolve.Ecology
{
    /// <summary>
    /// Pure decay model of a carcass: Fresh -> Rotten -> Humus -> Gone, driven by Tick(minutes).
    /// Fresh and Rotten can be eaten; Humus and Gone cannot. When it is Gone the leftover food (+10) goes back to the region.
    /// </summary>
    public class CarcassDecay
    {
        const float HumusBonus = 10f;

        readonly float freshMinutes, rottenMinutes, humusMinutes;
        float elapsed;

        public CarcassDecay(float freshMinutes = 3f, float rottenMinutes = 5f, float humusMinutes = 4f, float food = 30f)
        {
            this.freshMinutes = freshMinutes;
            this.rottenMinutes = rottenMinutes;
            this.humusMinutes = humusMinutes;
            FoodLeft = Mathf.Max(0f, food);
        }

        public CarcassStage Stage { get; private set; } = CarcassStage.Fresh;
        public float FoodLeft { get; private set; }

        /// <summary>Food handed to the region when the carcass is Gone (leftover food + 10). 0 before that.</summary>
        public float HumusFoodReleased => Stage == CarcassStage.Gone ? FoodLeft + HumusBonus : 0f;

        public void Tick(float minutes)
        {
            if (minutes <= 0f || Stage == CarcassStage.Gone) return;
            elapsed += minutes;
            if (elapsed >= freshMinutes + rottenMinutes + humusMinutes) Stage = CarcassStage.Gone;
            else if (elapsed >= freshMinutes + rottenMinutes) Stage = CarcassStage.Humus;
            else if (elapsed >= freshMinutes) Stage = CarcassStage.Rotten;
        }

        /// <summary>Eat up to <paramref name="want"/> food. Returns the amount actually eaten (0 for Humus/Gone).</summary>
        public float Eat(float want)
        {
            if (want <= 0f || (Stage != CarcassStage.Fresh && Stage != CarcassStage.Rotten)) return 0f;
            float eaten = Mathf.Min(want, FoodLeft);
            FoodLeft -= eaten;
            return eaten;
        }
    }
}
