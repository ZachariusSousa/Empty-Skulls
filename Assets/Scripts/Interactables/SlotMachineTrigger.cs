using UnityEngine;

public class SlotMachineTrigger : MonoBehaviour
{
    public GameObject slotsUIPanel;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            slotsUIPanel.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            slotsUIPanel.SetActive(false);
        }
    }
}

