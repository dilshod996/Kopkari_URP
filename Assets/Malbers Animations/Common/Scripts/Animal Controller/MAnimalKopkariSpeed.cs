using UnityEngine;

namespace MalbersAnimations.Controller
{
    public partial class MAnimal
    {
        /// <summary>
        /// Per-animal gameplay movement scale. It does not alter a shared MSpeedSet.
        /// </summary>
        public float KopkariMovementSpeedMultiplier { get; private set; } = 1f;

        public void SetKopkariMovementSpeedMultiplier(float multiplier)
        {
            KopkariMovementSpeedMultiplier = Mathf.Max(0f, multiplier);
        }
    }
}
