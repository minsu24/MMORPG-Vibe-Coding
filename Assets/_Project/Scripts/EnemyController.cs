using UnityEngine;
using System.Collections;
using TMPro;
using EasternFantasy.Player;
using EasternFantasy.Inventory;

public abstract class EnemyController : Entity
{
    public enum DetectionShape
    {
        Circle,
        Rectangle
    }

    public event System.Action<EnemyController> Defeated;

    [SerializeField] private float _maxHP;
    [SerializeField] protected float _attackPower;
    [SerializeField] private float _moveSpeed;
    [Header("Detection")]
    [SerializeField] private DetectionShape detectionShape = DetectionShape.Circle;
    [Tooltip("Circle radius or rectangle horizontal half-width, measured from the monster.")]
    [SerializeField, Min(0.1f)] protected float _detectRange;
    [Tooltip("Rectangle vertical half-height. Only used when Detection Shape is Rectangle.")]
    [SerializeField, Min(0.1f)] private float detectionHalfHeight = 1.5f;

    [Header("Patrol")]
    [Tooltip("Horizontal distance from the spawn point in each direction. Zero keeps the monster still until it detects a player.")]
    [SerializeField, Min(0f)] private float patrolRange;
    [Tooltip("Random idle time between patrol moves.")]
    [SerializeField, Min(0f)] private float patrolMinWait = 0.3f;
    [SerializeField, Min(0f)] private float patrolMaxWait = 1.5f;
    [Tooltip("Minimum fraction of the monster's normal movement speed while patrolling.")]
    [SerializeField, Range(0.1f, 1f)] private float patrolMinSpeedFactor = 0.7f;

    [SerializeField] private float _reward_EXP;
    [SerializeField, Min(0)] private int _rewardYeopjeon = 5;
    [SerializeField] protected LayerMask _playerLayer;

