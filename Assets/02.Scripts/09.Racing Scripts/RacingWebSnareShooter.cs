using System.Collections;
using MalbersAnimations;
using MalbersAnimations.Weapons;
using UnityEngine;

public class RacingWebSnareShooter : MonoBehaviour
{
    [Header("Weapon Animation")]
    [SerializeField] private MWeaponManager weaponManager;
    [SerializeField] private HolsterID webSnareHolster;
    [SerializeField] private bool connectToUIEvents;
    [SerializeField] private bool listenToUIButtonActions = true;
    [SerializeField] private bool listenToKopkariMainUI;
    [SerializeField] private bool driveWeaponManager = true;
    [SerializeField] private bool setAimWhileHolding;
    [SerializeField] private bool sendAttackDownToWeaponManager = true;
    [SerializeField] private bool playAttackActionOnRelease = true;
    [SerializeField] private bool releaseWeaponInputWithoutLegacyProjectile = true;
    [SerializeField] private float attackActionResetDelay = 0.15f;

    [Header("Projectile")]
    [SerializeField] private GameObject webSnareProjectilePrefab;
    [SerializeField] private Transform shootOrigin;
    [SerializeField] private Transform aimDirectionSource;
    [SerializeField] private Transform fallbackForwardSource;
    [SerializeField] private float shootSpeed = 20f;
    [SerializeField] private float upForce = 7f;
    [SerializeField] private float projectileLifeTime = 4f;
    [SerializeField] private int projectilePrewarm = 4;
    [SerializeField] private int projectilePoolMaxSize = 20;
    [SerializeField] private Transform projectilePoolParent;

    [Header("Direction")]
    [SerializeField] private bool flattenDirection = true;
    [SerializeField] private Vector3 fallbackDirection = Vector3.forward;

