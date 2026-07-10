using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using MalbersAnimations.Events;
using MalbersAnimations.Scriptables;

namespace MalbersAnimations
{
    //[HelpURL("https://malbersanimations.gitbook.io/animal-controller/mobile/mobile-joystick")]
    //[AddComponentMenu("Malbers/Input/Mobile Joystick")]
    public class MobileJoystickX : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        [Tooltip("What mouse button to use for the joystick ")]
        public PointerEventData.InputButton Button = PointerEventData.InputButton.Left;

        [Tooltip("Inverts the Horizontal value of the joystick")]
        public bool invertX;
        [Tooltip("Inverts the Vertical value of the joystick")]
        public bool invertY;

        [Tooltip("If the Axis Magnitude is lower than this value then the Axis will zero out")]
        public FloatReference deathpoint = new FloatReference(0.1f);
        /// <summary>sensitivity for the X Axis</summary>
        public FloatReference sensitivityX = new FloatReference(0.05f);
        /// <summary>sensitivity for the Y Axis</summary>
        public FloatReference sensitivityY = new FloatReference(0.05f);

        [Tooltip("The Joystick Start position will be First click on the Area")]
        public bool Dynamic = false;

        [Tooltip("If the Joystick is not Moving it will stop moving the Axis ")]
        public BoolReference StopJoyStick = new BoolReference(false);

        /// <summary> Is the Joystick is being pressed.</summary>
        public BoolReference pressed;
        /// <summary>Variable to Store the XAxis and Y Axis of the JoyStick</summary>
        public Vector2Reference axisValue;
        private Vector2 DeltaDrag;

        //   [Header("Events")]
        public UnityEvent OnJoystickDown = new UnityEvent();
        public UnityEvent OnJoystickUp = new UnityEvent();
        public Vector2Event OnAxisChange = new Vector2Event();
        public FloatEvent OnXAxisChange = new FloatEvent();
        public FloatEvent OnYAxisChange = new FloatEvent();
        public BoolEvent OnJoystickPressed = new BoolEvent();

        private float BgXSize;
        private float BgYSize;

        public bool AxisEditor = true;
        public bool EventsEditor = true;
        public bool ReferencesEditor = true;
        [Tooltip("If true, then the joystick will not use the starting position as guide for calculating the movement axis")]
        public bool m_Drag = false;

        /// <summary>Lets use it to see if the mouse has not moved.Zero means that it moves</summary>
        private int DragRegistered;

        /// <summary>JoyStick Background</summary>
        public Graphic bg;

        /// <summary>Drag Area Background</summary>
        public Graphic DragRect;

        /// <summary>JoyStick Button</summary>
        public Graphic Jbutton;

        /// <summary>Mutliplier to </summary>
        private const float mult = 3;

        // =======================
        //  TURN BUTTON Qo‘shimcha (kamera yaw uchun additive X)
        // =======================


        [SerializeField]
        [Header("Turn Button Additive (Yaw)")]
        [Tooltip("Turn tugma ta’siri kuchi (0.1..1). Kam qiymat = kam sezgir")]
        [Range(0.1f, 1f)] private float turnButtonSensitivity = 0.25f;

        [SerializeField]
        [Tooltip("Turn tugma silliqlash (0.01..0.5). Katta qiymat = sekinroq so‘nish")]
        [Range(0.01f, 0.5f)] private float turnButtonSmoothing = 0.15f;

        [Tooltip("Turn qo‘shimcha X’ni cheklash (0.2..1).")]
        [Range(0.2f, 1f)] public float turnClamp = 0.25f;

        [Tooltip("Bosib turganda targetga yaqinlashish tezligi (unit/sec)")]
        [Range(0.1f, 5f)] public float turnAccel = 1f;

        [Tooltip("Qo‘yilganda 0 ga qaytish tezligi (unit/sec)")]
        [Range(0.1f, 5f)] public float turnDecel = 1.2f;

        // tugmadan kelayotgan yo‘nalish: -1 (chap), +1 (o‘ng), 0 (yo‘q)
        private float _turnHoldDir = 0f;
        private float _curAddX = 0f;     // silliqlangan qo‘shimcha X
        private float _velAddX = 0f;     // smooth damp velocity
        // =======================

        public bool Pressed
        {
            get => pressed;
            set { OnJoystickPressed.Invoke(pressed.Value = value); }
        }

