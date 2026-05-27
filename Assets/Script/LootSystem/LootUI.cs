using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LootUI : MonoBehaviour
{
    public static LootUI Instance { get; private set; }

    [SerializeField] private GameObject lootItemRowPrefab;
    [SerializeField] private GameObject lootWindow; // nuvarande
    [SerializeField] private Transform contentParent;
    [SerializeField] private TMPro.TMP_Text titleText;


    // Nya fält (lägg in i inspector)
    [SerializeField] private RectTransform lootWindowRect; // LootWindow RectTransform
    [SerializeField] private RectTransform contentRect; // Content RectTransform
    [SerializeField] private int paddingTop = 6;
    [SerializeField] private int paddingBottom = 6;
    [SerializeField] private float maxHeight = 0f; // 0 = ingen max
    [SerializeField] private float titleHeight = 40f;

    LootContainer currentContainer;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }

    public void Show(LootContainer container, string title)
    {
        titleText.text = title;
        currentContainer = container;

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        HashSet<ItemData> shownItems = new HashSet<ItemData>();

        foreach (ItemData item in container.items)
        {
            if (shownItems.Contains(item))
                continue;

            shownItems.Add(item);

            GameObject row = Instantiate(lootItemRowPrefab, contentParent);
            row.GetComponent<LootItemRow>().Setup(item, container, this);
        }

        // Om lootWindowRect är stretch-anchored kan du istället göra:
        // Vector2 min = lootWindowRect.offsetMin; min.y = -targetHeight; lootWindowRect.offsetMin = min;
        // (Anpassa beroende på dina anchors)

        gameObject.SetActive(true);
    }

    public void Refresh()
    {
        if (currentContainer == null)
        {
            Close();
            return;
        }

        if (currentContainer.items.Count == 0)
        {
            Close();
            return;
        }

        Show(currentContainer, titleText.text);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}



