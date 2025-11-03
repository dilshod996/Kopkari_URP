using UnityEngine;
using MalbersAnimations;                // MobileJoystick uchun
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

        [Tooltip("Tugma bosilganda erishiladigan maksimal burilish (0–1)")]
        [Range(0.05f, 1f)] public float turnMaxAdd = 0.6f;
        [Tooltip("Bosilganda ohista ko‘tarilish tezligi (unit/sec)")]
        [Range(1f, 30f)] public float turnRampUp = 10f;
        [Tooltip("Qo‘yilganda darhol 0 ga qaytsin")]
        public bool instantRelease = true;
        [Range(1f, 30f)] public float turnRampDown = 14f;

        private float _turnAddX;
        private int _turnDir;
        private Vector2 _joyBase;

        public void TurnLeftDown() => _turnDir = -1;
        public void TurnRightDown() => _turnDir = +1;
        public void TurnUp() => _turnDir = 0;

        //protected override void Awake()
        //{
        //    base.Awake();
        //    _joyBase = Vector2.zero;
        //}

        public override void OnDrag(UnityEngine.EventSystems.PointerEventData point)
        {
            base.OnDrag(point);

            // bazaviy joystick qiymatini saqlab qolamiz
            _joyBase = axisValue.Value;
        }

        private void LateUpdate()
        {
            if (!turnButtonsEnabled) return;

            bool left = leftButton && leftButton.IsPressed;
            bool right = rightButton && rightButton.IsPressed;
            int dir = 0;
            if (left ^ right) dir = left ? -1 : +1;

            if (dir != 0)
            {
                float target = dir * turnMaxAdd;
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
            _turnDir = 0;
        }
    }
}
