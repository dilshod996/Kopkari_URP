using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.Reactions;
using MalbersAnimations.Scriptables;

namespace KopkariGame
{
    /// <summary>
    /// Lightweight rider controller for Kopkari game
    /// Replaces heavy MAnimal for mounted riders - much better for mobile performance
    /// Handles: mounting, animations, reactions, goat carcass interactions
    /// </summary>
    public class MountedRider : MonoBehaviour
    {
        [Header("Horse Reference")]
        [Tooltip("The horse this rider is mounted on")]
        public Transform horseTransform;

        [Tooltip("Mount point on the horse (saddle position)")]
        public Transform mountPoint;

        [Tooltip("Auto-find mount point by tag")]
        public string mountPointTag = "MountPoint";

        [Header("Animation")]
        [Tooltip("Rider's animator component")]
        public Animator animator;

        [Tooltip("Sync rider lean with horse movement")]
        public bool syncLeanWithHorse = true;

        [Tooltip("Lean smoothing speed")]
        public float leanSmoothSpeed = 5f;

        [Header("Goat Carcass System")]
        [Tooltip("Is this rider currently holding the goat?")]
        public bool hasGoat = false;

        [Tooltip("Transform where goat attaches to rider")]
        public Transform goatHoldPoint;

        [Tooltip("Current goat carcass object")]
        public GameObject goatCarcass;

        [Header("Reactions & Damage")]
        [Tooltip("Use Malbers Reaction system")]
        public bool useMalbersReactions = true;

        [Tooltip("Current health")]
        public float health = 100f;

        [Tooltip("Maximum health")]
        public float maxHealth = 100f;

        [Tooltip("Invincibility time after hit (seconds)")]
        public float invincibilityDuration = 1f;

        private bool isInvincible = false;
        private float invincibilityTimer = 0f;

        [Header("Performance")]
        [Tooltip("Update rider position every N frames (1=every frame)")]
        public int positionUpdateRate = 1;

        [Tooltip("Is this the player's rider? (updates every frame)")]
        public bool isPlayer = false;

        // Cached references
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private int frameCounter = 0;

        // Animation parameter hashes (cached for performance)
        private int hash_Horizontal;
        private int hash_Vertical;
        private int hash_HasGoat;
        private int hash_Grab;
        private int hash_Drop;
        private int hash_Hit;

        // Horse movement tracking for lean
        private Vector3 lastHorsePosition;
        private float currentLean = 0f;
        private float targetLean = 0f;

        void Awake()
        {
            // Cache animator if not assigned
            if (animator == null)
                animator = GetComponent<Animator>();

            // Cache animation parameter hashes
            if (animator != null)
            {
                hash_Horizontal = Animator.StringToHash("Horizontal");
                hash_Vertical = Animator.StringToHash("Vertical");
                hash_HasGoat = Animator.StringToHash("HasGoat");
                hash_Grab = Animator.StringToHash("Grab");
                hash_Drop = Animator.StringToHash("Drop");
                hash_Hit = Animator.StringToHash("Hit");
            }
        }

        void Start()
        {
            InitializeMounting();
        }

        /// <summary>
        /// Initialize mounting system
        /// </summary>
        private void InitializeMounting()
        {
            // Find horse if not assigned
            if (horseTransform == null)
            {
                // Try to find horse parent
                horseTransform = transform.parent;
            }

            // Find mount point
            if (mountPoint == null && horseTransform != null)
            {
                // Try to find by tag
                GameObject mountObj = GameObject.FindGameObjectWithTag(mountPointTag);
                if (mountObj != null)
                    mountPoint = mountObj.transform;
                else
                {
                    // Try to find child named "MountPoint" or "Saddle"
                    mountPoint = horseTransform.Find("MountPoint");
                    if (mountPoint == null)
                        mountPoint = horseTransform.Find("Saddle");
                }
            }

            // If still no mount point, create one at horse center
            if (mountPoint == null && horseTransform != null)
            {
                GameObject newMount = new GameObject("MountPoint_Auto");
                mountPoint = newMount.transform;
                mountPoint.SetParent(horseTransform);
                mountPoint.localPosition = Vector3.up * 1.5f; // Adjust height as needed
                Debug.LogWarning($"[MountedRider] Auto-created mount point for {gameObject.name}");
            }

            // Initialize horse position tracking
            if (horseTransform != null)
                lastHorsePosition = horseTransform.position;
        }

