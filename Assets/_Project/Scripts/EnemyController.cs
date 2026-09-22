using UnityEngine;
using System.Collections;
using TMPro;
using EasternFantasy.Player;
using EasternFantasy.Inventory;

public abstract class EnemyController : Entity
{
    [SerializeField] private float _maxHP;
    [SerializeField] protected float _attackPower;
    [SerializeField] private float _moveSpeed;
    [SerializeField] protected float _detectRange;

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
    public bool isKnockback = false;
    protected bool isAttacking, inFarAttackRange = false;
    private bool isDefeated;
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
        target = playerEntity;
        animator = GetComponent<Animator>();
        Attack_Power = _attackPower;
        Speed = _moveSpeed;
    }

    void Start()
    {
        animator.SetBool("isDead", false);
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
            animator.SetBool("isMoving", false);
        }
        else
        {
            animator.SetBool("isMoving", true);
        }
    }
    void FixedUpdate()
    {
        if(isAttacking || inFarAttackRange || isKnockback || isDefeated) return;
        Collider2D detectPlayer = Physics2D.OverlapCircle(transform.position, _detectRange, _playerLayer);
        if(detectPlayer != null){
            if (detectPlayer.CompareTag("Player"))
            {
                if(rb.linearVelocityX >= 0)
                {
                    transform.localScale = new Vector3(-1, 1, 1);
                }
                else
                {
                    transform.localScale = new Vector3(1, 1, 1);
                }
                
                
                float directionX = 0f;

                if (player.transform.position.x > transform.position.x)
                {
                    directionX = 1f;  // 플레이어가 오른쪽에 있음
                }
                else if (player.transform.position.x < transform.position.x)
                {
                    directionX = -1f; // 플레이어가 왼쪽에 있음
                }

                // 2. X축은 계산된 이동 속도를 적용하고, Y축은 기존의 중력 낙하 속도를 그대로 보존합니다!
                rb.linearVelocity = new Vector2(directionX * _moveSpeed, rb.linearVelocity.y);
                }
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

    }

    public override void TakeDamage(float damage) // 데미지 계산
    {
        if (isDefeated || damage <= 0f)
        {
            return;
        }
        if (HP > 0)
        {
            HP -= damage;
            // 1. 데미지 텍스트 생성
            // 몬스터 머리 위 위치 기준, 약간의 랜덤성을 주면 글자가 겹치지 않아 더 자연스럽습니다.
            Vector3 spawnPosition = textSpawnPoint.position + new Vector3(Random.Range(-0.1f, 0.1f), 0, 0);
            GameObject textObj = Instantiate(damageTextPrefab, spawnPosition, Quaternion.identity);

            // 2. 데미지 수치 전달
            DamageText damageText = textObj.GetComponent<DamageText>();
    
            if (damageText != null)
            {
                damageText.Setup(damage);
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
        StartCoroutine(DeadAnimation());

    }

    private void OnDrawGizmosSelected()
    {
        // Gizmos 색상을 노란색으로 설정
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectRange);
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
        Collider2D detectPlayer = Physics2D.OverlapCircle(transform.position, _detectRange, _playerLayer);
        return detectPlayer != null;
    }

    protected abstract void MonsterAbility();
}
