using UnityEngine;

public class Item : MonoBehaviour
{
    public InventoryItem ItemInfo;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (UIController.Instance.Inventory.TryAddItem(ItemInfo))
            {
                Destroy(gameObject);
            }
        }
    }
}