        void LateUpdate()
        {
            frameCounter++;

            // Player updates every frame, AI uses update rate
            bool shouldUpdate = isPlayer || (frameCounter % positionUpdateRate == 0);

            if (shouldUpdate)
            {
                UpdateRiderPosition();
                UpdateRiderAnimation();
            }

            // Always update invincibility timer
            if (isInvincible)
            {
                invincibilityTimer -= Time.deltaTime;
                if (invincibilityTimer <= 0f)
                    isInvincible = false;
            }
        }

        /// <summary>
        /// Update rider position to follow horse mount point
        /// </summary>
        private void UpdateRiderPosition()
        {
            if (mountPoint == null) return;

            // Smoothly move rider to mount point
            transform.position = mountPoint.position;
            transform.rotation = mountPoint.rotation;

            // Calculate lean based on horse movement
            if (syncLeanWithHorse && horseTransform != null)
            {
                Vector3 horseVelocity = (horseTransform.position - lastHorsePosition) / Time.deltaTime;

                // Calculate target lean based on turning
                float horizontalSpeed = Vector3.Dot(horseVelocity, horseTransform.right);
                targetLean = Mathf.Clamp(horizontalSpeed * 0.5f, -1f, 1f);

                // Smooth lean
                currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSmoothSpeed);

                lastHorsePosition = horseTransform.position;
            }
        }

        /// <summary>
        /// Update animator parameters based on horse movement
        /// </summary>
        private void UpdateRiderAnimation()
        {
            if (animator == null || horseTransform == null) return;

            // Get horse velocity and convert to local space
            Vector3 horseVelocity = (horseTransform.position - lastHorsePosition) / Time.deltaTime;
            Vector3 localVelocity = horseTransform.InverseTransformDirection(horseVelocity);

            // Update animator parameters
            animator.SetFloat(hash_Vertical, localVelocity.z * 0.2f); // Scale down for animation
            animator.SetFloat(hash_Horizontal, currentLean);
            animator.SetBool(hash_HasGoat, hasGoat);
        }

        #region Goat Carcass System

        /// <summary>
        /// Grab the goat carcass
        /// </summary>
        public void GrabGoat(GameObject goat)
        {
            if (hasGoat || goat == null) return;

            hasGoat = true;
            goatCarcass = goat;

            // Attach goat to rider
            if (goatHoldPoint != null)
            {
                goat.transform.SetParent(goatHoldPoint);
                goat.transform.localPosition = Vector3.zero;
                goat.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // Fallback: attach to rider root
                goat.transform.SetParent(transform);
                goat.transform.localPosition = Vector3.forward * 0.5f + Vector3.down * 0.5f;
            }

            // Disable goat physics while held
            Rigidbody goatRb = goat.GetComponent<Rigidbody>();
            if (goatRb != null)
            {
                goatRb.isKinematic = true;
                goatRb.useGravity = false;
            }

            // Trigger grab animation
            if (animator != null)
                animator.SetTrigger(hash_Grab);

            // Event callback
            OnGoatGrabbed?.Invoke();

            Debug.Log($"[MountedRider] {gameObject.name} grabbed the goat!");
        }

        /// <summary>
        /// Drop the goat carcass
        /// </summary>
        public void DropGoat()
        {
            if (!hasGoat || goatCarcass == null) return;

            hasGoat = false;

            // Detach goat
            goatCarcass.transform.SetParent(null);

            // Re-enable goat physics
            Rigidbody goatRb = goatCarcass.GetComponent<Rigidbody>();
            if (goatRb != null)
            {
                goatRb.isKinematic = false;
                goatRb.useGravity = true;

                // Add forward velocity when dropped
                if (horseTransform != null)
                {
                    Vector3 dropVelocity = horseTransform.forward * 3f + Vector3.up * 1f;
                    goatRb.velocity = dropVelocity;
                }
            }

            // Trigger drop animation
            if (animator != null)
                animator.SetTrigger(hash_Drop);

            // Event callback
            OnGoatDropped?.Invoke();

            GameObject droppedGoat = goatCarcass;
            goatCarcass = null;

            Debug.Log($"[MountedRider] {gameObject.name} dropped the goat!");
        }

