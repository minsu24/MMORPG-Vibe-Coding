namespace EasternFantasy.Player
{
    // Uses gameplay time so a short hit stop does not eat a queued press.
    public sealed class PlayerAttackInputBuffer
    {
        private float expiresAt = float.NegativeInfinity;
        private bool requireRelease;

        public bool Read(bool pressed, bool held, bool repeat, float now, float window)
        {
            if (requireRelease)
            {
                if (!held) requireRelease = false;
                return false;
            }
            if (pressed) expiresAt = now + window;
            return (repeat && held) || now <= expiresAt;
        }

        public void Consume() => expiresAt = float.NegativeInfinity;

        public void Suppress()
        {
            Consume();
            requireRelease = true;
        }
    }
}
