using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public Camera gameCamera;

    private BoatMovement selectedBoat = null;
    private Renderer[] selectedRenderers;
    private Color[] originalColors;

    // Highlight color when selected
    private static readonly Color HighlightColor = new Color(1f, 0.85f, 0f); // gold

    void Update()
    {
        // try to grab the main camera if user hasn't assigned one
        if (gameCamera == null)
            gameCamera = Camera.main;

        if (Input.GetMouseButtonDown(0))
            HandleClick();

        if (selectedBoat != null)
            HandleArrowKeys();
    }

    void HandleClick()
    {
        if (gameCamera == null)
        {
            Debug.LogWarning("InputHandler: no camera assigned for clicks.");
            return;
        }

        Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            BoatMovement bm = hit.collider.GetComponentInParent<BoatMovement>();
            if (bm != null)
            {
                if (selectedBoat == bm) return; // already selected, do nothing
                Deselect();
                Select(bm);
                return;
            }
        }

        Deselect(); // clicked empty space
    }

    void HandleArrowKeys()
    {
        if (selectedBoat == null || selectedBoat.IsMoving) return;

        if (Input.GetKeyDown(KeyCode.RightArrow)) selectedBoat.TryMove(Vector2Int.right);
        if (Input.GetKeyDown(KeyCode.LeftArrow))  selectedBoat.TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.UpArrow))    selectedBoat.TryMove(new Vector2Int(0, 1));
        if (Input.GetKeyDown(KeyCode.DownArrow))  selectedBoat.TryMove(new Vector2Int(0, -1));
    }

    void Select(BoatMovement boat) 
    {
        selectedBoat = boat;

        // Store original colors and apply highlight
        selectedRenderers = boat.GetComponentsInChildren<Renderer>();
        originalColors = new Color[selectedRenderers.Length];
        for (int i = 0; i < selectedRenderers.Length; i++)
        {
            // Each renderer may have its own material instance
            originalColors[i] = selectedRenderers[i].material.color;
            selectedRenderers[i].material.color = HighlightColor;
        }
    }

    void Deselect()
    {
        if (selectedBoat == null) return;

        // Restore original colors
        if (selectedRenderers != null && originalColors != null)
        {
            for (int i = 0; i < selectedRenderers.Length; i++)
            {
                if (selectedRenderers[i] != null)
                    selectedRenderers[i].material.color = originalColors[i];
            }
        }

        selectedBoat = null;
        selectedRenderers = null;
        originalColors = null;
    }
}