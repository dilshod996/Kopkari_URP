using UnityEngine;
using Cinemachine;

public class ImpulseManager : MonoBehaviour
{
    private CinemachineBrain brain;
    private CinemachineVirtualCamera currentVCam;
    private CinemachineBasicMultiChannelPerlin noise;
    private CinemachineImpulseSource impulseSource;

    // Base (default) qiymatlar
    private float baseFOV;
    private float baseDutch;
    private float baseNoiseAmplitude;
    private bool baseInitialized = false;

    // Holatlar
    private bool isSprinting = false;
    private bool isBoosting = false;
    private int turnDir = 0; // -1 = left, 0 = center, +1 = right

    [Header("Sprint Settings")]
    [SerializeField] private float sprintFOVDelta = 5f;
    [SerializeField] private float sprintNoiseExtra = 1.5f;
    [SerializeField] private float sprintImpulsePower = 0.6f;

    [Header("Boost / Nitro Settings")]
    [SerializeField] private float boostFOVDelta = 8f;
    [SerializeField] private float boostRollExtra = 2f;
    [SerializeField] private float boostImpulsePower = 0.9f;

    [Header("Obstacle Hit Settings")]
    [SerializeField] private float obstacleImpulsePower = 2.5f;

    [Header("Rank Change Settings")]
    [SerializeField] private float rankImpulsePower = 0.5f;
    [SerializeField] private float rankSideKick = 0.4f;

    [Header("Turn Roll Settings")]
    [SerializeField] private float turnRollAngle = 4f;   // chap/o¡®ng qiyalash darajasi

    private void Awake()
    {
        brain = GetComponent<CinemachineBrain>();
    }

    private void OnEnable()
    {
        // Senda bor eventlarga moslab qo¡®ydim
        //UIButtonActions.OnSprintStart += OnSprintStart;
        //UIButtonActions.OnSprintEnd += OnSprintEnd;

        //BoostersContainer.OnSprintEffectStart += OnBoostStart; // Nitro/Sprint booster
        //BoostersContainer.OnSprintEffectEnd += OnBoostEnd;

       //HorseMine.OnObstacleHit += OnObstacleHit;

        //RacingEvents.OnRankedOnePlus += OnRankUp;
        //RacingEvents.OnRankedOneMinus += OnRankDown;

        // Turn¡¯ni event yoki buttondan o¡®zing chaqirishing mumkin:
        // Masalan: CameraEffectsManager.Instance.TurnLeft();
        // Hozircha tashqi eventga bog¡®lamay qo¡®yaman.
    }

    private void OnDisable()
    {
        //UIButtonActions.OnSprintStart -= OnSprintStart;
        //UIButtonActions.OnSprintEnd -= OnSprintEnd;

        //BoostersContainer.OnSprintEffectStart -= OnBoostStart;
        //BoostersContainer.OnSprintEffectEnd -= OnBoostEnd;

       // HorseMine.OnObstacleHit -= OnObstacleHit;

        //RacingEvents.OnRankedOnePlus -= OnRankUp;
        //RacingEvents.OnRankedOneMinus -= OnRankDown;
    }

    #region Camera cache + apply

    private void UpdateCurrentCamera()
    {
        if (brain == null || brain.ActiveVirtualCamera == null)
            return;

        var vcamGo = brain.ActiveVirtualCamera.VirtualCameraGameObject;

        currentVCam = vcamGo.GetComponent<CinemachineVirtualCamera>();
        if (currentVCam == null) return;

        noise = currentVCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        vcamGo.TryGetComponent(out impulseSource);

        if (!baseInitialized)
        {
            baseFOV = currentVCam.m_Lens.FieldOfView;
            baseDutch = currentVCam.m_Lens.Dutch;
            baseNoiseAmplitude = noise != null ? noise.m_AmplitudeGain : 0f;
            baseInitialized = true;
        }
    }

    /// <summary>
    /// Holatlarga (sprint/boost/turn) qarab FOV, Dutch va Noise¡¯ni bir marta yangilaydi
    /// </summary>
    private void ApplyCameraState()
    {
        UpdateCurrentCamera();
        if (currentVCam == null) return;

        var lens = currentVCam.m_Lens;

        // FOV
        float fov = baseFOV;
        if (isSprinting) fov += sprintFOVDelta;
        if (isBoosting) fov += boostFOVDelta;
        lens.FieldOfView = fov;

        // Dutch (roll)
        float dutch = baseDutch;
        dutch += turnDir * turnRollAngle;
        if (isBoosting && turnDir != 0)
        {
            // boost paytida biroz ko¡®proq qiyalash
            dutch += Mathf.Sign(turnDir) * boostRollExtra;
        }
        lens.Dutch = dutch;

        currentVCam.m_Lens = lens;

        // Noise
        if (noise != null)
        {
            float amp = baseNoiseAmplitude;
            if (isSprinting) amp += sprintNoiseExtra;
            noise.m_AmplitudeGain = amp;
        }
    }

    #endregion

    #region Impulse helpers

    private void GenerateDirectionalImpulse(float power, Vector3 direction)
    {
        UpdateCurrentCamera();
        if (impulseSource == null) return;

        impulseSource.GenerateImpulse(direction.normalized * power);
    }

    private void GenerateRadialImpulse(float power)
    {
        UpdateCurrentCamera();
        if (impulseSource == null) return;

        Vector3 dir = Random.onUnitSphere;
        impulseSource.GenerateImpulse(dir * power);
    }

    #endregion

    #region Event handlers

    // Sprint = Noise + Light Impulse + FOV Kick
    private void OnSprintStart()
    {
        isSprinting = true;
        ApplyCameraState();

        Vector3 dir = new Vector3(0f, 1f, -0.4f);
        GenerateDirectionalImpulse(sprintImpulsePower, dir);
    }

    private void OnSprintEnd()
    {
        isSprinting = false;
        ApplyCameraState();
    }

    // Boost / Nitro = FOV + slight roll + impulse
    private void OnBoostStart()
    {
        isBoosting = true;
        ApplyCameraState();

        Vector3 dir = new Vector3(0.1f, 1.1f, -0.6f);
        GenerateDirectionalImpulse(boostImpulsePower, dir);
    }

    private void OnBoostEnd()
    {
        isBoosting = false;
        ApplyCameraState();
    }

    // Obstacle Hit = Radial impulse
    private void OnObstacleHit()
    {
        GenerateRadialImpulse(obstacleImpulsePower);
    }

    // Rank Change = Light directional impulse (yon tomon)
    private void OnRankUp()
    {
        Vector3 dir = new Vector3(-rankSideKick, 1f, -0.3f);
        GenerateDirectionalImpulse(rankImpulsePower, dir);
    }

    private void OnRankDown()
    {
        Vector3 dir = new Vector3(rankSideKick, 1f, -0.3f);
        GenerateDirectionalImpulse(rankImpulsePower, dir);
    }

    #endregion

    #region Turn public methods (button / inputdan chaqirasan)

    public void TurnLeft()
    {
        turnDir = -1;
        ApplyCameraState();
    }

    public void TurnRight()
    {
        turnDir = 1;
        ApplyCameraState();
    }

    public void TurnCenter()
    {
        turnDir = 0;
        ApplyCameraState();
    }

    #endregion
}