        public Vector2 AxisValue
        {
            get => axisValue;
            set
            {
                if (invertX) value.x *= -1;
                if (invertY) value.y *= -1;
                axisValue.Value = value;
            }
        }

        public float XAxis => AxisValue.x;
        public float YAxis => AxisValue.y;

        void Start()
        {
            if (bg == null) bg = GetComponent<Graphic>();
            if (Jbutton == null) Jbutton = transform.GetChild(0).GetComponent<Graphic>();
            if (DragRect == null) DragRect = GetComponent<Graphic>();

            BgXSize = bg.rectTransform.sizeDelta.x;
            BgYSize = bg.rectTransform.sizeDelta.y;
        }
        void Update()
        {
            // 1) Target qo'shimcha X (± sensitivity)
            float targetAddX = _turnHoldDir * turnButtonSensitivity;

            // 2) Bir zumda sakramasligi uchun tezlikni cheklab yaqinlashamiz
            float rate = (_turnHoldDir != 0f) ? turnAccel : turnDecel;   // bosilganda tezlanish, qo‘yilganda sekinlashish
            _curAddX = Mathf.MoveTowards(_curAddX, targetAddX, rate * Time.deltaTime);

            // 3) Joystick bazasi + qo‘shimcha yaw
            var baseAxis = axisValue.Value;
            var combined = baseAxis;
            combined.x = Mathf.Clamp(baseAxis.x + _curAddX, -turnClamp, turnClamp);
            //combined.x *= 0.13f; // umumiy chiqish kuchini 40% ga tushiradi


            AxisValue = combined; // invertlar setterda

            // 4) Turning aktivmi?
            bool turningActive = Mathf.Abs(_curAddX) > 0.0001f || Mathf.Abs(_turnHoldDir) > 0.0001f;

            // 5) StopJoyStick faqat turning bo‘lmaganda nolga tushirsin
            if (StopJoyStick.Value && DragRegistered > 1 && AxisValue != Vector2.zero && !turningActive)
            {
                AxisValue = Vector2.zero;
                DragRegistered = 0;
                _turnHoldDir = 0f;
            }

            // 6) Eventlarni yuborish (joystick bosilgan YOKI turning aktiv bo‘lsa)
            if (Pressed || turningActive)
            {
                OnAxisChange.Invoke(axisValue);
                OnXAxisChange.Invoke(axisValue.Value.x);
                OnYAxisChange.Invoke(axisValue.Value.y);
                DragRegistered++;
            }
        }


        //void Update()
        //{
        //    // === 1) Turn qo‘shimchasini silliqlash ===
        //    float targetAddX = _turnHoldDir * turnButtonSensitivity;
        //    _curAddX = Mathf.SmoothDamp(_curAddX, targetAddX, ref _velAddX, turnButtonSmoothing);

        //    // === 2) Joystick bazaviy qiymati ===
        //    var baseAxis = axisValue.Value;

        //    // === 3) Turn qo‘shimchasini bazaga qo‘shamiz (faqat X / yaw) ===
        //    var combined = baseAxis;
        //    combined.x = Mathf.Clamp(baseAxis.x + _curAddX, -turnClamp, turnClamp);

        //    // NOTE: invertX/Y ni setterda qo‘llaymiz — shu sabab bu yerda qo‘llamaymiz.
        //    AxisValue = combined; // (setter invertlarni hisobga oladi)

        //    // StopJoyStick (sizdagi mantiq)
        //    bool turningActive = Mathf.Abs(_curAddX) > 0.0001f || Mathf.Abs(_turnHoldDir) > 0.0001f;
        //    if (StopJoyStick.Value && DragRegistered > 1 && AxisValue != Vector2.zero && !turningActive)
        //    {
        //        AxisValue = Vector3.zero;
        //        DragRegistered = 0;
        //        _turnHoldDir = 0f; // tugma ta'sirini ham so'ndirib qo'yamiz
        //    }

        //    // Eventlar: tugma ushlab turilganda ham voqealar emitsiya qilinadi

        //    if (Pressed || turningActive)
        //    {
        //        OnAxisChange.Invoke(axisValue);
        //        OnXAxisChange.Invoke(axisValue.Value.x);
        //        OnYAxisChange.Invoke(axisValue.Value.y);
        //        DragRegistered++;
        //    }
        //}

