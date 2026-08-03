using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerDepthSorting : MonoBehaviour
{
    [Header("Renderer Player")]
    [SerializeField] private Renderer playerRenderer;
    [SerializeField] private string sharedSortingLayer = "player";
    [Min(1)] [SerializeField] private int orderScale = 100;
    [SerializeField] private int playerOrderOffset = 1;
    [Tooltip("Bobot kedalaman horizontal untuk grid isometrik. Nilai positif membuat sisi kanan semakin ke depan.")]
    [Range(-2f, 2f)] [SerializeField] private float horizontalDepthWeight = .5f;

    [Header("Furnitur yang Bisa Menutupi Player")]
    [SerializeField] private string roomRootName = "IsiKamar";
    [SerializeField] private string[] occluderNames = { "kasur", "kursi" };

    private readonly List<Renderer> occluderRenderers = new List<Renderer>();

    void Awake()
    {
        ResolveRenderers();
        RefreshFurnitureOrders();
        RefreshPlayerOrder();
    }

    void OnEnable()
    {
        ResolveRenderers();
        RefreshFurnitureOrders();
        RefreshPlayerOrder();
    }

    void LateUpdate()
    {
        if (playerRenderer == null)
            ResolveRenderers();

        RefreshFurnitureOrders();
        RefreshPlayerOrder();
    }

    void ResolveRenderers()
    {
        playerRenderer ??= GetComponent<SpriteRenderer>();
        occluderRenderers.Clear();

        Transform roomRoot = FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item =>
                item.name.Equals(roomRootName, StringComparison.OrdinalIgnoreCase));
        if (roomRoot == null || occluderNames == null)
            return;

        foreach (string objectName in occluderNames)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                continue;

            Transform target = roomRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item =>
                    item.name.Equals(objectName, StringComparison.OrdinalIgnoreCase));
            if (target == null)
                continue;

            Renderer renderer = target.GetComponent<Renderer>() ??
                                target.GetComponentInChildren<Renderer>(true);
            if (renderer != null && !occluderRenderers.Contains(renderer))
                occluderRenderers.Add(renderer);
        }

        ApplySharedSortingLayer(playerRenderer);
        foreach (Renderer renderer in occluderRenderers)
            ApplySharedSortingLayer(renderer);
    }

    void RefreshFurnitureOrders()
    {
        foreach (Renderer renderer in occluderRenderers)
        {
            if (renderer == null)
                continue;

            ApplySharedSortingLayer(renderer);
            renderer.sortingOrder = CalculateOrder(DepthPoint(renderer));
        }
    }

    void RefreshPlayerOrder()
    {
        if (playerRenderer == null)
            return;

        ApplySharedSortingLayer(playerRenderer);
        playerRenderer.sortingOrder =
            CalculateOrder(DepthPoint(playerRenderer)) + playerOrderOffset;
    }

    void ApplySharedSortingLayer(Renderer target)
    {
        if (target != null && !string.IsNullOrWhiteSpace(sharedSortingLayer))
            target.sortingLayerName = sharedSortingLayer;
    }

    int CalculateOrder(float worldY) =>
        -Mathf.RoundToInt(worldY * Mathf.Max(1, orderScale));

    float DepthPoint(Renderer target)
    {
        Bounds bounds = target.bounds;
        Vector3 position = bounds.size.sqrMagnitude > 0f
            ? new Vector3(bounds.center.x, bounds.min.y, 0f)
            : target.transform.position;
        return position.y - position.x * horizontalDepthWeight;
    }
}
