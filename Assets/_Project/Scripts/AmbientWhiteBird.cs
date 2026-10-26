using System.Collections.Generic;
using UnityEngine;

namespace EasternFantasy.World
{
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class AmbientWhiteBird : MonoBehaviour
    {
        [SerializeField] private Sprite[] flightFrames;
        [SerializeField] private Sprite[] landingFrames;
        [SerializeField] private Sprite[] restingFrames;
        [SerializeField] private Sprite[] takeoffFrames;
        [SerializeField] private Collider2D[] landingSurfaces = System.Array.Empty<Collider2D>();
        [SerializeField] private Transform[] landingPoints = System.Array.Empty<Transform>();
        [SerializeField] private Vector2 horizontalRange = new Vector2(-19f, 18f);
        [SerializeField] private Vector2 restSeconds = new Vector2(3f, 6f);
        [SerializeField, Min(.1f)] private float flightSpeed = 3.2f;
        [SerializeField, Min(1f)] private float framesPerSecond = 14f;
        [SerializeField, Min(.1f)] private float restingFramesPerSecond = 3f;

        private enum Phase { Flying, Landing, Resting, TakingOff }
        private Phase phase;
        private SpriteRenderer image;
        private System.Random random;
        private Collider2D previousSurface;
        private Transform previousPoint, perchAnchor, nextPerchAnchor;
        private Vector2 perch, nextPerch, start, controlA, controlB, finish;
        private float clock, duration, depth;
        private readonly float[] distances = new float[65];
        private float distance;

        private void Start()
        {
            image = GetComponent<SpriteRenderer>();
            random = new System.Random(unchecked(System.Environment.TickCount ^ GetInstanceID()));
            depth = transform.position.z;
            perch = transform.position;
            foreach (var surface in landingSurfaces)
                if (surface != null && surface.bounds.Contains(new Vector3(perch.x, perch.y - .02f, surface.bounds.center.z)))
                    previousSurface = surface;
            image.enabled = true;
            BeginEntrance();
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

        private void BeginEntrance()
        {
            var camera = Camera.main;
            // The opening landing stays near the entry camera, including entry from the right portal.
            if (camera != null)
            {
                foreach (var surface in landingSurfaces)
                {
                    if (surface == null || !surface.enabled || !surface.gameObject.activeInHierarchy
                        || surface.isTrigger || surface.bounds.size.x < 10f) continue;
                    var bounds = surface.bounds;
                    float x = Mathf.Clamp(camera.transform.position.x + 1.5f,
                        Mathf.Max(bounds.min.x + .4f, horizontalRange.x + .4f),
                        Mathf.Min(bounds.max.x - .4f, horizontalRange.y - .4f));
                    foreach (var hit in Physics2D.RaycastAll(new Vector2(x, bounds.max.y + 1f), Vector2.down, bounds.size.y + 2f))
                        if (hit.collider == surface)
                        {
                            perch = hit.point + Vector2.up * .012f;
                            previousSurface = surface;
                            break;
                        }
                    break;
                }
            }
            Vector2 entry = new Vector2(horizontalRange.x - 2f, perch.y + 4f);
            if (camera != null)
            {
                float cameraDepth = Vector3.Dot(new Vector3(perch.x, perch.y, depth) - camera.transform.position, camera.transform.forward);
                entry = camera.ViewportToWorldPoint(new Vector3(-.08f, .65f, Mathf.Max(.1f, cameraDepth)));
            }
            Place(entry);
            image.flipX = entry.x > perch.x;
            Animate(flightFrames, 0f, true);
            var near = perch + new Vector2(image.flipX ? 1.1f : -1.1f, 1.1f);
            BuildFlight(entry, Vector2.Lerp(entry, near, .5f) + Vector2.up * .6f, near, perch);
            SetPhase(Phase.Flying, Mathf.Clamp(distance / flightSpeed, 2f, 14f));
        }

        private void Update() => Advance(Time.deltaTime);

        // Shared by runtime and editor verification so full cycles can be checked without playing the scene.
        public void Advance(float deltaTime)
        {
            if (image == null) Start();
            if (phase != Phase.Flying && perchAnchor != null)
                perch = (Vector2)perchAnchor.position + Vector2.up * .012f;
            clock += Mathf.Max(0f, deltaTime);
            float t = Mathf.Clamp01(clock / Mathf.Max(.01f, duration));
            switch (phase)
            {
                case Phase.Flying:
                    float u = DistanceParameter(t);
                    Vector2 point = Curve(u);
                    Vector2 direction = Curve(Mathf.Min(1f, u + .015f)) - Curve(Mathf.Max(0f, u - .015f));
                    float flutter = Mathf.Sin(clock * 8f) * .045f * Mathf.Sin(t * Mathf.PI);
                    Place(point + Vector2.up * flutter);
                    if (Mathf.Abs(direction.x) > .001f) image.flipX = direction.x < 0f;
                    float bank = Mathf.Clamp(Mathf.Atan2(direction.y, Mathf.Abs(direction.x) + .1f) * Mathf.Rad2Deg * .14f, -12f, 12f);
                    transform.rotation = Quaternion.Euler(0f, 0f, bank * (image.flipX ? -1f : 1f) * Mathf.Sin(t * Mathf.PI));
                    Animate(flightFrames, clock * framesPerSecond, true);
                    if (t >= 1f)
                    {
                        transform.rotation = Quaternion.identity;
                        Place(perch);
                        SetPhase(Phase.Landing, landingFrames.Length / 11f);
                    }
                    break;
                case Phase.Landing:
                    Place(perch);
                    Animate(landingFrames, clock * 11f, false);
                    if (t >= 1f) SetPhase(Phase.Resting, Range(restSeconds.x, restSeconds.y));
                    break;
                case Phase.Resting:
                    Place(perch);
                    Animate(restingFrames, clock * restingFramesPerSecond, true);
                    if (t >= 1f)
                    {
                        if (ChooseDestination())
                        {
                            image.flipX = nextPerch.x < perch.x;
                            SetPhase(Phase.TakingOff, takeoffFrames.Length / 14f);
                        }
                        else SetPhase(Phase.Resting, 2f);
                    }
                    break;
                case Phase.TakingOff:
                    Place(perch + Vector2.up * (.38f * t * t));
                    Animate(takeoffFrames, clock * 14f, false);
                    if (t >= 1f) BeginFlight();
                    break;
            }
        }

        private bool ChooseDestination()
        {
            var available = new List<Collider2D>();
            foreach (var surface in landingSurfaces)
                if (surface != null && surface.enabled && surface.gameObject.activeInHierarchy && !surface.isTrigger && surface != previousSurface)
                    available.Add(surface);
            if (available.Count == 0 && previousSurface != null && previousSurface.enabled && previousSurface.gameObject.activeInHierarchy)
                available.Add(previousSurface);
            var points = new List<Transform>();
            foreach (var point in landingPoints)
                if (point != null && point.gameObject.activeInHierarchy && point != previousPoint
                    && point.position.x >= horizontalRange.x + .3f && point.position.x <= horizontalRange.y - .3f
                    && Vector2.Distance(point.position, perch) >= 2f)
                    points.Add(point);
            if (available.Count + points.Count == 0) return false;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                int choice = random.Next(available.Count + points.Count);
                if (choice >= available.Count)
                {
                    nextPerchAnchor = points[choice - available.Count];
                    nextPerch = (Vector2)nextPerchAnchor.position + Vector2.up * .012f;
                    previousPoint = nextPerchAnchor;
                    previousSurface = null;
                    return true;
                }
                Collider2D chosen = available[choice];
                Bounds bounds = chosen.bounds;
                float margin = Mathf.Min(.35f, bounds.size.x * .2f);
                float minX = Mathf.Max(bounds.min.x + margin, horizontalRange.x + margin);
                float maxX = Mathf.Min(bounds.max.x - margin, horizontalRange.y - margin);
                if (minX > maxX) continue;
                float x = Range(minX, maxX);
                foreach (var hit in Physics2D.RaycastAll(new Vector2(x, bounds.max.y + 1f), Vector2.down, bounds.size.y + 2f))
                {
                    if (hit.collider != chosen || Vector2.Distance(hit.point, perch) < 2f) continue;
                    nextPerch = hit.point + Vector2.up * .012f;
                    nextPerchAnchor = null;
                    previousPoint = null;
                    previousSurface = chosen;
                    return true;
                }
            }
            return false;
        }

        private void BeginFlight()
        {
            Vector2 origin = transform.position;
            float sign = image.flipX ? -1f : 1f;
            perch = nextPerch;
            perchAnchor = nextPerchAnchor;
            float cruiseHeight = Mathf.Min(9.4f, Mathf.Max(origin.y, perch.y) + Range(1.5f, 3.2f));
            float lead = Mathf.Min(3f, Mathf.Abs(perch.x - origin.x) * .4f + .4f);
            Vector2 a = new Vector2(Mathf.Clamp(origin.x + sign * lead, horizontalRange.x + .4f, horizontalRange.y - .4f), cruiseHeight);
            Vector2 b = new Vector2(Mathf.Clamp(perch.x - sign * lead, horizontalRange.x + .4f, horizontalRange.y - .4f), cruiseHeight);
            BuildFlight(origin, a, b, perch);
            SetPhase(Phase.Flying, Mathf.Clamp(distance / flightSpeed, 2f, 14f));
        }

        private void SetPhase(Phase value, float seconds) { phase = value; clock = 0f; duration = Mathf.Max(.05f, seconds); }
        private void Place(Vector2 point) => transform.position = new Vector3(point.x, point.y, depth);
        private void Animate(Sprite[] frames, float frame, bool loop)
        {
            if (frames == null || frames.Length == 0) return;
            int index = loop ? Mathf.FloorToInt(frame) % frames.Length : Mathf.Min(Mathf.FloorToInt(frame), frames.Length - 1);
            image.sprite = frames[index];
        }

        private Vector2 Curve(float t)
        {
            float v = 1f - t;
            return v * v * v * start + 3f * v * v * t * controlA + 3f * v * t * t * controlB + t * t * t * finish;
        }

        private void BuildFlight(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            start = a; controlA = b; controlB = c; finish = d;
            distance = 0f; distances[0] = 0f;
            Vector2 last = a;
            for (int i = 1; i < distances.Length; i++)
            {
                Vector2 next = Curve(i / 64f);
                distance += Vector2.Distance(last, next);
                distances[i] = distance; last = next;
            }
        }

        private float DistanceParameter(float t)
        {
            // Accelerate after takeoff and slow down again for touchdown.
            float progress = t * t * (3f - 2f * t);
            float target = progress * distance;
            for (int i = 1; i < distances.Length; i++)
                if (target <= distances[i]) return (i - 1 + Mathf.InverseLerp(distances[i - 1], distances[i], target)) / 64f;
            return 1f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, .8f, .25f, .9f);
            foreach (var point in landingPoints)
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, .09f);
                    Gizmos.DrawLine(point.position + Vector3.left * .18f, point.position + Vector3.right * .18f);
                }
        }
    }
}