    [Header("Trajectory Preview")]
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private GameObject trajectoryHitPoint;
    [SerializeField] private bool showTrajectoryWhileHolding = true;
    [SerializeField] private bool hideTrajectoryAfterShot = true;
    [SerializeField] private LayerMask trajectoryMask = ~0;
    [SerializeField] private QueryTriggerInteraction trajectoryTriggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField] private Vector3 trajectoryGravity = new Vector3(0f, -9.18f, 0f);
    [SerializeField] private float trajectoryStep = 0.04f;
    [SerializeField] private int trajectoryMaxSteps = 50;
    [SerializeField] private float trajectoryUpdateRate = 0.03f;

    private const int MaxTrajectoryBufferSize = 96;
    private readonly Vector3[] trajectoryPoints = new Vector3[MaxTrajectoryBufferSize];

    private bool isHoldingShoot;
    private float nextTrajectoryUpdateTime;
    private Coroutine resetAttackRoutine;

    public bool HandlesUIButtonActions => isActiveAndEnabled && connectToUIEvents && listenToUIButtonActions;
    public bool HandlesKopkariMainUI => isActiveAndEnabled && connectToUIEvents && listenToKopkariMainUI;

    private void Awake()
    {
        if (weaponManager == null)
            TryGetComponent(out weaponManager);

        if (fallbackForwardSource == null)
            fallbackForwardSource = transform;

        if (shootOrigin == null)
            shootOrigin = transform;

        if (webSnareProjectilePrefab != null)
        {
            SimplePool.CreatePool(
                webSnareProjectilePrefab,
                projectilePrewarm,
                projectilePoolMaxSize,
                expandable: true,
                parent: projectilePoolParent);
        }

        ConfigureTrajectoryLine();
        SetTrajectoryVisible(false);
    }

    private void OnEnable()
    {
        if (!connectToUIEvents)
            return;

        if (listenToUIButtonActions)
        {
            UIButtonActions.OnWebSnareBtnEnable += PrepareWeapon;
            UIButtonActions.OnWebSnareStart += BeginShoot;
            UIButtonActions.OnWebSnareFinish += FinishShoot;
        }

        if (listenToKopkariMainUI)
        {
            KopkariMainUI.OnWebSnareBtnEnable += PrepareWeapon;
            KopkariMainUI.OnWebSnareStart += BeginShoot;
            KopkariMainUI.OnWebSnareFinish += FinishShoot;
        }
    }

    private void OnDisable()
    {
        if (connectToUIEvents && listenToUIButtonActions)
        {
            UIButtonActions.OnWebSnareBtnEnable -= PrepareWeapon;
            UIButtonActions.OnWebSnareStart -= BeginShoot;
            UIButtonActions.OnWebSnareFinish -= FinishShoot;
        }

        if (connectToUIEvents && listenToKopkariMainUI)
        {
            KopkariMainUI.OnWebSnareBtnEnable -= PrepareWeapon;
            KopkariMainUI.OnWebSnareStart -= BeginShoot;
            KopkariMainUI.OnWebSnareFinish -= FinishShoot;
        }

        isHoldingShoot = false;
        SetTrajectoryVisible(false);
    }

    private void Update()
    {
        if (!showTrajectoryWhileHolding || !isHoldingShoot || trajectoryLine == null)
            return;

        if (Time.time < nextTrajectoryUpdateTime)
            return;

        nextTrajectoryUpdateTime = Time.time + trajectoryUpdateRate;
        UpdateTrajectory();
    }

    public void PrepareWeapon()
    {
        if (!driveWeaponManager || weaponManager == null || webSnareHolster == null)
            return;

        if (weaponManager.WeaponIsActive &&
            weaponManager.Weapon != null &&
            weaponManager.Weapon.HolsterID == webSnareHolster.ID)
            return;

        weaponManager.Holster_Equip(webSnareHolster);
    }

    public void BeginShoot()
    {
        isHoldingShoot = true;
        PrepareWeapon();

        if (driveWeaponManager && weaponManager != null)
        {
            if (setAimWhileHolding)
                weaponManager.Aim_Set(true);

            if (sendAttackDownToWeaponManager)
                weaponManager.MainAttack(true);
        }

        if (showTrajectoryWhileHolding)
        {
            SetTrajectoryVisible(true);
            UpdateTrajectory();
        }
    }

    public void FinishShoot()
    {
        if (!isHoldingShoot)
            return;

        isHoldingShoot = false;
        FireControlledProjectile();
        PlayWeaponReleaseAnimation();

        if (hideTrajectoryAfterShot)
            SetTrajectoryVisible(false);
    }

    public void FireControlledProjectile()
    {
        if (webSnareProjectilePrefab == null || shootOrigin == null)
            return;

        Vector3 dir = GetShootDirection();
        Vector3 origin = shootOrigin.position;
        Quaternion rotation = Quaternion.LookRotation(dir);

        GameObject go = SimplePool.Spawn(webSnareProjectilePrefab, origin, rotation, lifeTime: projectileLifeTime);
        if (go == null)
            return;

        if (go.TryGetComponent(out WebSnareProjectile projectile))
        {
            projectile.LaunchArc(dir, shootSpeed, upForce);
            return;
        }

        if (go.TryGetComponent(out Rigidbody rb))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.velocity = dir * shootSpeed + Vector3.up * upForce;
        }
    }

    private void PlayWeaponReleaseAnimation()
    {
        if (!driveWeaponManager || weaponManager == null)
            return;

        if (playAttackActionOnRelease)
            weaponManager.WeaponAction = Weapon_Action.Attack;

        if (releaseWeaponInputWithoutLegacyProjectile)
            ReleaseWeaponInputWithoutProjectile();
        else
            weaponManager.MainAttack(false);

        if (resetAttackRoutine != null)
            StopCoroutine(resetAttackRoutine);

        resetAttackRoutine = StartCoroutine(ResetWeaponActionAfterDelay());
    }

    private void ReleaseWeaponInputWithoutProjectile()
    {
        MShootable shootable = weaponManager.Weapon as MShootable;
        if (shootable == null)
        {
            weaponManager.MainAttack(false);
            return;
        }

        MShootable.Release_Projectile originalRelease = shootable.releaseProjectile;
        shootable.releaseProjectile = MShootable.Release_Projectile.Never;

        try
        {
            weaponManager.MainAttack(false);
        }
        finally
        {
            shootable.releaseProjectile = originalRelease;
        }
    }

    private IEnumerator ResetWeaponActionAfterDelay()
    {
        yield return new WaitForSeconds(attackActionResetDelay);

        if (weaponManager != null && weaponManager.WeaponAction == Weapon_Action.Attack)
            weaponManager.WeaponAction = setAimWhileHolding ? Weapon_Action.Aim : Weapon_Action.Idle;

        resetAttackRoutine = null;
    }

    private Vector3 GetShootDirection()
    {
        Transform source = aimDirectionSource != null ? aimDirectionSource : fallbackForwardSource;
        Vector3 dir = source != null ? source.forward : fallbackDirection;

        if (flattenDirection)
            dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = shootOrigin != null ? shootOrigin.forward : fallbackDirection;
            if (flattenDirection)
                dir.y = 0f;
        }

        if (dir.sqrMagnitude < 0.0001f)
            dir = fallbackDirection;

        return dir.normalized;
    }

    private void ConfigureTrajectoryLine()
    {
        if (trajectoryLine == null)
            return;

        trajectoryLine.useWorldSpace = true;
        trajectoryLine.positionCount = 0;
    }

    private void SetTrajectoryVisible(bool visible)
    {
        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = visible;
            if (!visible)
                trajectoryLine.positionCount = 0;
        }

        if (trajectoryHitPoint != null)
            trajectoryHitPoint.SetActive(visible);
    }

    private void UpdateTrajectory()
    {
        if (trajectoryLine == null || shootOrigin == null)
            return;

        Vector3 origin = shootOrigin.position;
        Vector3 velocity = GetShootDirection() * shootSpeed + Vector3.up * upForce;
        int count = BuildTrajectory(origin, velocity);

        trajectoryLine.positionCount = count;
        for (int i = 0; i < count; i++)
            trajectoryLine.SetPosition(i, trajectoryPoints[i]);
    }

    private int BuildTrajectory(Vector3 origin, Vector3 velocity)
    {
        int maxSteps = Mathf.Clamp(trajectoryMaxSteps, 2, MaxTrajectoryBufferSize);
        float step = Mathf.Max(0.001f, trajectoryStep);

        trajectoryPoints[0] = origin;
        Vector3 previous = origin;
        RaycastHit lastHit = default;
        bool hitSomething = false;
        int count = 1;

        for (int i = 1; i < maxSteps; i++)
        {
            float time = step * i;
            Vector3 point = origin + velocity * time + 0.5f * trajectoryGravity * time * time;

            if (Physics.Linecast(previous, point, out RaycastHit hit, trajectoryMask, trajectoryTriggerInteraction))
            {
                trajectoryPoints[count++] = hit.point;
                lastHit = hit;
                hitSomething = true;
                break;
            }

            trajectoryPoints[count++] = point;
            previous = point;
        }

        if (trajectoryHitPoint != null)
        {
            trajectoryHitPoint.SetActive(hitSomething);
            if (hitSomething)
            {
                trajectoryHitPoint.transform.position = lastHit.point;
                trajectoryHitPoint.transform.up = lastHit.normal;
            }
        }

        return count;
    }

    private void OnValidate()
    {
        projectilePrewarm = Mathf.Max(0, projectilePrewarm);
        projectilePoolMaxSize = Mathf.Max(1, projectilePoolMaxSize);
        shootSpeed = Mathf.Max(0f, shootSpeed);
        projectileLifeTime = Mathf.Max(0.1f, projectileLifeTime);
        trajectoryStep = Mathf.Max(0.001f, trajectoryStep);
        trajectoryMaxSteps = Mathf.Clamp(trajectoryMaxSteps, 2, MaxTrajectoryBufferSize);
        trajectoryUpdateRate = Mathf.Max(0.001f, trajectoryUpdateRate);
    }
}
