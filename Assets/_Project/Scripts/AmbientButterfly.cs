using UnityEngine;

namespace EasternFantasy.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AmbientButterfly : MonoBehaviour
    {
        [SerializeField] private Sprite[] flightFrames;
        [SerializeField] private Vector2[] route;
        [SerializeField, Min(0.01f)] private float travelSpeed = 0.8f;
        [SerializeField, Min(1f)] private float framesPerSecond = 14f;
        [SerializeField, Range(0f, 1f)] private float startProgress;
        [SerializeField] private float flutterPhase;
        [SerializeField, Min(0f)] private float flutterAmplitude = 0.08f;

        private const int SamplesPerSegment = 32;
        private float[] distances;
        private float totalLength;
        private float elapsed;
        private float localDepth;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            localDepth = transform.localPosition.z;
            BuildDistanceTable();
            SampleAtTime(0f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            SampleAtTime(elapsed);
        }

        private void OnValidate()
        {
            distances = null;
        }

        // Deterministic sampling also allows an editor preview without running
        // the rest of the scene. Route coordinates belong to the parent object.
        public void SampleAtTime(float seconds)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                localDepth = transform.localPosition.z;
            }
            if (distances == null)
                BuildDistanceTable();
            if (totalLength <= 0.001f)
                return;

            Vector2 position = FlightPosition(seconds);
            Vector2 velocity = FlightPosition(seconds + 0.04f) - FlightPosition(seconds - 0.04f);
            transform.localPosition = new Vector3(position.x, position.y, localDepth);

            if (Mathf.Abs(velocity.x) > 0.003f)
                spriteRenderer.flipX = velocity.x < 0f;
            float bank = Mathf.Atan2(velocity.y, Mathf.Abs(velocity.x) + 0.03f) * Mathf.Rad2Deg * 0.25f;
            bank = Mathf.Clamp(bank, -16f, 16f) * (spriteRenderer.flipX ? -1f : 1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, bank);

            if (flightFrames != null && flightFrames.Length > 0)
            {
                int frame = Mathf.FloorToInt(Mathf.Repeat(seconds * framesPerSecond + flutterPhase, flightFrames.Length));
                if (flightFrames[frame] != null)
                    spriteRenderer.sprite = flightFrames[frame];
            }
        }

        private Vector2 FlightPosition(float seconds)
        {
            float distance = Mathf.Repeat(startProgress * totalLength + seconds * travelSpeed, totalLength);
            int low = 0;
            int high = distances.Length - 1;
            while (high - low > 1)
            {
                int middle = (low + high) / 2;
                if (distances[middle] <= distance)
                    low = middle;
                else
                    high = middle;
            }
            float fraction = Mathf.InverseLerp(distances[low], distances[high], distance);
            float progress = (low + fraction) / (distances.Length - 1);
            Vector2 position = CurvePosition(progress);
            // Small, independent drifting motions keep the route from looking mechanical.
            position.x += Mathf.Sin(seconds * 1.7f + flutterPhase) * flutterAmplitude * 0.4f;
            position.y += Mathf.Sin(seconds * 2.8f + flutterPhase) * flutterAmplitude
                + Mathf.Sin(seconds * 1.13f + flutterPhase * 2f) * flutterAmplitude * 0.35f;
            return position;
        }

        private void BuildDistanceTable()
        {
            totalLength = 0f;
            if (route == null || route.Length < 4)
            {
                distances = new float[2];
                return;
            }
            int samples = route.Length * SamplesPerSegment;
            distances = new float[samples + 1];
            Vector2 previous = CurvePosition(0f);
            for (int i = 1; i <= samples; i++)
            {
                Vector2 point = CurvePosition((float)i / samples);
                totalLength += Vector2.Distance(previous, point);
                distances[i] = totalLength;
                previous = point;
            }
        }

        private Vector2 CurvePosition(float progress)
        {
            int count = route.Length;
            float scaled = Mathf.Repeat(progress, 1f) * count;
            int index = Mathf.FloorToInt(scaled);
            float t = scaled - index;
            Vector2 a = route[(index + count - 1) % count];
            Vector2 b = route[index % count];
            Vector2 c = route[(index + 1) % count];
            Vector2 d = route[(index + 2) % count];
            return 0.5f * ((2f * b) + (-a + c) * t
                + (2f * a - 5f * b + 4f * c - d) * t * t
                + (-a + 3f * b - 3f * c + d) * t * t * t);
        }

        private void OnDrawGizmosSelected()
        {
            if (route == null || route.Length < 4)
                return;
            Gizmos.color = new Color(1f, 0.65f, 0.85f, 0.85f);
            Vector3 previous = RouteWorldPoint(CurvePosition(0f));
            for (int i = 1; i <= 160; i++)
            {
                Vector3 point = RouteWorldPoint(CurvePosition(i / 160f));
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
            foreach (Vector2 point in route)
                Gizmos.DrawWireSphere(RouteWorldPoint(point), 0.08f);
        }

        private Vector3 RouteWorldPoint(Vector2 point)
        {
            Vector3 local = new Vector3(point.x, point.y, transform.localPosition.z);
            return transform.parent != null ? transform.parent.TransformPoint(local) : local;
        }
    }
}
