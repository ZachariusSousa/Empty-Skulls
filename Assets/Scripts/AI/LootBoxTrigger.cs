using UnityEngine;

public class LootBoxTrigger : MonoBehaviour
{
    public GameObject inventoryUIPanel; // Drag the UI panel here in Inspector

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            inventoryUIPanel.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            inventoryUIPanel.SetActive(false);
        }
    }
}

