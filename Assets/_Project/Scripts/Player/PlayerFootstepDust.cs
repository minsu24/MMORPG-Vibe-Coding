using UnityEngine;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement2D), typeof(PlayerEntity), typeof(CapsuleCollider2D))]
    [DefaultExecutionOrder(110)]
    public sealed class PlayerFootstepDust : MonoBehaviour
    {
        [Header("Footstep Timing")]
        [SerializeField] private string walkStateName = "PlayerWalk";
        [SerializeField, Range(0f, 0.5f)] private float stepPhaseOffset;
        [SerializeField, Min(0f)] private float minimumTravelPerStep = 0.12f;
        [Header("Small Ground Dust")]
        [SerializeField] private Material dustMaterial;
        [SerializeField] private Color dustColor = new Color(0.85f, 0.78f, 0.65f, 0.85f);
        [SerializeField, Range(1, 12)] private int particlesPerStep = 8;
        [SerializeField, Range(0.02f, 0.5f)] private float dustSize = 0.32f;
        [SerializeField, Range(0.1f, 0.6f)] private float dustLifetime = 0.45f;
        [SerializeField, Min(0f)] private float footSpread = 0.12f;

        private static Mesh puffMesh;
        private PlayerMovement2D movement;
        private PlayerEntity entity;
        private PlayerIdleAnimation animationController;
        private CapsuleCollider2D bodyCollider;
        private Animator animator;
        private ParticleSystem dust;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[16];
        private int walkStateHash;
        private int previousStep = int.MinValue;
        private Vector2 previousPosition;
        private float distanceSinceStep;
        private float lastMovementTime = float.NegativeInfinity;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement2D>();
            entity = GetComponent<PlayerEntity>();
            animationController = GetComponent<PlayerIdleAnimation>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            animator = GetComponentInChildren<Animator>();
            walkStateHash = Animator.StringToHash(walkStateName);
            previousPosition = transform.position;
            if (animator == null || dustMaterial == null)
            {
                enabled = false;
                return;
            }
            CreateDustSystem();
        }

        private void LateUpdate()
        {
            Vector2 position = movement.body != null ? movement.body.position : (Vector2)transform.position;
            float distance = Mathf.Abs(position.x - previousPosition.x);
            previousPosition = position;
            if (distance > Mathf.Max(1.5f, entity.Speed * 0.25f))
            {
                ResetSteps();
                lastMovementTime = float.NegativeInfinity;
                return;
            }
            if (distance > 0.0001f) lastMovementTime = Time.time;

            bool walking = Time.timeScale > 0f && movement.isActiveAndEnabled && movement.body != null
                && movement.IsGrounded && !movement.IsDashing && !movement.IsMovementLocked
                && !entity.IsDead && !entity.isKnockback
                && animationController != null && !animationController.IsAttacking
                && Mathf.Abs(movement.MoveInput) > 0.05f && Mathf.Abs(movement.body.linearVelocity.x) > 0.1f
                && Time.time - lastMovementTime <= Mathf.Max(0.06f, Time.fixedDeltaTime * 2f)
                && animator.isActiveAndEnabled && animator.isInitialized
                && animator.GetCurrentAnimatorStateInfo(0).shortNameHash == walkStateHash;
            if (!walking)
            {
                ResetSteps();
                return;
            }

            distanceSinceStep += distance;
            float phase = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            int step = Mathf.FloorToInt((phase - stepPhaseOffset) * 2f + 0.00001f);
            if (step == previousStep || distanceSinceStep < minimumTravelPerStep) return;
            previousStep = step;
            distanceSinceStep = 0f;
            float facing = animationController.FacingDirectionX < 0f ? -1f : 1f;
            float foot = (step & 1) == 0 ? -footSpread : footSpread;
            Vector2 origin = new Vector2(bodyCollider.bounds.center.x + foot * facing,
                bodyCollider.bounds.min.y + 0.15f);
            if (!TryFindGround(origin, out Vector2 contact)) return;
            EmitStep(contact, facing);
        }

        private bool TryFindGround(Vector2 origin, out Vector2 contact)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            int count = gameObject.scene.GetPhysicsScene2D().Raycast(origin, Vector2.down, 0.35f, filter, groundHits);
            float nearest = float.PositiveInfinity;
            contact = origin;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider == null || hit.normal.y < 0.65f
                    || hit.collider.GetComponentInParent<PlayerEntity>() != null
                    || hit.collider.GetComponentInParent<EnemyController>() != null
                    || Physics2D.GetIgnoreCollision(bodyCollider, hit.collider)
                    || hit.distance >= nearest) continue;
                nearest = hit.distance;
                contact = hit.point;
            }
            return !float.IsPositiveInfinity(nearest);
        }

        private void EmitStep(Vector2 contact, float facing)
        {
            for (int i = 0; i < particlesPerStep; i++)
            {
                var emit = new ParticleSystem.EmitParams
                {
                    position = new Vector3(contact.x + Random.Range(-0.15f, 0.15f),
                        contact.y + Random.Range(0.08f, 0.12f), transform.position.z),
                    velocity = new Vector3(-facing * Random.Range(0.3f, 0.75f), Random.Range(0.3f, 0.6f), 0f),
                    startSize = dustSize * Random.Range(0.65f, 1.15f),
                    startLifetime = dustLifetime * Random.Range(0.8f, 1.1f),
                    startColor = dustColor
                };
                dust.Emit(emit, 1);
            }
        }

        private void CreateDustSystem()
        {
            var child = new GameObject("Footstep Dust");
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            dust = child.AddComponent<ParticleSystem>();
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dust.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.05f;
            main.maxParticles = 48;
            main.startSpeed = 0f;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = dust.emission;
            emission.enabled = false;
            var shape = dust.shape;
            shape.enabled = false;
            var color = dust.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;
            var size = dust.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.5f));
            var renderer = dust.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            if (puffMesh == null) puffMesh = CreatePuffMesh();
            renderer.mesh = puffMesh;
            renderer.sharedMaterial = dustMaterial;
            var character = animator.GetComponent<SpriteRenderer>();
            if (character != null)
            {
                renderer.sortingLayerID = character.sortingLayerID;
                renderer.sortingOrder = character.sortingOrder + 1;
            }
            dust.Play();
        }

        private static Mesh CreatePuffMesh()
        {
            const int sides = 12;
            var vertices = new Vector3[sides + 1];
            var colors = new Color32[sides + 1];
            var uv = new Vector2[sides + 1];
            var triangles = new int[sides * 3];
            colors[0] = Color.white;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.5f;
                colors[i + 1] = new Color(1f, 1f, 1f, 0f);
                uv[i + 1] = new Vector2(vertices[i + 1].x + 0.5f, vertices[i + 1].y + 0.5f);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % sides + 1;
            }
            var mesh = new Mesh { name = "Soft Footstep Dust Puff" };
            mesh.vertices = vertices;
            // Particle mesh colors must use the packed color format.
            mesh.colors32 = colors;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void ResetSteps()
        {
            previousStep = int.MinValue;
            distanceSinceStep = 0f;
        }

        private void OnDisable()
        {
            ResetSteps();
            lastMovementTime = float.NegativeInfinity;
            previousPosition = transform.position;
            if (dust != null) dust.Clear();
        }
    }
}
