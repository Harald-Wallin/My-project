using System.Collections.Generic;
using UnityEngine;

public sealed class FavourGiver :
    MonoBehaviour
{
    [Header("Identity")]

    [SerializeField]
    [Tooltip(
        "Valfritt presentationsnamn. Om tomt används " +
        "CharacterStats.DisplayName eller GameObject-namnet."
    )]
    private string giverName;

    [Header("Favours")]

    [SerializeField]
    private List<FavourData>
        favours =
            new();

    [Header("Interaction")]

    [SerializeField]
    [Tooltip(
        "Registrerar ExplicitAccept-favours när spelaren " +
        "interagerar med objektet."
    )]
    private bool registerOnInteraction =
        true;

    public string GiverName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(
                    giverName))
            {
                return giverName;
            }

            CharacterStats character =
                GetComponent<CharacterStats>();

            if (character != null)
            {
                return character.DisplayName;
            }

            return gameObject.name;
        }
    }

    public IReadOnlyList<FavourData>
        Favours =>
            favours;

    private void Start()
    {
        RegisterBackgroundFavours();
    }

    private void RegisterBackgroundFavours()
    {
        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return;

        foreach (FavourData favour
                 in favours)
        {
            if (favour == null)
                continue;

            if (favour.ActivationPolicy !=
                FavourActivationPolicy
                    .TrackBeforeDiscovery)
            {
                continue;
            }

            FavourRuntime runtime =
                manager.RegisterFavour(
                    favour
                );

            if (runtime != null &&
                runtime.State ==
                FavourState.Available)
            {
                runtime.TryActivate();
            }
        }
    }

    /// <summary>
    /// Ska senare anropas av InteractionController.
    ///
    /// För närvarande kan metoden även anropas från en
    /// tillfällig testknapp eller UnityEvent.
    /// </summary>
    public void Interact()
    {
        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning(
                $"{name} försökte öppna favours men spelaren " +
                "saknar PlayerFavourManager.",
                this
            );

            return;
        }

        foreach (FavourData favour
                 in favours)
        {
            if (favour == null)
                continue;

            switch (favour.ActivationPolicy)
            {
                case FavourActivationPolicy
                    .ExplicitAccept:

                    if (registerOnInteraction)
                    {
                        manager.RegisterFavour(
                            favour
                        );
                    }

                    break;

                case FavourActivationPolicy
                    .DiscoverOnInteraction:

                    FavourRuntime runtime =
                        manager.RegisterFavour(
                            favour
                        );

                    if (runtime != null &&
                        runtime.State ==
                        FavourState.Available)
                    {
                        runtime.TryActivate();
                    }

                    break;

                case FavourActivationPolicy
                    .TrackBeforeDiscovery:

                    manager.RegisterFavour(
                        favour
                    );

                    break;
            }
        }

        /*
         * Nästa UI-fas öppnar favour-selection-fönstret här.
         */
    }

    public bool TryAccept(
        FavourData favour)
    {
        if (!ContainsFavour(
                favour))
        {
            return false;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        return manager != null &&
               manager.TryAccept(
                   favour
               );
    }

    public bool TryTurnIn(
        FavourData favour)
    {
        if (!ContainsFavour(
                favour))
        {
            return false;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        return manager != null &&
               manager.TryTurnIn(
                   favour
               );
    }

    public List<FavourRuntime>
        GetVisibleFavours()
    {
        List<FavourRuntime> result =
            new();

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return result;

        foreach (FavourData favour
                 in favours)
        {
            if (favour == null)
                continue;

            if (!manager.TryGetRuntime(
                    favour,
                    out FavourRuntime runtime))
            {
                continue;
            }

            if (runtime.State ==
                FavourState.Unavailable)
            {
                continue;
            }

            result.Add(
                runtime
            );
        }

        return result;
    }

    private bool ContainsFavour(
        FavourData favour)
    {
        return favour != null &&
               favours.Contains(
                   favour
               );
    }
}
