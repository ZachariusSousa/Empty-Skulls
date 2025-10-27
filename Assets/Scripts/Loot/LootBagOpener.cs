using UnityEngine;

public class LootBagOpener : MonoBehaviour
{
    public LootBagUI lootUI;
    public string lootTag = "LootBag";     // tag your bag prefabs with this
    public float openDistance = 3.5f;

    LootBag _current;

    void Update()
    {
        // If we have an open bag but walked away, close it
        if (_current)
        {
            float d = Vector3.Distance(transform.position, _current.transform.position);
            if (d > Mathf.Min(_current.openDistance, openDistance))
            {
                lootUI.Unbind();
                _current = null;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryOpen(other.gameObject);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!_current) TryOpen(other.gameObject);
    }

    void TryOpen(GameObject go)
    {
        if (lootUI == null || _current != null) return;
        if (!go.CompareTag(lootTag)) return;

        var bag = go.GetComponent<LootBag>();
        if (!bag) return;

        float d = Vector3.Distance(transform.position, bag.transform.position);
        if (d <= bag.openDistance)
        {
            _current = bag;
            lootUI.Bind(bag);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (_current && other.gameObject == _current.gameObject)
        {
            lootUI.Unbind();
            _current = null;
        }
    }
}
