using UnityEngine;

namespace EasternFantasy.Inventory
{
    // Start only after landing; collection and airborne physics retain control of the transform.
    [DisallowMultipleComponent]
    public sealed class DroppedItemFloat : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float height = .08f;
        [SerializeField, Min(.1f)] private float period = 2.4f;
        private Vector3 restPosition;
        private float elapsed;
        private bool floating;

        public void Begin(Vector3 position)
        {
            restPosition = position;
            elapsed = 0f;
            floating = true;
        }

        public void Stop() => floating = false;

        private void Update()
        {
            if (!floating) return;
            elapsed += Time.deltaTime;
            transform.position = restPosition + Vector3.up *
                (height * .5f * (1f - Mathf.Cos(elapsed * Mathf.PI * 2f / period)));
        }
    }
}
