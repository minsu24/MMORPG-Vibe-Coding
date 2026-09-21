using EasternFantasy.Inventory;
using EasternFantasy.UI;
using UnityEngine;

namespace EasternFantasy.Shop
{
    [DisallowMultipleComponent]
    public sealed class Shopkeeper : MonoBehaviour
    {
        [SerializeField] private ItemDefinition[] stock;

        public ItemDefinition[] Stock => stock;

        public void OpenShop()
        {
            if (ShopWindowUI.Instance == null)
            {
                Debug.LogWarning("The scene needs a ShopWindowUI.", this);
                return;
            }

            ShopWindowUI.Instance.Open(this);
        }
    }
}
