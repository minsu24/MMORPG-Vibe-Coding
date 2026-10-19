using System;

namespace EasternFantasy.Player
{
    // Companion attacks do not register activity: only the player's combat keeps this state active.
    public sealed class PlayerCombatState
    {
        private bool hasActivity;
        private float lastActivity;
        public void Register(float now) { hasActivity = true; lastActivity = now; }
        public void Clear() => hasActivity = false;
        public bool IsActive(float now, float timeout) => hasActivity && now - lastActivity < Math.Max(.01f, timeout);
    }
}
