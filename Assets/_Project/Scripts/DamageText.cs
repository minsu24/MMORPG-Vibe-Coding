using DG.Tweening;
using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    public float moveSpeed = 2.0f;     // 위로 올라가는 속도
    public float destroyTime = 1.0f;   // 텍스트가 사라지기까지의 시간
    public float alphaSpeed = 2.0f;    // 투명해지는 속도

    private TextMeshPro textMesh;
    [Header("DOTween pop effect")]
    [SerializeField, Min(1f)] private float popScale = 1.18f;
    [SerializeField, Min(1f)] private float criticalPopScale = 1.4f;
    [SerializeField, Min(0.01f)] private float popDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float settleDuration = 0.16f;

    private Color normalColor;
    private Vector3 normalScale;
    private float normalFontSize;
    private bool critical;
    private Sequence animation;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        normalColor = textMesh.color;
        normalScale = transform.localScale;
        normalFontSize = textMesh.fontSize;
    }

    void Start()
    {
        float lifetime = Mathf.Max(0.01f, destroyTime);
        float riseDuration = Mathf.Min(Mathf.Max(0.01f, popDuration), lifetime * 0.25f);
        float returnDuration = Mathf.Min(Mathf.Max(0.01f, settleDuration), lifetime * 0.25f);
        float fadeDuration = alphaSpeed > 0f
            ? Mathf.Min(lifetime * 0.65f, 1f / alphaSpeed)
            : lifetime * 0.65f;
        float speed = moveSpeed * (critical ? 1.35f : 1f);
        float drift = critical ? Random.Range(-0.7f, 0.7f) : 0f;
        Vector3 destination = transform.position
            + (transform.up * speed + Vector3.right * drift) * lifetime;

        transform.localScale = normalScale * 0.65f;
        animation = DOTween.Sequence();
        animation.Append(transform.DOScale(normalScale * (critical ? criticalPopScale : popScale),
            riseDuration).SetEase(Ease.OutCubic));
        animation.Append(transform.DOScale(normalScale, returnDuration).SetEase(Ease.OutQuad));
        animation.Insert(0f, transform.DOMove(destination, lifetime).SetEase(Ease.OutQuad));
        // Keep the number legible first, then fade completely before destroying it.
        animation.Insert(lifetime - fadeDuration,
            DOTween.To(() => textMesh.color.a, SetAlpha, 0f, fadeDuration).SetEase(Ease.InQuad));
        animation.OnComplete(() =>
        {
            animation = null;
            Destroy(gameObject);
        });
    }

    // 외부에서 데미지 수치를 전달받아 텍스트를 변경하는 함수
    public void Setup(float damageAmount)
    {
        Setup(damageAmount, false);
    }

    public void Setup(float damageAmount, bool isCritical)
    {
        textMesh.text = isCritical ? $"CRIT! {damageAmount:F0}" : $"{damageAmount:F0}";
        critical = isCritical;
        textMesh.fontSize = normalFontSize * (critical ? 1.35f : 1f);
        textMesh.color = critical ? new Color(1f, 0.65f, 0.12f, normalColor.a) : normalColor;
    }

    public void SetFontSize(float fontSize)
    {
        normalFontSize = fontSize;
        textMesh.fontSize = fontSize * (critical ? 1.35f : 1f);
    }

    private void SetAlpha(float alpha)
    {
        Color color = textMesh.color;
        color.a = alpha;
        textMesh.color = color;
    }

    private void OnDestroy()
    {
        animation?.Kill();
    }
}
