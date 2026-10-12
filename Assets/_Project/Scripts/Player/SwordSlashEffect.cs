using UnityEngine;

namespace EasternFantasy.Player
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class SwordSlashEffect : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float duration = 0.18f;
        [SerializeField, Min(0.1f)] private float radius = 1.65f;
        [SerializeField, Min(0.01f)] private float thickness = 0.32f;
        [SerializeField] private Color energyColor = new Color(0.35f, 0.85f, 1f, 1f);
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private Mesh mesh;
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock properties;
        private float elapsed;
        private float direction = 1f;
        private Vector3 origin;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            properties = new MaterialPropertyBlock();
            BuildMesh();
            origin = transform.position;
        }

        public void Play(float facing, SpriteRenderer character)
        {
            direction = facing < 0f ? -1f : 1f;
            origin = transform.position;
            elapsed = 0f;
            if (character != null)
            {
                meshRenderer.sortingLayerID = character.sortingLayerID;
                meshRenderer.sortingOrder = character.sortingOrder + 2;
            }
            UpdateAppearance(0f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                Destroy(gameObject);
                return;
            }
            UpdateAppearance(elapsed / duration);
        }

        private void UpdateAppearance(float progress)
        {
            float size = Mathf.Lerp(0.8f, 1.08f, progress);
            transform.localScale = new Vector3(size * direction, size, 1f);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(18f, -18f, progress) * direction);
            transform.position = origin + Vector3.right * (direction * progress * 0.2f);
            properties.SetColor(ColorProperty, new Color(1f, 1f, 1f, 1f - progress * progress));
            meshRenderer.SetPropertyBlock(properties);
        }

        private void BuildMesh()
        {
            const int segments = 40;
            const int rows = 5;
            Vector3[] vertices = new Vector3[(segments + 1) * rows];
            Color[] colors = new Color[vertices.Length];
            int[] triangles = new int[segments * (rows - 1) * 6];
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = Mathf.Lerp(-65f, 65f, t) * Mathf.Deg2Rad;
                float width = thickness * Mathf.Sin(t * Mathf.PI);
                for (int row = 0; row < rows; row++)
                {
                    float r = radius - width + width * row / (rows - 1f);
                    int index = i * rows + row;
                    vertices[index] = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r * 0.65f, 0f);
                    Color color = row == 2 ? Color.white : energyColor;
                    color.a = row == 0 || row == 4 ? 0f : row == 2 ? 1f : 0.65f;
                    colors[index] = color;
                }
            }
            int triangle = 0;
            for (int i = 0; i < segments; i++)
                for (int row = 0; row < rows - 1; row++)
                {
                    int a = i * rows + row;
                    int b = a + rows;
                    triangles[triangle++] = a; triangles[triangle++] = b; triangles[triangle++] = a + 1;
                    triangles[triangle++] = a + 1; triangles[triangle++] = b; triangles[triangle++] = b + 1;
                }
            mesh = new Mesh { name = "Sword Energy Crescent" };
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        private void OnDestroy()
        {
            if (mesh == null) return;
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
        }
    }
}
