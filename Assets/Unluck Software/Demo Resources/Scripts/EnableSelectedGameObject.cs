namespace UnluckSoftware
{
    // Demo script to preview gameobjects and particle systems
    // Please ignore the MinMaxCurve error, it is harmless.
    // !Gizmos must be enabled for this object.

    using UnityEngine;
    using UnityEngine.InputSystem;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    public class EnableSelectedGameObject : MonoBehaviour
    {
        [HideInInspector] public Transform prevSelect;
        [HideInInspector] public Transform currentSelect;
        [HideInInspector] public Transform prevSelectWrong;

        [Header("Gizmos must be enabled for preview to work.")]
        public int particleSystems;
        public bool disableMe;
        public float disableDelay;
        public string ignoreTag = "^^^^";
        public bool disableOnPlay;

        [Header("Slide Show")]
        public bool autoSlideShow;
        public float autoSlideShowDelay = 3f;

        [Header("New Input System Keys")]
        public Key nextKey = Key.N;
        public Key pauseKey = Key.P;

        public bool randomEnable = false;
        public bool repeat;

        int emitOnEnable = 1;
        int counter = 0;

        Transform randomEnablePlaceHolder;
        GameObject prevRandomBird;
        bool autoSlideShowStarted;

        public bool clearBeforeNext;
        public GameObject[] ignoreDisable;

        public bool IsGameObjectInArray(GameObject obj)
        {
            if (ignoreDisable == null || ignoreDisable.Length == 0) return false;

            foreach (GameObject item in ignoreDisable)
            {
                if (item == obj) return true;
            }

            return false;
        }

        void Start()
        {
            if (disableOnPlay)
            {
                gameObject.SetActive(false);
                return;
            }

            DisableAllChildren();

            randomEnablePlaceHolder = new GameObject("randomEnablePlaceHolder").transform;

            if (autoSlideShow)
            {
                DisableAllChildren();
            }
        }

        private void Update()
        {
            if (autoSlideShowStarted) return;

            if (WasKeyReleased(nextKey))
            {
                if (autoSlideShow)
                {
                    if (randomEnable)
                        InvokeRepeating(nameof(RandomModel), 0f, autoSlideShowDelay);
                    else
                        InvokeRepeating(nameof(NextModel), 0f, autoSlideShowDelay);

                    autoSlideShowStarted = true;
                }
                else
                {
                    if (randomEnable)
                        RandomModel();
                    else
                        NextModel();
                }
            }

            if (WasKeyReleased(pauseKey))
            {
                PauseParticles();
            }
        }

        private bool WasKeyReleased(Key key)
        {
            if (Keyboard.current == null) return false;

            return Keyboard.current[key].wasReleasedThisFrame;
        }

        void RandomModel()
        {
            if (transform.childCount == 0 && !repeat) return;

            if (transform.childCount == 0 && repeat)
            {
                while (randomEnablePlaceHolder.childCount > 0)
                {
                    randomEnablePlaceHolder.GetChild(0).parent = transform;
                }
            }

            if (prevRandomBird != null)
            {
                StopParticles();
                DisableGameObject(prevRandomBird);
                prevRandomBird.transform.SetParent(randomEnablePlaceHolder.transform);
            }

            if (transform.childCount == 0) return;

            int randomBird = Random.Range(0, transform.childCount);
            prevRandomBird = transform.GetChild(randomBird).gameObject;
            prevRandomBird.SetActive(true);
            currentSelect = prevRandomBird.transform;
        }

        void NextModel()
        {
            StopParticles();

            if (currentSelect != null && currentSelect.transform.parent == transform)
            {
                if (!repeat)
                    currentSelect.transform.SetParent(randomEnablePlaceHolder.transform);

                DisableGameObject(currentSelect.gameObject);
            }

            if (transform.childCount == 0) return;

            Transform next = transform.GetChild(counter % transform.childCount);

            next.gameObject.SetActive(true);

            prevSelect = currentSelect;
            currentSelect = next;

            if (repeat)
                counter++;
        }

        void StopParticles()
        {
            if (!currentSelect) currentSelect = prevSelect;
            if (!currentSelect) return;

            ParticleSystem pss = currentSelect.GetComponent<ParticleSystem>();
            if (!pss) return;

            if (pss.isPlaying)
                pss.Stop();

            if (clearBeforeNext)
                pss.Clear();
        }

        void PauseParticles()
        {
            if (!currentSelect) currentSelect = prevSelect;
            if (!currentSelect) return;

            ParticleSystem pss = currentSelect.GetComponent<ParticleSystem>();
            if (!pss) return;

            if (pss.isPaused)
                pss.Play();
            else
                pss.Pause();
        }

        void DisableAllChildren()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).transform.parent == transform)
                {
                    DisableGameObject(transform.GetChild(i).gameObject);
                }
            }
        }

        void DisableGameObject(GameObject go)
        {
            if (IsGameObjectInArray(go)) return;

            go.SetActive(false);
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (disableMe) return;
            if (Selection.activeTransform == null) return;

            Transform s = Selection.activeTransform;

            if (s == prevSelect) return;
            if (s == null) return;
            if (s == prevSelect) return;
            if (s == prevSelectWrong) return;
            if (s.parent == null) return;
            if (s.name.Contains(ignoreTag)) return;
            if (s.parent != transform) return;

            if (prevSelect != null)
            {
                DisableGameObject(prevSelect.gameObject);

                s.gameObject.SetActive(true);

                ParticleSystem pss = s.GetComponent<ParticleSystem>();
                if (pss)
                {
                    pss.time -= pss.time;

                    if (pss.isPlaying)
                        pss.Stop();
                }
            }

            prevSelect = s;

            CountParticles_Editor();
            PlayParticles_Editor();
        }

        void PlayParticles_Editor()
        {
            if (!prevSelect) return;

            ParticleSystem pss = prevSelect.GetComponent<ParticleSystem>();
            if (!pss) return;

            pss.Clear();

            if (pss.isPlaying)
                pss.Stop();

            if (!pss.isPlaying)
                pss.Play();

            pss.Emit(emitOnEnable);
        }

        void CountParticles_Editor()
        {
            ParticleSystem[] obj = transform.GetComponentsInChildren<ParticleSystem>(true);

            particleSystems = 0;

            for (int i = 0; i < obj.Length; i++)
            {
                if (obj[i].transform.parent != null)
                {
                    ParticleSystem p = obj[i].transform.parent.GetComponent<ParticleSystem>();

                    if (p == null)
                    {
                        particleSystems++;
                    }
                }
            }
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(EnableSelectedGameObject))]
    public class EnableSelectedGameObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Disable Children", GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.5f)))
            {
                DisableDirectChildren((EnableSelectedGameObject)target);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void DisableDirectChildren(EnableSelectedGameObject script)
        {
            Transform parent = script.transform;

            foreach (Transform child in parent)
            {
                child.gameObject.SetActive(false);
            }
        }
    }
#endif
}