        /// <summary>
        /// Force drop goat (when hit, stunned, etc)
        /// </summary>
        public void ForceDropGoat()
        {
            if (hasGoat)
            {
                DropGoat();
                OnGoatForcedDrop?.Invoke();
            }
        }

        #endregion

        #region Damage & Reactions

        /// <summary>
        /// Take damage from another rider's attack
        /// </summary>
        public void TakeDamage(float damage, Vector3 hitDirection)
        {
            if (isInvincible) return;

            health = Mathf.Max(0, health - damage);

            // Start invincibility
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;

            // Trigger hit animation
            if (animator != null)
                animator.SetTrigger(hash_Hit);

            // Drop goat if holding it
            if (hasGoat && damage > 20f) // Only drop on significant hits
                ForceDropGoat();

            // Trigger Malbers reaction if enabled
            if (useMalbersReactions)
            {
                // You can integrate Malbers Reaction system here
                TriggerMalbersReaction(damage);
            }

            // Event callback
            OnDamageTaken?.Invoke(damage);

            Debug.Log($"[MountedRider] {gameObject.name} took {damage} damage. Health: {health}");

            // Check for death
            if (health <= 0)
                Die();
        }

        /// <summary>
        /// Trigger Malbers Reaction system
        /// </summary>
        private void TriggerMalbersReaction(float damageAmount)
        {
            // Example: Get reaction component and trigger it
            var reactionComponent = GetComponent<MalbersAnimations.Reactions.MReaction>();
            if (reactionComponent != null)
            {
                // Trigger reaction based on damage
                reactionComponent.React(animator);
            }
        }

        /// <summary>
        /// Heal rider
        /// </summary>
        public void Heal(float amount)
        {
            health = Mathf.Min(maxHealth, health + amount);
            OnHealed?.Invoke(amount);
        }

        /// <summary>
        /// Die and respawn/disable
        /// </summary>
        private void Die()
        {
            // Drop goat
            if (hasGoat)
                ForceDropGoat();

            // Disable rider temporarily or trigger death animation
            OnDeath?.Invoke();

            Debug.Log($"[MountedRider] {gameObject.name} has been eliminated!");

            // You can implement respawn logic here
            // For now, just disable
            StartCoroutine(RespawnAfterDelay(5f));
        }

        private System.Collections.IEnumerator RespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // Respawn logic
            health = maxHealth;
            isInvincible = false;

            OnRespawn?.Invoke();
            Debug.Log($"[MountedRider] {gameObject.name} respawned!");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set the horse this rider is mounted on
        /// </summary>
        public void SetHorse(Transform horse, Transform mount = null)
        {
            horseTransform = horse;
            mountPoint = mount;

            if (mountPoint == null && horse != null)
                InitializeMounting();
        }

        /// <summary>
        /// Check if rider can grab goat
        /// </summary>
        public bool CanGrabGoat()
        {
            return !hasGoat && health > 0;
        }

        /// <summary>
        /// Get rider's current status
        /// </summary>
        public string GetStatus()
        {
            string status = $"Health: {health}/{maxHealth}";
            if (hasGoat) status += " | Has Goat";
            if (isInvincible) status += " | Invincible";
            return status;
        }

        #endregion

        #region Events

        // Events for game logic integration
        public System.Action OnGoatGrabbed;
        public System.Action OnGoatDropped;
        public System.Action OnGoatForcedDrop;
        public System.Action<float> OnDamageTaken;
        public System.Action<float> OnHealed;
        public System.Action OnDeath;
        public System.Action OnRespawn;

        #endregion

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            // Visualize mount point connection
            if (mountPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, mountPoint.position);
                Gizmos.DrawWireSphere(mountPoint.position, 0.1f);
            }

            // Visualize goat hold point
            if (goatHoldPoint != null)
            {
                Gizmos.color = hasGoat ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(goatHoldPoint.position, 0.15f);
            }
        }

        void OnDrawGizmosSelected()
        {
            // Show health bar in editor
            if (Application.isPlaying)
            {
                Vector3 healthBarPos = transform.position + Vector3.up * 2.5f;
                UnityEditor.Handles.Label(healthBarPos, GetStatus());
            }
        }
#endif
    }
}