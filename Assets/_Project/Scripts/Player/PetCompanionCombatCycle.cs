using System;

namespace EasternFantasy.Player
{
    public enum PetCompanionCombatState { Waiting, Approaching, Attacking, Returning }

    // Shared by the actor and isolated timing tests. Only an owner-selected target enables attacks.
    public sealed class PetCompanionCombatCycle
    {
        private readonly float interval, duration, hitTime;
        private float cooldown, elapsed;
        private bool hitApplied;
        public PetCompanionCombatState State { get; private set; }
        public float CooldownRemaining => cooldown;

        public PetCompanionCombatCycle(float attackInterval, float attackDuration, float attackHitTime)
        {
            interval = Math.Max(.01f, attackInterval);
            duration = Math.Max(.01f, attackDuration);
            hitTime = Math.Min(duration, Math.Max(0f, attackHitTime));
        }

        public void CancelAttack()
        {
            if (State != PetCompanionCombatState.Waiting) State = PetCompanionCombatState.Returning;
            hitApplied = true;
            elapsed = 0f;
        }

        public bool Tick(float deltaTime, bool hasTarget, bool nearTarget, bool nearOwner)
        {
            if (deltaTime <= 0f) return false;
            cooldown = Math.Max(0f, cooldown - deltaTime);
            switch (State)
            {
                case PetCompanionCombatState.Waiting:
                    if (!nearOwner) State = PetCompanionCombatState.Returning;
                    else if (hasTarget && cooldown <= 0f) State = PetCompanionCombatState.Approaching;
                    break;
                case PetCompanionCombatState.Approaching:
                    if (!hasTarget) State = PetCompanionCombatState.Returning;
                    else if (nearTarget)
                    {
                        State = PetCompanionCombatState.Attacking;
                        cooldown = interval;
                        elapsed = 0f;
                        hitApplied = false;
                    }
                    break;
                case PetCompanionCombatState.Attacking:
                    if (!hasTarget) { State = PetCompanionCombatState.Returning; break; }
                    elapsed += deltaTime;
                    bool hit = !hitApplied && elapsed >= hitTime;
                    if (hit) hitApplied = true;
                    if (elapsed >= duration) State = PetCompanionCombatState.Returning;
                    return hit;
                case PetCompanionCombatState.Returning:
                    if (nearOwner) State = PetCompanionCombatState.Waiting;
                    break;
            }
            return false;
        }
    }
}
