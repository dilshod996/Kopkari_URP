using UnityEngine;
using MalbersAnimations;

namespace MalbersExtensions
{
    [AddComponentMenu("Malbers/Input/Mobile Joystick With Turn")]
    public class JoystickTurnMixer : MobileJoystick
    {
        private const string ControllerPrefsKey = "Racing_Controller_Type";

        [Header("Turn Buttons")]
        [SerializeField] private bool turnButtonsEnabled = true;
        [SerializeField] private TurnButton leftButton;
        [SerializeField] private TurnButton rightButton;

        [Header("Reins Drag")]
        [SerializeField] private bool reinsEnabled = false;
        [SerializeField] private ReinZone leftRein;
        [SerializeField] private ReinZone rightRein;

        [Header("Optional UI Roots")]
        [SerializeField] private GameObject buttonsRoot;
        [SerializeField] private GameObject reinsRoot;

        [Header("Turn Settings")]
        [Tooltip("Tugma/Rein bosilganda erishiladigan maksimal burilish (0–1)")]
        [Range(0.05f, 1f)]
        public float turnMaxAdd = 0.6f;

        [Tooltip("Bosilganda ohista ko‘tarilish tezligi (unit/sec)")]
        [Range(1f, 30f)]
        public float turnRampUp = 10f;

        [Tooltip("Qo‘yilganda darhol 0 ga qaytsin")]
        public bool instantRelease = true;

        [Range(1f, 30f)]
        public float turnRampDown = 14f;

        private float _turnAddX;
        private Vector2 _joyBase;

        private void Start()
        {
            //ApplySavedController();
        }

        private void OnEnable()
        {
            // ApplySavedController();
            RacingControllerSelecterUI.OnControllerSelected += SetControllerType;
            ResetTurnValue();
        }
        private void OnDestroy()
        {
            RacingControllerSelecterUI.OnControllerSelected -= SetControllerType;
        }
        public override void OnDrag(UnityEngine.EventSystems.PointerEventData point)
        {
            base.OnDrag(point);
            _joyBase = axisValue.Value;
        }

        private void LateUpdate()
        {
            float target = 0f;

            if (reinsEnabled)
            {
                float leftPull = leftRein != null && leftRein.IsHeld ? leftRein.Pull01 : 0f;
                float rightPull = rightRein != null && rightRein.IsHeld ? rightRein.Pull01 : 0f;

                float signed = Mathf.Clamp(rightPull - leftPull, -1f, 1f);
                target = signed * turnMaxAdd;
            }
            else if (turnButtonsEnabled)
            {
                bool left = leftButton != null && leftButton.IsPressed;
                bool right = rightButton != null && rightButton.IsPressed;

                int dir = 0;

                if (left && !right)
                    dir = -1;
                else if (right && !left)
                    dir = 1;

                target = dir * turnMaxAdd;
            }

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

            Vector2 final = _joyBase;
            final.x = Mathf.Clamp(final.x + _turnAddX, -1f, 1f);

            AxisValue = final;
        }

        public void ApplySavedController()
        {
            int savedValue = PlayerPrefs.GetInt(
                ControllerPrefsKey,
                (int)RacingControllerType.Buttons
            );

            RacingControllerType controllerType = savedValue == (int)RacingControllerType.Reins
                ? RacingControllerType.Reins
                : RacingControllerType.Buttons;

            SetControllerType(controllerType);
        }

        public void SetControllerType(RacingControllerType controllerType)
        {
            bool useReins = controllerType == RacingControllerType.Reins;
            bool useButtons = controllerType == RacingControllerType.Buttons;

            reinsEnabled = useReins;
            turnButtonsEnabled = useButtons;

            if (reinsRoot != null)
                reinsRoot.SetActive(useReins);

            if (buttonsRoot != null)
                buttonsRoot.SetActive(useButtons);

            ResetTurnValue();
        }

        public void SelectReinsController()
        {
            PlayerPrefs.SetInt(ControllerPrefsKey, (int)RacingControllerType.Reins);
            PlayerPrefs.Save();

            SetControllerType(RacingControllerType.Reins);
        }

        public void SelectButtonsController()
        {
            PlayerPrefs.SetInt(ControllerPrefsKey, (int)RacingControllerType.Buttons);
            PlayerPrefs.Save();

            SetControllerType(RacingControllerType.Buttons);
        }

        private void ResetTurnValue()
        {
            _turnAddX = 0f;
        }

        private void OnDisable()
        {
            ResetTurnValue();
        }
    }
}