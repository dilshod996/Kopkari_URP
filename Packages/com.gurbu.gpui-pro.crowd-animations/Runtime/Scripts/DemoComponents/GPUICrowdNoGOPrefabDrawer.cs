// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace GPUInstancerPro.CrowdAnimations
{
    [ExecuteInEditMode]
    public class GPUICrowdNoGOPrefabDrawer : MonoBehaviour
    {
        public GPUICrowdInstance crowdInstancePrefab;
        public GPUIProfile profile;
        public int randomSeed = 42;
        public AnimationClip[] animationClips;

        [SerializeField, Range(0, 2000)]
        private int _instanceCount = 256;

        public Vector2 spacing = new Vector2(1, 1);
        public Quaternion rotation = Quaternion.identity;
        public bool3 randomRotation;
        public Vector3 scale = Vector3.one;
        public bool randomScale;
        public Vector2 randomScaleRange = new Vector2(0.5f, 3f);
        public Text instanceCountText;

        // =========================
        // ✅ Placement Mode
        // =========================
        public enum PlacementMode { Grid, Spline }
        public PlacementMode placementMode = PlacementMode.Grid;

        [Header("Spline Placement (Baked Points)")]
        [Tooltip("Spline Spawner (Staggart) chiqargan point Transformlar ro'yxati. Kamida 2 ta bo'lsin.")]
        public Transform[] bakedPoints;

        [Tooltip("Spline yo'nalishi bo'yicha burib qo'yadi")]
        public bool alignToSpline = true;

        [Tooltip("Yo'l kengligi (metr). 0 bo'lsa bitta chiziq bo'ladi.")]
        public float roadWidth = 0f;

        [Tooltip("RoadWidth bo'yicha chap-o'ng random tarqatadi")]
        public bool randomOffsetOnWidth = true;

        public float InstanceCount
        {
            set
            {
                _instanceCount = (int)value;
                if (_instanceCount < 0)
                    _instanceCount = 0;
                RegisterRenderers();
            }
        }

        private int _rendererKey;
        private GraphicsBuffer _colorBuffer;
        private int _currentInstanceCount;
        private const int MAX_INSTANCE_COUT = 2000;
        private Vector2 _currentSpacing;
        public enum RotationSource { SplineTangent, BakedPointRotation }
        [Header("Spline Rotation")]
        public RotationSource rotationSource = RotationSource.SplineTangent;

        // Prefab forward mos kelmasa (Z emas, X yoki -Z bo‘lsa)
        public Vector3 modelForwardEulerOffset = Vector3.zero;

        // Yo‘lga qarab turib, ozgina tarqalishi uchun
        [Range(0f, 60f)]
        public float randomYawRange = 0f;

        public void OnEnable() => RegisterRenderers();
        public void OnDisable() => DisposeRenderers();

        private void OnValidate()
        {
            if (GPUIRenderingSystem.IsActive && _rendererKey != 0)
                RegisterRenderers();
        }

        private void RegisterRenderers()
        {
            if (_instanceCount <= 0 || spacing != _currentSpacing)
                DisposeRenderers();

            if (_instanceCount > 0 && crowdInstancePrefab != null)
            {
                _instanceCount = Mathf.Min(_instanceCount, MAX_INSTANCE_COUT);

                if (_rendererKey == 0)
                {
                    GPUICoreAPI.RegisterRenderer(this, crowdInstancePrefab.gameObject, profile, out _rendererKey);
                    GPUICoreAPI.SetTransformBufferData(_rendererKey, GenerateMatrixArray(MAX_INSTANCE_COUT, _instanceCount));
                }

                GPUICoreAPI.SetTransformBufferData(_rendererKey, GenerateMatrixArray(MAX_INSTANCE_COUT, _instanceCount));
                GPUICoreAPI.SetInstanceCount(_rendererKey, _instanceCount);

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    return;
#endif
                GPUIAWComputeAnimator computeAnimator = GPUICrowdAPI.GetAnimatorWorkflow<GPUIAWComputeAnimator>();
                GPUICrowdAPI.SetAnimatorWorkflowForAll(_rendererKey, computeAnimator);

                if (animationClips != null && animationClips.Length > 0 && animationClips[0] != null)
                {
                    int clipCount = animationClips.Length;
                    UnityEngine.Random.InitState(randomSeed);

                    for (int i = _currentInstanceCount; i < _instanceCount; i++)
                    {
                        AnimationClip clip = animationClips[UnityEngine.Random.Range(0, clipCount)];
                        if (clip == null) continue;

                        computeAnimator.StartAnimation(_rendererKey, i, clip, UnityEngine.Random.Range(0f, 1f), 1f, 0f, true);
                    }
                }

                _currentInstanceCount = _instanceCount;
            }
            else
                _currentInstanceCount = 0;

            if (instanceCountText != null)
                instanceCountText.text = _currentInstanceCount.FormatNumberWithSuffix();
        }

        private void DisposeRenderers()
        {
            if (_rendererKey != 0)
            {
                GPUICoreAPI.DisposeRenderer(_rendererKey);
                _rendererKey = 0;
            }
            if (_colorBuffer != null)
            {
                _colorBuffer.Dispose();
                _colorBuffer = null;
            }
            _currentInstanceCount = 0;
        }

        private Matrix4x4[] GenerateMatrixArray(int totalCount, int visibleCount)
        {
            _currentSpacing = spacing;

            Matrix4x4[] matrix4X4s = new Matrix4x4[totalCount];
            Matrix4x4 matrix4X4 = Matrix4x4.TRS(Vector3.zero, rotation, scale);

            Vector3 originPos = transform.position;
            UnityEngine.Random.InitState(randomSeed);

            // Clamp visibleCount
            if (visibleCount < 0) visibleCount = 0;
            if (visibleCount > totalCount) visibleCount = totalCount;

            // ======================================
            // ✅ SPLINE MODE (baked points asosida)
            // ======================================
            if (placementMode == PlacementMode.Spline && bakedPoints != null && bakedPoints.Length > 0)
            {
                Quaternion modelOffsetRot = Quaternion.Euler(modelForwardEulerOffset);

                int pointCount = bakedPoints.Length;
                int spawnCount = Mathf.Min(visibleCount, pointCount);

                for (int i = 0; i < totalCount; i++)
                {
                    if (i >= spawnCount)
                    {
                        matrix4X4.SetTRS(new Vector3(999999, 999999, 999999), Quaternion.identity, Vector3.zero);
                        matrix4X4s[i] = matrix4X4;
                        continue;
                    }

                    Transform tp = bakedPoints[i];
                    Vector3 pos = tp ? tp.position : originPos;

                    Vector3 forward = transform.forward;

                    if (alignToSpline)
                    {
                        Vector3 prevPos = pos;
                        Vector3 nextPos = pos;

                        if (i > 0 && bakedPoints[i - 1] != null)
                            prevPos = bakedPoints[i - 1].position;

                        if (i < pointCount - 1 && bakedPoints[i + 1] != null)
                            nextPos = bakedPoints[i + 1].position;

                        if (i == 0 && pointCount > 1 && bakedPoints[i + 1] != null)
                            forward = (bakedPoints[i + 1].position - pos);
                        else if (i == pointCount - 1 && pointCount > 1 && bakedPoints[i - 1] != null)
                            forward = (pos - bakedPoints[i - 1].position);
                        else
                            forward = (nextPos - prevPos);

                        forward.y = 0f;

                        if (forward.sqrMagnitude < 0.0001f)
                            forward = transform.forward;
                        else
                            forward.Normalize();
                    }

                    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                    if (roadWidth > 0f)
                    {
                        float half = roadWidth * 0.5f;
                        float off = randomOffsetOnWidth ? UnityEngine.Random.Range(-half, half) : 0f;
                        pos += right * off;
                    }

                    Quaternion baseRot;


                    if (!alignToSpline)
                    {
                        baseRot = rotation;
                    }
                    else
                    {
                        float yRot;

                        if (tp != null)
                            yRot = tp.eulerAngles.y;
                        else
                            yRot = transform.eulerAngles.y;

                        baseRot = Quaternion.Euler(0f, yRot, 0f);

                        baseRot = baseRot * modelOffsetRot * rotation;

                        if (randomYawRange > 0.01f)
                        {
                            float yaw = UnityEngine.Random.Range(-randomYawRange, randomYawRange);
                            baseRot = baseRot * Quaternion.Euler(0f, yaw, 0f);
                        }
                    }

                    Vector3 sc = randomScale
                        ? scale * UnityEngine.Random.Range(randomScaleRange.x, randomScaleRange.y)
                        : scale;

                    matrix4X4.SetTRS(pos, baseRot, sc);
                    matrix4X4s[i] = matrix4X4;
                }

                return matrix4X4s;
            }

            // ======================================
            // ✅ GRID MODE (original)
            // ======================================
            int size = Mathf.CeilToInt(Mathf.Sqrt(totalCount));

            int[] dx = { 1, 0, -1, 0 };
            int[] dy = { 0, 1, 0, -1 };

            int x = 0, y = 0, index = 0, direction = 0;
            int steps = 1, stepIncreaseCounter = 0;

            while (index < totalCount)
            {
                for (int i = 0; i < steps; i++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        Vector3 pos = new Vector3(x * spacing.x, 0, y * spacing.y) + originPos;

                        if (index >= visibleCount)
                        {
                            matrix4X4.SetTRS(new Vector3(999999, 999999, 999999), Quaternion.identity, Vector3.zero);
                            matrix4X4s[index++] = matrix4X4;
                        }
                        else
                        {
                            if (randomRotation.x || randomRotation.y || randomRotation.z || randomScale)
                            {
                                Vector3 rotEuler = rotation.eulerAngles;
                                Quaternion rr = Quaternion.Euler(new Vector3(
                                    randomRotation.x ? UnityEngine.Random.Range(0, 360) : rotEuler.x,
                                    randomRotation.y ? UnityEngine.Random.Range(0, 360) : rotEuler.y,
                                    randomRotation.z ? UnityEngine.Random.Range(0, 360) : rotEuler.z
                                ));

                                Vector3 sc = randomScale
                                    ? scale * UnityEngine.Random.Range(randomScaleRange.x, randomScaleRange.y)
                                    : scale;

                                matrix4X4.SetTRS(pos, rr, sc);
                            }
                            else
                            {
                                matrix4X4.SetTRS(pos, rotation, scale);
                            }

                            matrix4X4s[index++] = matrix4X4;
                        }
                    }

                    x += dx[direction];
                    y += dy[direction];
                    if (index >= totalCount) break;
                }

                direction = (direction + 1) % 4;
                stepIncreaseCounter++;
                if (stepIncreaseCounter % 2 == 0) steps++;
            }

            return matrix4X4s;
        }

        public void SetInstanceCount(int instanceCount)
        {
            this._instanceCount = instanceCount;
            RegisterRenderers();
        }

        // ✅ Qulay: bakedPoints o'zgarsa, qo'lda refresh bosish uchun
        [ContextMenu("Refresh Crowd Placement")]
        private void RefreshCrowdPlacement()
        {
            if (_rendererKey != 0)
            {
                GPUICoreAPI.SetTransformBufferData(_rendererKey, GenerateMatrixArray(MAX_INSTANCE_COUT, _instanceCount));
                GPUICoreAPI.SetInstanceCount(_rendererKey, _instanceCount);
            }
            else
            {
                RegisterRenderers();
            }
        }
    }
}