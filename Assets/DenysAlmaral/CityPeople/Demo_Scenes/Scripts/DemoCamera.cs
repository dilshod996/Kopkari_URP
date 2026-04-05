using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CityPeople
{
    public class DemoCamera : MonoBehaviour
    {
        public AnimationCurve easingCurve;
        public float motionTimeTotal = 1.0f;
        public Camera[] cameraTargets;
        public GameObject TextLeft;
        public GameObject TextRight;
        public Text TextCharName;
        public string HintMessage;
        public List<Material> MaterialRef;
        public Texture[] Textures;

        private int currentCamera = 0;
        private Vector3 targetPosition;
        private Vector3 startPosition;
        private float motionTime = 2.1f;
        private Collider hoveredCollider;
        private Vector3 hoveredStoredPos;
        private Vector3 hoveredStoredScale;
        private Quaternion hoveredStoredRotation;
        private CityPeople hoveredPeople;
       

        void Start()
        {
            motionTime = motionTimeTotal + 0.1f;
            DisableArrows();
            if (cameraTargets.Length > 0)
            {
                var startCam = cameraTargets[currentCamera];
                if (startCam != null)
                {
                    transform.position = cameraTargets[currentCamera].transform.position;
                }
            }
            TextCharName.text = HintMessage;
        }

        void Update()
        {
            //Handle Automatic Camera Movement
            if (motionTime < motionTimeTotal)
            {
                motionTime += Time.deltaTime;
                var normalizedTime = motionTime / motionTimeTotal;
                var easedTime = easingCurve.Evaluate(normalizedTime);
                transform.position = Vector3.Lerp(startPosition, targetPosition, easedTime);
            }

            //Handle All Input (Keyboard + Mouse UI + Mouse 3D)
            HandleInput();

            //Handle Hover Effects
            MouseMove();
            HoverSpin();
        }

        private void HandleInput()
        {
            //Keyboard Input ---
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
                    OnRightClick();

                if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
                    OnLeftClick();
            }
#else
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                OnRightClick();

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                OnLeftClick();
#endif

            //Mouse Click Input (UI & 3D) ---
            bool isClick = false;
            Vector2 mousePos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                isClick = Mouse.current.leftButton.wasPressedThisFrame;
                mousePos = Mouse.current.position.ReadValue();
            }
#else
            isClick = Input.GetMouseButtonDown(0);
            mousePos = Input.mousePosition;
#endif

            if (isClick)
            {
                // Define the size of the UI "Hotzones" (1/6 of screen height)
                float uiZoneSize = Screen.height / 6.0f;

                // Check Bottom-Left Corner (Previous Camera)
                if (mousePos.x < uiZoneSize && mousePos.y < uiZoneSize)
                {
                    // Only trigger if we can actually go left
                    if (currentCamera > 0)
                        OnLeftClick();
                    return; // Stop here so we don't click through to the character
                }

                // Check Bottom-Right Corner (Next Camera)
                if (mousePos.x > (Screen.width - uiZoneSize) && mousePos.y < uiZoneSize)
                {
                    // Only trigger if we can actually go right
                    if (currentCamera < cameraTargets.Length - 1)
                        OnRightClick();
                    return; // Stop here
                }

                // If we are here, we clicked, but NOT on the UI.
                // Proceed to Character Selection Logic.
                TrySelectCharacter();
            }
        }

        private void HoverSpin()
        {
            if (hoveredPeople != null)
            {
                hoveredPeople.transform.Rotate(0, 90f * Time.deltaTime, 0);
            }
        }

        void MouseMove()
        {
            Vector3 mousePos;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null)
                return;

            Vector2 mousePos2D = Mouse.current.position.ReadValue();
            mousePos = new Vector3(mousePos2D.x, mousePos2D.y, 0f);
#else
        mousePos = Input.mousePosition;
#endif

            if (mousePos.x >= 0 && mousePos.x <= Screen.width &&
                mousePos.y >= 0 && mousePos.y <= Screen.height)
            {
                Ray ray = Camera.main.ScreenPointToRay(mousePos);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    Collider currentCollider = hit.collider;
                    if (currentCollider != hoveredCollider)
                    {
                        if (hoveredCollider != null)
                        {
                            ColliderExit(hoveredCollider);
                        }

                        ColliderEnter(currentCollider);
                        hoveredCollider = currentCollider;
                    }
                }
                else
                {
                    if (hoveredCollider != null)
                    {
                        ColliderExit(hoveredCollider);
                        hoveredCollider = null;
                    }
                }
            }
        }

        private void ColliderEnter(Collider currentCollider)
        {
            hoveredPeople = currentCollider.gameObject.GetComponent<CityPeople>();
            if (hoveredPeople != null)
            {
                if (TextCharName != null)
                {
                    TextCharName.text = $"{hoveredPeople.transform.parent.name} > {hoveredPeople.name} (mat: {hoveredPeople.CurrentPaletteName})";
                }
                var t = hoveredPeople.gameObject.transform;
                hoveredStoredPos = t.position;
                hoveredStoredScale = t.localScale;
                hoveredStoredRotation = t.rotation;
                t.position += new Vector3(0, 0, 1f);
                t.localScale = Vector3.one * 1.5f;
            }
        }

        private string CharacterInfo(CityPeople cityPeople)
        {
            string s = $"{cityPeople.transform.parent.name} > {cityPeople.name} ";
            var rends = cityPeople.GetComponentsInChildren<Renderer>();
            foreach (var rend in rends)
            {
              // var matName = mesh.sharedMaterial.name 
            }
            return s;
        }

        private void ColliderExit(Collider currentCollider)
        {
            hoveredPeople = currentCollider.gameObject.GetComponent<CityPeople>();
            if (hoveredPeople != null)
            {
                var t = hoveredPeople.gameObject.transform;
                t.position = hoveredStoredPos;
                t.localScale = hoveredStoredScale;
            }
            if (TextCharName != null)
            {
                TextCharName.text = HintMessage;
            }
            hoveredPeople = null;
        }

        void TrySelectCharacter()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
#else
    if (Input.GetMouseButtonDown(0))
#endif
            {
                if (hoveredPeople != null)
                {
                    var idx = MaterialRef.FindIndex(m => m.name == hoveredPeople.CurrentPaletteName);
                    idx += 1;
                    if (idx == MaterialRef.Count) idx = 0;

                    hoveredPeople.SetPalette(MaterialRef[idx]);

                    if (TextCharName != null)
                    {
                        TextCharName.text =
                            $"{hoveredPeople.transform.parent.name} > {hoveredPeople.name} (mat: {hoveredPeople.CurrentPaletteName})";
                    }
                }
            }
        }

        void GoTo(Camera cam)
        {
            startPosition = transform.position;
            targetPosition = cam.transform.position;
            motionTime = 0;
        }

        public void OnLeftClick()
        {
            if (currentCamera > 0)
            {
                currentCamera--;
                var nextCam = cameraTargets[currentCamera];
                GoTo(nextCam);
                DisableArrows();
            }
        }

        public void OnRightClick()
        {
            if (currentCamera < cameraTargets.Length - 1)
            {
                currentCamera++;
                var nextCam = cameraTargets[currentCamera];
                GoTo(nextCam);
                DisableArrows();
            }
        }



        private void DisableArrows()
        {
            if (TextRight != null && TextLeft != null)
            {
                TextRight.SetActive(currentCamera < cameraTargets.Length - 1);
                TextLeft.SetActive(currentCamera > 0);
            }

        }
    }

}
    
