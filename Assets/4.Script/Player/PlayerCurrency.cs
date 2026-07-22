using UnityEngine;
using System;

public class PlayerCurrency : MonoBehaviour
{
    public static PlayerCurrency Instance;

    public event Action OnCoinsChanged;

    [SerializeField] private int bronzeCoins;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public int GetCoins()
    {
        return bronzeCoins;
    }

    public void AddCoins(int amount)
    {
        bronzeCoins += amount;
        OnCoinsChanged?.Invoke();
    }

    public bool TrySpendCoins(int amount)
    {
        if (bronzeCoins < amount)
            return false;

        bronzeCoins -= amount;
        OnCoinsChanged?.Invoke();
        return true;
    }
}
