using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SortingGroup))]
public class YSorter : MonoBehaviour
{
    private const float SortingPrecision = 100f;

    private SortingGroup sortingGroup;

    [SerializeField]
    private int sortingOrderOffset = 0;

    [SerializeField]
    private float yOffset = 0f;

    private int lastSortingOrder =
        int.MinValue;

    private void Awake()
    {
        sortingGroup =
            GetComponent<SortingGroup>();

        RefreshSortingOrder();
    }

    private void LateUpdate()
    {
        RefreshSortingOrder();
    }

    private void RefreshSortingOrder()
    {
        int newSortingOrder =
            CalculateSortingOrder(
                transform.position.y,
                yOffset,
                sortingOrderOffset
            );

        if (newSortingOrder ==
            lastSortingOrder)
        {
            return;
        }

        sortingGroup.sortingOrder =
            newSortingOrder;

        lastSortingOrder =
            newSortingOrder;
    }

    public static int CalculateSortingOrder(
        float worldY,
        float yOffset = 0f,
        int sortingOrderOffset = 0)
    {
        return
            Mathf.RoundToInt(
                (-worldY - yOffset) *
                SortingPrecision
            )
            +
            sortingOrderOffset;
    }
}