        private void OnDisable()
        {
            PointerUP();
            SafeTurnRelease();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SafeTurnRelease();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SafeTurnRelease();
        }

        private void SafeTurnRelease()
        {
            _turnHoldDir = 0f; // maqsad 0
            // _curAddX SmoothDamp orqali o‘zi silliq 0 ga boradi
        }

        // When draging is occuring this will be called every time the cursor is moved.
        public virtual void OnDrag(PointerEventData Point)
        {
            if (Point.button != Button) return; //Check if the Correct Mouse Click.. Right Left or Middle

            Vector2 TargetAxis = Vector2.zero;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bg.rectTransform, Point.position, Point.pressEventCamera, out Vector2 pos))
            {
                if (!m_Drag || Dynamic)
                {
                    pos.x /= BgXSize;              // Get the Joystick position on the 2 axes based on the Bg position.
                    pos.y /= BgYSize;

                    TargetAxis = new Vector3(pos.x * mult * sensitivityX, pos.y * mult * sensitivityY); // Position is relative to the  Bg.
                    TargetAxis = (TargetAxis.magnitude > 1.0f ? TargetAxis.normalized : TargetAxis);

                    Vector2 JButtonPos = new Vector2(TargetAxis.x * (BgXSize / mult), TargetAxis.y * (BgYSize / mult));
                    Jbutton.rectTransform.anchoredPosition = JButtonPos;
                }
                else
                {
                    Jbutton.rectTransform.anchoredPosition = pos;
                    var relative = pos - DeltaDrag;

                    TargetAxis = new Vector3(
                        relative.x * sensitivityX * Screen.width * 0.001f,
                        relative.y * sensitivityY * 0.001f * Screen.height);

                    DeltaDrag = pos;
                }
            }

            DragRegistered = 0;

            if (TargetAxis.magnitude <= deathpoint)
            {
                AxisValue = Vector2.zero;
            }
            else
            {
                AxisValue = TargetAxis;
            }

            // Eventlar Update ichida combined bilan emit qilinadi
        }

        // When the virtual analog's press occured this will be called.
        public virtual void OnPointerDown(PointerEventData Point)
        {
            if (Point.button != Button) return; //Check if the Correct Mouse Click.. Right Left or Middle

            OnJoystickDown.Invoke();
            Pressed = true;

            DeltaDrag = Vector2.zero;

            if (Dynamic && !m_Drag)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(DragRect.rectTransform, Point.position, Point.pressEventCamera, out Vector2 DeltaDrag))
                {
                    DeltaDrag.x -= DragRect.rectTransform.sizeDelta.x;
                    DeltaDrag.y -= DragRect.rectTransform.sizeDelta.y;
                    bg.rectTransform.anchoredPosition = DeltaDrag;
                }
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(bg.rectTransform, Point.position, Point.pressEventCamera, out DeltaDrag);
            }
            OnDrag(Point);
        }

        // When the virtual analog's release occured this will be called.
        public virtual void OnPointerUp(PointerEventData Point)
        {
            if (Point.button != Button) return; //Check if the Correct Mouse Click.. Right Left or Middle
            PointerUP();
        }

        private void PointerUP()
        {
            OnJoystickUp.Invoke();
            Pressed = false;
            AxisValue = Vector2.zero;
            Jbutton.rectTransform.anchoredPosition = Vector3.zero;
            DeltaDrag = Vector2.zero;

            // turn tugma additive-ni ham so‘ndirib qo‘yamiz (silliq so‘nish davom etadi)
            _turnHoldDir = 0f;

            OnAxisChange.Invoke(axisValue);
            OnXAxisChange.Invoke(axisValue.Value.x);
            OnYAxisChange.Invoke(axisValue.Value.y);
        }

        // =======================
        //  TURN BUTTON uchun PUBLIC metodlar
        //  UI Button -> OnPointerDown: TurnButtonDown(-1 yoki +1)
        //  UI Button -> OnPointerUp:   TurnButtonUp()
        // =======================
        public void TurnLeftDown() => _turnHoldDir = -1f;
        public void TurnRightDown() => _turnHoldDir = 1f;

        public void TurnButtonDown(float dir)
        {
            _turnHoldDir = Mathf.Clamp(dir, -1f, 1f);
        }

        public void TurnButtonUp()
        {
            _turnHoldDir = 0f;
        }
    }
}
