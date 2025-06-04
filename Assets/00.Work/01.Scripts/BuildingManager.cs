using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GridBuildManager : MonoBehaviour
{
    [SerializeField] private GameObject[] buildPrefabs;
    [SerializeField] private Grid mapGrid;
    [SerializeField] private LayerMask buildableLayer;
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;

    private GameObject preview;
    private bool isBuilding = false;
    private int selectedIndex = 0;
    private HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();

    void Update()
    {
        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            ToggleBuildMode();
        }

        if (!isBuilding || preview == null) return;

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, buildableLayer))
        {
            Vector3Int baseCell = mapGrid.WorldToCell(hit.point);
            Vector3Int targetCell = baseCell;

            // Check for existing blocks at the base cell and stack above
            while (occupiedCells.Contains(targetCell))
            {
                targetCell.y += 1;
            }

            Vector3 cellCenter = mapGrid.GetCellCenterWorld(targetCell);
            preview.transform.position = cellCenter;

            bool canPlace = !occupiedCells.Contains(targetCell);
            SetPreviewMaterial(canPlace);

            if (Mouse.current.leftButton.wasPressedThisFrame && canPlace)
            {
                Instantiate(buildPrefabs[selectedIndex], cellCenter, Quaternion.identity);
                occupiedCells.Add(targetCell);
            }
        }
    }

    void ToggleBuildMode()
    {
        isBuilding = !isBuilding;

        if (isBuilding)
        {
            if (preview != null) Destroy(preview);
            preview = Instantiate(buildPrefabs[selectedIndex]);
            ApplyPreviewMaterial(preview);

            if (preview.GetComponent<Collider>() == null)
            {
                preview.AddComponent<BoxCollider>();
            }
        }
        else
        {
            if (preview != null) Destroy(preview);
        }
    }

    void ApplyPreviewMaterial(GameObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            r.material = validMaterial;
            r.material.renderQueue = 3000;
        }
    }

    void SetPreviewMaterial(bool canPlace)
    {
        Material mat = canPlace ? validMaterial : invalidMaterial;
        foreach (Renderer r in preview.GetComponentsInChildren<Renderer>())
        {
            r.material = mat;
            r.material.renderQueue = 3000;
        }
    }
}
