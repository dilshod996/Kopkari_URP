using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace MalbersAnimations
{
    // [HelpURL("https://malbersanimations.gitbook.io/animal-controller/main-components/malbers-input")]
    [AddComponentMenu("Malbers/Input/Mouse Scroll")]
    public class MMouseScroll : MonoBehaviour
    {
        public UnityEvent OnScrollUp = new();
        public UnityEvent OnScrollDown = new();

        private float mousedelta = 0;

        private void Update()
        {
            if (Mouse.current != null)
            {
                float newDelta = Mouse.current.scroll.ReadValue().y;

                if (newDelta != mousedelta)
                {
                    mousedelta = newDelta;

                    if (mousedelta < 0)
                        OnScrollDown.Invoke();
                    else if (mousedelta > 0)
                        OnScrollUp.Invoke();
                }
            }
        }
    }
}