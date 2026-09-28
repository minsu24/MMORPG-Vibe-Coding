using UnityEngine;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerEntity))]
    public sealed class PlayerGroundShadow : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float width = 1.15f;
        [SerializeField, Min(0.1f)] private float rayDistance = 5f;
        [SerializeField, Range(0f, 1f)] private float maximumOpacity = 0.42f;
        [SerializeField] private bool addCharacterEdge;

        private static Sprite shadowSprite;
        private SpriteRenderer shadow;
        private SpriteRenderer character;
        private SpriteRenderer characterEdge;
        private PlayerEntity entity;

        private void Awake()
        {
            entity = GetComponent<PlayerEntity>();
            character = GetComponentInChildren<SpriteRenderer>(true);
            if (shadowSprite == null)
                shadowSprite = CreateShadowSprite();

            GameObject child = new GameObject("Ground Shadow", typeof(SpriteRenderer));
            child.transform.SetParent(transform, false);
            shadow = child.GetComponent<SpriteRenderer>();
            shadow.sprite = shadowSprite;
            shadow.color = new Color(0.02f, 0.025f, 0.03f, maximumOpacity);
            shadow.transform.localScale = new Vector3(width, 0.45f, 1f);
            if (character != null)
            {
                shadow.sortingLayerID = character.sortingLayerID;
                shadow.sortingOrder = character.sortingOrder - 2;
                if (addCharacterEdge)
                {
                    GameObject edge = new GameObject("Character Edge", typeof(SpriteRenderer));
                    edge.transform.SetParent(character.transform.parent, false);
                    characterEdge = edge.GetComponent<SpriteRenderer>();
                    characterEdge.color = new Color(0.055f, 0.08f, 0.09f, 0.2f);
                    characterEdge.sortingLayerID = character.sortingLayerID;
                    characterEdge.sortingOrder = character.sortingOrder - 1;
                }
            }
        }

        private void LateUpdate()
        {
            if (entity.IsDead)
            {
                shadow.enabled = false;
                if (characterEdge != null)
                    characterEdge.enabled = false;
                return;
            }

            if (characterEdge != null)
            {
                characterEdge.enabled = character.enabled;
                characterEdge.sprite = character.sprite;
                characterEdge.flipX = character.flipX;
                characterEdge.flipY = character.flipY;
                characterEdge.transform.localPosition = character.transform.localPosition;
                characterEdge.transform.localRotation = character.transform.localRotation;
                characterEdge.transform.localScale = character.transform.localScale * 1.035f;
                characterEdge.color = new Color(0.055f, 0.08f, 0.09f, 0.2f);
            }

            RaycastHit2D[] hits = Physics2D.RaycastAll(
                (Vector2)transform.position + Vector2.up * 0.4f,
                Vector2.down, rayDistance);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger
                    || hit.collider.GetComponentInParent<PlayerEntity>() != null
                    || hit.collider.GetComponentInParent<EnemyController>() != null)
                    continue;

                shadow.enabled = true;
                shadow.transform.position = new Vector3(transform.position.x,
                    hit.point.y + 0.03f, transform.position.z);
                float distance = Mathf.Max(0f, transform.position.y - hit.point.y);
                shadow.color = new Color(0.02f, 0.025f, 0.03f,
                    maximumOpacity * Mathf.Clamp01(1f - distance / rayDistance));
                return;
            }
            shadow.enabled = false;
        }

        private static Sprite CreateShadowSprite()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                    float dy = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - dx * dx - dy * dy), 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), size);
        }
    }
}
