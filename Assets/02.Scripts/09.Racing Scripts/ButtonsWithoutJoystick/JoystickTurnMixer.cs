using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.Scriptables;
using MalbersAnimations.Events;

namespace MalbersExtensions
{
    [AddComponentMenu("Malbers/Input/Mobile Joystick With Turn")]
    public class JoystickTurnMixer : MobileJoystick
    {
        [Header("Turn Buttons (Optional)")]
        [SerializeField] private bool turnButtonsEnabled = true;
        [SerializeField] private TurnButton leftButton;
        [SerializeField] private TurnButton rightButton;

        [Header("Reins Drag (Optional)")]
        [SerializeField] private bool reinsEnabled = true;
        [SerializeField] private ReinZone leftRein;
        [SerializeField] private ReinZone rightRein;

        [Tooltip("Tugma/Rein bosilganda erishiladigan maksimal burilish (0–1)")]
        [Range(0.05f, 1f)] public float turnMaxAdd = 0.6f;
        [Tooltip("Bosilganda ohista ko‘tarilish tezligi (unit/sec)")]
        [Range(1f, 30f)] public float turnRampUp = 10f;
        [Tooltip("Qo‘yilganda darhol 0 ga qaytsin")]
        public bool instantRelease = true;
        [Range(1f, 30f)] public float turnRampDown = 14f;

        private float _turnAddX;
        private Vector2 _joyBase;

        public override void OnDrag(UnityEngine.EventSystems.PointerEventData point)
        {
            base.OnDrag(point);
            _joyBase = axisValue.Value; // joystick bazasi
        }

        private void LateUpdate()
        {
            float target = 0f;

            // 1) Reins target (prioritet)
            if (reinsEnabled && leftRein && rightRein)
            {
                float leftPull = leftRein.IsHeld ? leftRein.Pull01 : 0f;
                float rightPull = rightRein.IsHeld ? rightRein.Pull01 : 0f;

                // right - left => [-1..+1]
                float signed = Mathf.Clamp(rightPull - leftPull, -1f, 1f);
                target = signed * turnMaxAdd;
            }
            // 2) Button fallback
            else if (turnButtonsEnabled)
            {
                bool left = leftButton && leftButton.IsPressed;
                bool right = rightButton && rightButton.IsPressed;

                int dir = 0;
                if (left ^ right) dir = left ? -1 : +1;

                target = dir * turnMaxAdd;
            }

            // Ramp (smooth)
            if (Mathf.Abs(target) > 0.0001f)
            {
                _turnAddX = Mathf.MoveTowards(_turnAddX, target, turnRampUp * Time.deltaTime);
            }
            else
            {
                _turnAddX = instantRelease
                    ? 0f
                    : Mathf.MoveTowards(_turnAddX, 0f, turnRampDown * Time.deltaTime);
            }

            var final = _joyBase;
            final.x = Mathf.Clamp(final.x + _turnAddX, -1f, 1f);
            AxisValue = final;
        }

        private void OnDisable()
        {
            _turnAddX = 0f;
        }
    }
}