    [Header("Hit Flash")]
    [SerializeField] private Material hitFlashMaterial;
    [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.08f;

    [SerializeField, Min(0f)] private float horizontalKnockbackPower = 2f;
    [SerializeField, Min(0f)] private float verticalKnockbackPower = 1f;

    private Material originalMaterial;
    private Coroutine hitFlashRoutine;

    public GameObject damageTextPrefab; // Inspector에서 프리팹 할당

    public Transform textSpawnPoint;    // 텍스트가 뜰 위치 (예: 몬스터 머리 위 빈 오브젝트)
    private Vector3 direction, moveDirection; //플레이어 따라가기 위한 벡터 값.
    protected GameObject player;
    protected PlayerMovement2D playerController;
    protected PlayerEntity playerEntity;
    protected PlayerProgression playerProgression;
    protected PlayerCurrency playerCurrency;
    protected SpriteRenderer spriteRenderer;
    protected Rigidbody2D rb;
    protected CapsuleCollider2D capsuleCollider2D;
    protected Animator animator;
    private bool hasMovingParameter;
    public bool isKnockback = false;
    protected bool isAttacking, inFarAttackRange = false;
    private bool isDefeated;
    private Vector2 patrolOrigin;
    private float patrolTargetX;
    private float patrolSpeed;
    private float nextPatrolTime;
    private bool hasPatrolTarget;
    private bool wasChasing;
    protected bool HasLivingPlayer => playerEntity != null && !playerEntity.IsDead;
    public override float maxHP => _maxHP;
    public override float maxMP => 0;
    public override float maxMental => 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        base.Setup();
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider2D = GetComponent<CapsuleCollider2D>();
        player = GameObject.FindGameObjectWithTag("Player");
        playerController = player.GetComponent<PlayerMovement2D>();
        playerEntity = player.GetComponent<PlayerEntity>();
        playerProgression = player.GetComponent<PlayerProgression>();
        playerCurrency = player.GetComponent<PlayerCurrency>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalMaterial = spriteRenderer.sharedMaterial;
        target = HasLivingPlayer ? playerEntity : null;
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.ResetTrigger("isDead");
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == "isMoving" && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    hasMovingParameter = true;
                    break;
                }
            }
        }
        Attack_Power = _attackPower;
        Speed = _moveSpeed;
        patrolOrigin = rb.position;
        nextPatrolTime = Time.time + Random.Range(0f, Mathf.Max(patrolMinWait, patrolMaxWait));

        // All monsters use the Enemy layer; they may overlap while still colliding with the world and player.
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
    }

    // Update is called once per frame
    void Update()
    {
        if (CanUseAbility())
        {
            MonsterAbility();
        }
        if (rb.linearVelocity.normalized.x == 0) // Idle과 Run 애니메이션 제어문
        {
            if (hasMovingParameter && !isDefeated)
                animator.SetBool("isMoving", false);
        }
        else
        {
            if (hasMovingParameter && !isDefeated)
                animator.SetBool("isMoving", true);
        }
    }
    void FixedUpdate()
    {
        target = HasLivingPlayer ? playerEntity : null;
        if (isDefeated || isKnockback)
            return;

        if (isAttacking || inFarAttackRange)
        {
            SetHorizontalVelocity(0f);
            return;
        }

        if (CanDetectLivingPlayer())
        {
            wasChasing = true;
            float deltaX = playerEntity.transform.position.x - transform.position.x;
            float directionX = Mathf.Abs(deltaX) < 0.05f ? 0f : Mathf.Sign(deltaX);
            SetHorizontalVelocity(directionX * Speed);
            FaceDirection(directionX);
            return;
        }

        if (wasChasing)
        {
            wasChasing = false;
            hasPatrolTarget = false;
            nextPatrolTime = Time.time + RandomPatrolWait();
        }

        Patrol();
    }

    protected bool CanDetectLivingPlayer()
    {
        if (!HasLivingPlayer)
            return false;

        Vector2 offset = playerEntity.transform.position - transform.position;
        if (detectionShape == DetectionShape.Rectangle)
        {
            return Mathf.Abs(offset.x) <= _detectRange
                && Mathf.Abs(offset.y) <= detectionHalfHeight
                && Physics2D.OverlapBox(transform.position,
                    new Vector2(_detectRange * 2f, detectionHalfHeight * 2f),
                    0f, _playerLayer) != null;
        }

        return offset.sqrMagnitude <= _detectRange * _detectRange
            && Physics2D.OverlapCircle(transform.position, _detectRange,
                _playerLayer) != null;
    }

    private void Patrol()
    {
        if (patrolRange <= 0f || Speed <= 0f)
        {
            SetHorizontalVelocity(0f);
            return;
        }

        if (hasPatrolTarget && Mathf.Abs(rb.position.x - patrolTargetX) <= 0.05f)
        {
            hasPatrolTarget = false;
            nextPatrolTime = Time.time + RandomPatrolWait();
        }

        if (!hasPatrolTarget && Time.time >= nextPatrolTime)
            ChoosePatrolTarget();

        if (!hasPatrolTarget)
        {
            SetHorizontalVelocity(0f);
            return;
        }

        float remaining = patrolTargetX - rb.position.x;
        float direction = Mathf.Sign(remaining);
        float stepSpeed = Mathf.Min(patrolSpeed, Mathf.Abs(remaining) / Time.fixedDeltaTime);
        SetHorizontalVelocity(direction * stepSpeed);
        FaceDirection(direction);
    }

    private void ChoosePatrolTarget()
    {
        float left = patrolOrigin.x - patrolRange;
        float right = patrolOrigin.x + patrolRange;
        float currentX = rb.position.x;

        if (currentX < left || currentX > right)
        {
            patrolTargetX = Mathf.Clamp(currentX, left, right);
        }
        else
        {
            float leftSpace = currentX - left;
            float rightSpace = right - currentX;
            bool moveRight = (leftSpace <= 0.05f && rightSpace > leftSpace) ||
                (rightSpace > 0.05f && Random.value < 0.5f);
            float available = moveRight ? rightSpace : leftSpace;
            float distance = Random.Range(Mathf.Min(0.5f, available), available);
            patrolTargetX = currentX + (moveRight ? distance : -distance);
        }

        patrolSpeed = Speed * Random.Range(patrolMinSpeedFactor, 1f);
        hasPatrolTarget = true;
    }

    private float RandomPatrolWait()
    {
        return Random.Range(Mathf.Min(patrolMinWait, patrolMaxWait),
            Mathf.Max(patrolMinWait, patrolMaxWait));
    }

    private void SetHorizontalVelocity(float horizontalVelocity)
    {
        rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
    }

    private void FaceDirection(float directionX)
    {
        if (Mathf.Approximately(directionX, 0f))
            return;

        Vector3 scale = transform.localScale;
        scale.x = directionX > 0f ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    public override void TakeDamage(float damage)
    {
        TakeDamage(damage, false);
    }

    public void TakeDamage(float damage, bool isCritical)
    {
        if (isDefeated || damage <= 0f)
        {
            return;
        }
        if (HP > 0)
        {
            HP -= damage;
            CombatImpactFeedback.Play(transform.position, isCritical);
            // 1. 데미지 텍스트 생성
            // 몬스터 머리 위 위치 기준, 약간의 랜덤성을 주면 글자가 겹치지 않아 더 자연스럽습니다.
            Vector3 spawnPosition = textSpawnPoint.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0, 0.3f), 0);
            GameObject textObj = Instantiate(damageTextPrefab, spawnPosition, Quaternion.identity);

            // 2. 데미지 수치 전달
            DamageText damageText = textObj.GetComponent<DamageText>();
    
            if (damageText != null)
            {
                damageText.Setup(damage, isCritical);
            }
        }
        if(HP<=0)
        {
            isDefeated = true;
            OnDefeated();
            return;
        }
        Debug.Log("적 HP : " + HP);

        if (hitFlashMaterial != null) 
        {
            if (hitFlashRoutine != null)
                {
                    StopCoroutine(hitFlashRoutine);
                }

            hitFlashRoutine = StartCoroutine(HitAnimation());
        }
    }

    protected virtual void OnDefeated()
    {
        capsuleCollider2D.isTrigger = true;
        rb.simulated = false;
        if (_reward_EXP > 0f && playerProgression != null)
            playerProgression.AddExperience(_reward_EXP);
        if (_rewardYeopjeon > 0 && playerCurrency != null)
            playerCurrency.Add(_rewardYeopjeon);
        Defeated?.Invoke(this);
        StartCoroutine(DeadAnimation());

    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (detectionShape == DetectionShape.Rectangle)
            Gizmos.DrawWireCube(transform.position,
                new Vector3(_detectRange * 2f, detectionHalfHeight * 2f, 0f));
        else
            Gizmos.DrawWireSphere(transform.position, _detectRange);

        if (patrolRange <= 0f)
            return;

        Vector3 center = Application.isPlaying ? (Vector3)patrolOrigin : transform.position;
        Vector3 left = center + Vector3.left * patrolRange;
        Vector3 right = center + Vector3.right * patrolRange;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(left, right);
        Gizmos.DrawWireSphere(left, 0.1f);
        Gizmos.DrawWireSphere(right, 0.1f);
    }
    
    private IEnumerator HitAnimation() 
    {
        spriteRenderer.sharedMaterial = hitFlashMaterial;

        yield return new WaitForSeconds(hitFlashDuration);

        spriteRenderer.sharedMaterial = originalMaterial;
        hitFlashRoutine = null;
    }

    private IEnumerator DeadAnimation()
    {
        
        animator.SetTrigger("isDead");
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);

    }

    public void ApplyKnockback(float directionX)
    {
        if (isDefeated || rb == null)
        return;

        StartCoroutine(KnockBackTRoutine());

        rb.linearVelocity = Vector2.zero;

        Vector2 knockbackForce = new Vector2(
            directionX * horizontalKnockbackPower,
            verticalKnockbackPower
        );

        rb.AddForce(knockbackForce, ForceMode2D.Impulse);
    }

    private IEnumerator KnockBackTRoutine()
    {
        isKnockback = true;
        yield return new WaitForSeconds(0.2f);
        isKnockback = false;
    }

    private void OnDisable()
    {
        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
            hitFlashRoutine = null;
        }

        if (spriteRenderer != null && originalMaterial != null)
            spriteRenderer.sharedMaterial = originalMaterial;
    }
    protected virtual bool CanUseAbility()
    {
        return !isDefeated && CanDetectLivingPlayer();
    }

    protected abstract void MonsterAbility();
}
