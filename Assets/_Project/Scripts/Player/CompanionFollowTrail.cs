using System.Collections.Generic;
using UnityEngine;

namespace EasternFantasy.Player
{
    // Replay actual foot positions, rather than springing toward the owner's current position.
    public sealed class CompanionFollowTrail
    {
        private struct Point
        {
            public float Time, Distance;
            public Vector3 Position;
            public bool Grounded;
        }

        private readonly List<Point> points = new List<Point>();
        private int cursor;
        private float clock;
        private float lastLandingTime = float.NegativeInfinity;
        public int Count => points.Count;
        public bool IsCaughtUp { get; private set; }

        public void Reset(Vector3 pet, Vector3 owner, bool grounded, float now, float delay)
        {
            points.Clear();
            cursor = 0;
            clock = now - delay;
            lastLandingTime = float.NegativeInfinity;
            IsCaughtUp = false;
            points.Add(new Point { Time = clock, Position = pet, Grounded = grounded });
            Record(owner, grounded, now);
        }

        public void Record(Vector3 position, bool grounded, float now)
        {
            if (points.Count == 0) return;
            Point previous = points[points.Count - 1];
            if (now <= previous.Time) return;
            if (!previous.Grounded && grounded) lastLandingTime = now;
            var next = new Point
            {
                Time = now, Position = position, Grounded = grounded,
                Distance = previous.Distance + Vector3.Distance(previous.Position, position)
            };
            // Keep both ends of a rest, without accumulating thousands of identical frames.
            if (points.Count > cursor + 2 && previous.Grounded == grounded
                && points[points.Count - 2].Grounded == grounded
                && (previous.Position - position).sqrMagnitude < .000001f
                && (points[points.Count - 2].Position - position).sqrMagnitude < .000001f)
                points[points.Count - 1] = next;
            else points.Add(next);
        }

        public Vector3 Advance(float now, float deltaTime, float delay, float spacing)
        {
            if (points.Count == 0) return Vector3.zero;
            float limit = Mathf.Min(now - delay, Mathf.Max(SpacingLimit(spacing), lastLandingTime));
            float budget = Mathf.Max(0f, deltaTime);
            while (cursor + 1 < points.Count && clock < limit)
            {
                Point a = points[cursor], b = points[cursor + 1];
                float end = Mathf.Min(limit, b.Time);
                // A long rest in the recorded path must not add a second waiting period.
                bool stationary = (b.Position - a.Position).sqrMagnitude < .000001f;
                float step = stationary ? Mathf.Max(0f, end - clock) : Mathf.Min(budget, Mathf.Max(0f, end - clock));
                clock += step;
                if (!stationary) budget -= step;
                if (clock >= b.Time - .000001f) cursor++;
                else break;
                if (budget <= 0f) break;
            }
            Point from = points[cursor];
            Vector3 result = from.Position;
            if (cursor + 1 < points.Count)
            {
                Point to = points[cursor + 1];
                result = Vector3.Lerp(from.Position, to.Position, Mathf.InverseLerp(from.Time, to.Time, clock));
            }
            if (cursor > 64)
            {
                points.RemoveRange(0, cursor);
                cursor = 0;
            }
            IsCaughtUp = clock >= limit - .05f;
            return result;
        }

        private float SpacingLimit(float spacing)
        {
            Point last = points[points.Count - 1];
            if (!last.Grounded || spacing <= 0f) return last.Time;
            float distance = last.Distance - spacing;
            for (int i = points.Count - 2; i >= cursor; i--)
            {
                Point a = points[i], b = points[i + 1];
                if (a.Distance > distance) continue;
                // Never park in mid-jump just to maintain an idle gap: finish landing first.
                if (!a.Grounded || !b.Grounded)
                {
                    for (int landing = i + 1; landing < points.Count; landing++)
                        if (points[landing].Grounded) return points[landing].Time;
                    return last.Time;
                }
                return Mathf.Lerp(a.Time, b.Time, Mathf.InverseLerp(a.Distance, b.Distance, distance));
            }
            return clock;
        }
    }
}
