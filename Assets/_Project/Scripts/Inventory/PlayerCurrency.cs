using System;
using UnityEngine;

namespace EasternFantasy.Inventory
{
    [DisallowMultipleComponent]
    public sealed class PlayerCurrency : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingYeopjeon;

        private int yeopjeon;
        private bool initialized;

        public int Yeopjeon
        {
            get
            {
                EnsureInitialized();
                return yeopjeon;
            }
        }

        public event Action<int> CurrencyChanged;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            EnsureInitialized();
            yeopjeon += amount;
            CurrencyChanged?.Invoke(yeopjeon);
        }

        public bool TrySpend(int amount)
        {
            if (!TrySpendSilently(amount)) return false;
            NotifyChanged();
            return true;
        }

        internal bool TrySpendSilently(int amount)
        {
            if (amount < 0) return false;
            EnsureInitialized();
            if (yeopjeon < amount) return false;
            yeopjeon -= amount;
            return true;
        }

        internal void NotifyChanged() => CurrencyChanged?.Invoke(yeopjeon);
        public bool CanAfford(int amount)
        {
            return amount >= 0 && Yeopjeon >= amount;
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;
            yeopjeon = Mathf.Max(0, startingYeopjeon);
        }
    }
}
