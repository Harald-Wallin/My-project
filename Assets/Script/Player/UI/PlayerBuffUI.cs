using System.Collections.Generic;
using UnityEngine;

public class PlayerBuffUI : MonoBehaviour
{
    public static PlayerBuffUI Instance;

    [SerializeField] private GameObject buffSlotPrefab;
    [SerializeField] private Transform container;

    private List<BuffSlotUI> activeSlots = new List<BuffSlotUI>();
    private BuffSystem subscribedBuffSystem;

    void Awake()
    {
        Instance = this;

        // Try to populate existing player buffs if any were applied before UI awake
        var player = PlayerReference.Player;
        if (player == null)
            player = FindFirstObjectByType<PlayerStats>();

        if (player != null)
        {
            var bs = player.GetComponent<BuffSystem>();
            if (bs != null)
            {
                subscribedBuffSystem = bs;

                var list = bs.GetActiveBuffs();
                foreach (var b in list)
                {
                    AddBuff(b, bs);
                }

                // subscribe to future adds/removes
                bs.OnBuffAdded += OnBuffAdded;
                bs.OnBuffRemoved += OnBuffRemoved;
            }
        }
    }

    void OnDestroy()
    {
        if (subscribedBuffSystem != null)
        {
            subscribedBuffSystem.OnBuffAdded -= OnBuffAdded;
            subscribedBuffSystem.OnBuffRemoved -= OnBuffRemoved;
        }
    }

    private void OnBuffAdded(ActiveBuff buff, BuffSystem owner)
    {
        AddBuff(buff, owner);
    }

    private void OnBuffRemoved(ActiveBuff buff, BuffSystem owner)
    {
        // Clean up any dead slots; BuffSlotUI will self-destruct when owner.HasBuff(buff) == false
        activeSlots.RemoveAll(s => s == null || s.GetRemainingTime() <= 0f);
    }

    public void AddBuff(ActiveBuff buff, BuffSystem owner = null)
    {
        //Debug.Log($"PlayerBuffUI.AddBuff: adding '{buff?.Name}' owner={(owner!=null?owner.gameObject.name:"null")}");
        var go = Instantiate(buffSlotPrefab, container);
        var slot = go.GetComponent<BuffSlotUI>();

        // Prefer explicit owner passed by BuffSystem.ApplyEffect
        if (owner != null)
        {
            slot.Setup(buff, owner);
        }
        else
        {
            // Fallback: try to get BuffSystem from the PlayerReference
            var player = PlayerReference.Player ?? FindFirstObjectByType<PlayerStats>();
            if (player != null)
            {
                var playerBuffSystem = player.GetComponent<BuffSystem>();
                slot.Setup(buff, playerBuffSystem);
            }
            else
            {
                slot.Setup(buff, null);
            }
        }

        activeSlots.Add(slot);

        SortBuffs();
    }

    // Backwards-compatible overload
    public void AddBuff(ActiveBuff buff)
    {
        AddBuff(buff, null);
    }

    void Update()
    {
        // Remove destroyed slots
        activeSlots.RemoveAll(slot => slot == null);

        // Sort by remaining time (longest first)
        activeSlots.Sort((a, b) => b.GetRemainingTime().CompareTo(a.GetRemainingTime()));

        // Update sibling order
        for (int i = 0; i < activeSlots.Count; i++)
        {
            activeSlots[i].transform.SetSiblingIndex(i);
        }
    }

    void SortBuffs()
    {
        activeSlots.Sort((a, b) => b.GetRemainingTime().CompareTo(a.GetRemainingTime()));

        for (int i = 0; i < activeSlots.Count; i++)
        {
            activeSlots[i].transform.SetSiblingIndex(i);
        }
    }
}
