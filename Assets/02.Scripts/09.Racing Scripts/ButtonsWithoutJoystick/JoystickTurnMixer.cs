using UnityEngine;
using MalbersAnimations;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MalbersExtensions
{
    [AddComponentMenu("Malbers/Input/Mobile Joystick With Turn")]
    public class JoystickTurnMixer : MobileJoystick
    {
        private const string TrainingRacingSceneName = "TrainingRacing";

        [Header("Turn Buttons")]
        [SerializeField] private bool turnButtonsEnabled = true;
        [SerializeField] private TurnButton leftButton;
        [SerializeField] private TurnButton rightButton;

        [Header("Reins Drag")]
        [SerializeField] private bool reinsEnabled = false;
        [SerializeField] private ReinZone leftRein;
        [SerializeField] private ReinZone rightRein;

        [Header("Tilt Steering")]
        [SerializeField] private bool tiltEnabled = false;
        [SerializeField] private bool tiltInvert = false;
        [SerializeField] private bool calibrateTiltOnEnable = true;
        [SerializeField, Range(0f, 0.5f)] private float tiltDeadZone = 0.08f;
        [SerializeField, Range(0.1f, 5f)] private float tiltSensitivity = 2.2f;
        [SerializeField, Range(0.05f, 1f)] private float tiltMaxAdd = 0.68f;
        [SerializeField] private bool tiltRecalibrateAfterObstacle = true;
        [SerializeField, Range(0f, 1f)] private float tiltObstacleRecalibrateDelay = 0.15f;
        [SerializeField, Range(0f, 0.5f)] private float tiltObstacleRecalibrateNeutralZone = 0.12f;
        [SerializeField, Range(0.1f, 2f)] private float tiltObstacleRecalibrateTimeout = 0.75f;

        [Header("Optional UI Roots")]
        [SerializeField] private GameObject buttonsRoot;
        [SerializeField] private GameObject reinsRoot;
        [SerializeField] private GameObject tiltRoot;

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
        private float _tiltNeutralX;
        private float _tiltRecalibrateAt = -1f;
        private float _tiltRecalibrateCancelAt = -1f;
        private Vector2 _joyBase;

        private void Start()
        {
            ApplySavedControllerIfSelected();
        }

        private void OnEnable()
        {
            ApplySavedControllerIfSelected();
            RacingControllerSelecterUI.OnControllerSelected += SetControllerType;
            HorseMine.OnObstacleTouchedEvent -= OnObstacleTouched;
            HorseMine.OnObstacleTouchedEvent += OnObstacleTouched;
            EnableTiltSensorIfNeeded();
            CalibrateTilt();
            ResetTurnValue();
        }
        private void OnDestroy()
        {
            RacingControllerSelecterUI.OnControllerSelected -= SetControllerType;
            HorseMine.OnObstacleTouchedEvent -= OnObstacleTouched;
        }
        public override void OnDrag(UnityEngine.EventSystems.PointerEventData point)
        {
            base.OnDrag(point);
            _joyBase = axisValue.Value;

            if (tiltEnabled)
                _joyBase.x = 0f;
        }

        public override void OnPointerUp(UnityEngine.EventSystems.PointerEventData point)
        {
            base.OnPointerUp(point);
            _joyBase = Vector2.zero;
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
            else if (tiltEnabled)
            {
                if (CanUseTiltInput())
                {
                    ApplyPendingTiltRecalibration();
                    target = ReadTiltTurn() * tiltMaxAdd;
                }
                else
                {
                    ResetTiltInput();
                    return;
                }
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

            Vector2 final = tiltEnabled
                ? new Vector2(0f, _joyBase.y)
                : _joyBase;
            final.x = Mathf.Clamp(final.x + _turnAddX, -1f, 1f);

            AxisValue = final;
        }

        public void ApplySavedController()
        {
            if (ShouldForceReinsController())
            {
                SetControllerType(RacingControllerType.Reins);
                return;
            }

            SetControllerType(RacingControllerSelecterUI.GetSavedControllerOrDefault());
        }

        private void ApplySavedControllerIfSelected()
        {
            if (ShouldForceReinsController())
            {
                SetControllerType(RacingControllerType.Reins);
                return;
            }

            if (RacingControllerSelecterUI.HasSavedControllerSelection())
                ApplySavedController();
        }

        public void SetControllerType(RacingControllerType controllerType)
        {
            if (ShouldForceReinsController())
                controllerType = RacingControllerType.Reins;

            bool useReins = controllerType == RacingControllerType.Reins;
            bool useButtons = controllerType == RacingControllerType.Buttons;
            bool useTilt = controllerType == RacingControllerType.Tilt;

            if (useTilt && Accelerometer.current == null)
            {
                Debug.LogWarning($"{nameof(JoystickTurnMixer)}: Tilt controller selected, but no accelerometer is available. Falling back to button controls.", this);
                useTilt = false;
                useButtons = true;
            }

            reinsEnabled = useReins;
            turnButtonsEnabled = useButtons;
            tiltEnabled = useTilt;

            if (reinsRoot != null)
                reinsRoot.SetActive(useReins);

            if (buttonsRoot != null)
                buttonsRoot.SetActive(useButtons);

            if (tiltRoot != null)
                tiltRoot.SetActive(useTilt);

            if (useTilt)
            {
                EnableTiltSensorIfNeeded();
                CalibrateTilt();
                _joyBase = Vector2.zero;
            }

            ResetTurnValue();
        }

        private static bool ShouldForceReinsController()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == TrainingRacingSceneName)
                return true;

            Scene trainingScene = SceneManager.GetSceneByName(TrainingRacingSceneName);
            return trainingScene.IsValid() && trainingScene.isLoaded;
        }

        public void SelectReinsController()
        {
            PlayerPrefs.SetInt(RacingControllerSelecterUI.ControllerPrefsKey, (int)RacingControllerType.Reins);
            PlayerPrefs.Save();

            SetControllerType(RacingControllerType.Reins);
        }

        public void SelectButtonsController()
        {
            PlayerPrefs.SetInt(RacingControllerSelecterUI.ControllerPrefsKey, (int)RacingControllerType.Buttons);
            PlayerPrefs.Save();

            SetControllerType(RacingControllerType.Buttons);
        }

        public void SelectTiltController()
        {
            PlayerPrefs.SetInt(RacingControllerSelecterUI.ControllerPrefsKey, (int)RacingControllerType.Tilt);
            PlayerPrefs.Save();

            SetControllerType(RacingControllerType.Tilt);
        }

        public void CalibrateTilt()
        {
            if (!calibrateTiltOnEnable)
            {
                _tiltNeutralX = 0f;
                return;
            }

            _tiltNeutralX = Accelerometer.current != null
                ? Accelerometer.current.acceleration.ReadValue().x
                : 0f;
        }

        private void ResetTurnValue()
        {
            _turnAddX = 0f;
        }

        private void EnableTiltSensorIfNeeded()
        {
            if (Accelerometer.current != null && !Accelerometer.current.enabled)
            {
                InputSystem.EnableDevice(Accelerometer.current);
            }
        }

        private float ReadTiltTurn()
        {
            if (Accelerometer.current == null)
                return 0f;

            float rawX = Accelerometer.current.acceleration.ReadValue().x - _tiltNeutralX;

            if (tiltInvert)
                rawX *= -1f;

            float absX = Mathf.Abs(rawX);
            if (absX <= tiltDeadZone)
                return 0f;

            float normalized = Mathf.InverseLerp(tiltDeadZone, 1f, absX);
            return Mathf.Sign(rawX) * Mathf.Clamp01(normalized * tiltSensitivity);
        }

        private bool CanUseTiltInput()
        {
            RacingController controller = RacingController.Instance;

            return controller != null
                && controller.HasStarted
                && !controller.HasFinished
                && !controller.IsRaceOver;
        }

        private void ResetTiltInput()
        {
            _turnAddX = 0f;
            _joyBase = Vector2.zero;
            AxisValue = Vector2.zero;
        }

        private void OnObstacleTouched()
        {
            if (!tiltEnabled || !tiltRecalibrateAfterObstacle)
                return;

            _tiltRecalibrateAt = Time.time + tiltObstacleRecalibrateDelay;
            _tiltRecalibrateCancelAt = Time.time + tiltObstacleRecalibrateTimeout;
        }

        private void ApplyPendingTiltRecalibration()
        {
            if (_tiltRecalibrateAt < 0f || Time.time < _tiltRecalibrateAt)
                return;

            if (Accelerometer.current == null)
            {
                ClearPendingTiltRecalibration();
                return;
            }

            float rawX = Accelerometer.current.acceleration.ReadValue().x - _tiltNeutralX;
            if (Mathf.Abs(rawX) <= tiltObstacleRecalibrateNeutralZone)
            {
                ClearPendingTiltRecalibration();
                CalibrateTilt();
                return;
            }

            if (Time.time >= _tiltRecalibrateCancelAt)
                ClearPendingTiltRecalibration();
        }

        private void ClearPendingTiltRecalibration()
        {
            _tiltRecalibrateAt = -1f;
            _tiltRecalibrateCancelAt = -1f;
        }

        private void OnDisable()
        {
            RacingControllerSelecterUI.OnControllerSelected -= SetControllerType;
            HorseMine.OnObstacleTouchedEvent -= OnObstacleTouched;
            ResetTurnValue();
        }
    }
}
