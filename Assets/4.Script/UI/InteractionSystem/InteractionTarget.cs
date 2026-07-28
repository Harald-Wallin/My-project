using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class InteractionTarget :
    MonoBehaviour
{
    [Header("Interaction Owner")]

    [Tooltip(
        "Objektet vars komponenter erbjuder interaktioner. " +
        "För en NPC är detta normalt NPC-rooten.")]
    [SerializeField]
    private GameObject interactionOwner;

    [Header("Distance")]

    [Tooltip(
        "Maximalt avstånd från spelaren för att starta interaktionen.")]
    [SerializeField]
    [Min(0f)]
    private float interactionDistance = 3f;

    [Tooltip(
        "Avståndet där ett UI-fönster som öppnats från objektet stängs.")]
    [SerializeField]
    [Min(0f)]
    private float windowCloseDistance = 3.5f;

    private Collider2D interactionCollider;

    public GameObject InteractionOwner =>
        interactionOwner != null
            ? interactionOwner
            : gameObject;

    public Transform InteractionTransform =>
        InteractionOwner.transform;

    public Collider2D InteractionCollider =>
        interactionCollider;

    public float InteractionDistance =>
        interactionDistance;

    public float WindowCloseDistance =>
        Mathf.Max(
            interactionDistance,
            windowCloseDistance);

    private void Awake()
    {
        interactionCollider =
            GetComponent<Collider2D>();

        if (interactionOwner == null)
        {
            Transform parent =
                transform.parent;

            interactionOwner =
                parent != null
                    ? parent.gameObject
                    : gameObject;
        }
    }

    public bool IsWithinInteractionDistance(
        Transform interactor)
    {
        if (interactor == null)
            return false;

        Vector2 interactionPoint =
            GetClosestInteractionPoint(
                interactor.position);

        float sqrDistance =
            (
                (Vector2)interactor.position -
                interactionPoint
            ).sqrMagnitude;

        return sqrDistance <=
               interactionDistance *
               interactionDistance;
    }

    public void GetInteractionOptions(
        List<IInteractionOption> results)
    {
        if (results == null)
            return;

        results.Clear();

        GameObject owner =
            InteractionOwner;

        if (owner == null)
            return;

        MonoBehaviour[] behaviours =
            owner.GetComponents<MonoBehaviour>();

        for (int i = 0;
             i < behaviours.Length;
             i++)
        {
            MonoBehaviour behaviour =
                behaviours[i];

            if (behaviour == null ||
                !behaviour.isActiveAndEnabled)
            {
                continue;
            }

            if (behaviour is
                IInteractionOption option)
            {
                results.Add(option);
            }
        }
    }

    private Vector2 GetClosestInteractionPoint(
        Vector2 position)
    {
        if (interactionCollider == null)
        {
            return InteractionTransform.position;
        }

        return interactionCollider
            .ClosestPoint(position);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        interactionDistance =
            Mathf.Max(
                0f,
                interactionDistance);

        windowCloseDistance =
            Mathf.Max(
                interactionDistance,
                windowCloseDistance);
    }
#endif
}
