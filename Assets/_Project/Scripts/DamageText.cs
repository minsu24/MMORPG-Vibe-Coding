using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    public float moveSpeed = 2.0f;     // 위로 올라가는 속도
    public float destroyTime = 1.0f;   // 텍스트가 사라지기까지의 시간
    public float alphaSpeed = 2.0f;    // 투명해지는 속도

    private TextMeshPro textMesh;
    private Color textColor;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        textColor = textMesh.color;
    }

    void Start()
    {
        // 설정한 시간 뒤에 오브젝트 파괴
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        // 1. 위로 이동
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

        // 2. 페이드 아웃 (알파값 감소)
        textColor.a = Mathf.Lerp(textColor.a, 0, Time.deltaTime * alphaSpeed);
        textMesh.color = textColor;
    }

    // 외부에서 데미지 수치를 전달받아 텍스트를 변경하는 함수
    public void Setup(float damageAmount)
    {
        textMesh.text = damageAmount.ToString();
    }

    public void SetFontSize(float fontSize)
    {
        textMesh.fontSize = fontSize;
    }

    
}
