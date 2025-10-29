using UnityEngine;

public class LootBagOpener : MonoBehaviour
{
    public LootBagUI lootUI;
    public string lootTag = "LootBag";
    public float openDistance = 3.5f;
    public LayerMask lootMask; 

    LootBag _current;

    void Update()
    {
        // Close if gone or too far
        if (_current)
        {
            if (!_current.gameObject || !_current.gameObject.activeInHierarchy)
            {
                lootUI.Unbind();
                _current = null;
            }
            else
            {
                float d = Vector3.Distance(transform.position, _current.transform.position);
                if (d > Mathf.Min(_current.openDistance, openDistance))
                {
                    lootUI.Unbind();

                    if (_current != null && _current.IsEmpty())
                        Destroy(_current.gameObject);

                    _current = null;
                }
            }
        }

        // Acquire
        if (!_current)
        {
            var best = FindBestBag(transform.position, openDistance);
            if (best)
            {
                _current = best;
                lootUI.Bind(_current);
            }
        }
    }

    LootBag FindBestBag(Vector3 pos, float radius)
    {
        // if mask is 0 (Nothing), ignore it and search all layers
        Collider2D[] hits = (lootMask.value != 0)
            ? Physics2D.OverlapCircleAll(pos, radius, lootMask)
            : Physics2D.OverlapCircleAll(pos, radius);

        LootBag bestNonEmpty = null; float bestNonEmptyD = float.MaxValue;
        LootBag bestAny = null;      float bestAnyD = float.MaxValue;

        foreach (var h in hits)
        {
            if (!h) continue;
            if (!h.CompareTag(lootTag)) continue;

            var bag = h.GetComponent<LootBag>();
            if (!bag || !bag.gameObject.activeInHierarchy) continue;

            float d = (bag.transform.position - pos).sqrMagnitude;

            if (!bag.IsEmpty() && d < bestNonEmptyD)
            {
                bestNonEmptyD = d; bestNonEmpty = bag;
            }
            if (d < bestAnyD)
            {
                bestAnyD = d; bestAny = bag;
            }
        }

        var chosen = bestNonEmpty ? bestNonEmpty : bestAny;
        if (!chosen) return null;

        float realD = Vector3.Distance(pos, chosen.transform.position);
        return (realD <= Mathf.Min(chosen.openDistance, openDistance)) ? chosen : null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1,1,1,0.25f);
        Gizmos.DrawWireSphere(transform.position, openDistance);
    }
#endif
}
