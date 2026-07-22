using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SortingGroup))]
public class YSorter : MonoBehaviour
{
    private SortingGroup sortingGroup;

    [SerializeField] private int sortingOrderOffset = 0;
    [SerializeField] private float yOffset = 0f;

    private void Awake()
    {
        sortingGroup = GetComponent<SortingGroup>();
    }

    private void LateUpdate()
    {
        sortingGroup.sortingOrder =
            (int)((-transform.position.y - yOffset) * 100)
            + sortingOrderOffset;
    }
}
