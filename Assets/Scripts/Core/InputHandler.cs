using UnityEngine;
using UnityEngine.EventSystems;

public class InputHandler : MonoBehaviour
{
    public Camera gameCamera;

    [Header("Touch / Mouse Input")]
    [SerializeField] float swipeThresholdPixels = 45f;

    private BoatMovement _selectedBoat;
    private Renderer[]   _selectedRenderers;
    private Color[]      _originalColors;

    // Per-touch tracking
    private bool      _touchTracking;
    private bool      _swipeFired;          // one swipe allowed per finger-down event
    private Vector2   _touchStartPos;
    private int       _touchFingerId;

    // Highlight colour when selected
    private static readonly Color HighlightColor = new Color(1f, 0.85f, 0f);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Update()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        // Use touch when touches exist; fall back to mouse otherwise.
        // This lets you test in the Editor with the mouse AND works on device.
        if (Input.touchCount > 0)
            HandleTouchInput();
        else
            HandleMouseInput();

        if (_selectedBoat != null)
            HandleKeyboardInput();
    }

    // ── Mouse (editor / desktop) ──────────────────────────────────────────────

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI(-1)) return;
            SelectBoatAtScreenPoint(Input.mousePosition);

            // Start tracking for mouse-drag swipe
            _touchTracking  = true;
            _swipeFired     = false;
            _touchStartPos  = Input.mousePosition;
        }

        if (_touchTracking && !_swipeFired && _selectedBoat != null && !_selectedBoat.IsMoving)
        {
            Vector2 delta = (Vector2)Input.mousePosition - _touchStartPos;
            if (delta.sqrMagnitude >= swipeThresholdPixels * swipeThresholdPixels)
            {
                TryMoveFromDelta(delta);
                _swipeFired = true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            _touchTracking = false;
            _swipeFired    = false;
        }
    }

    // ── Touch ─────────────────────────────────────────────────────────────────

    void HandleTouchInput()
    {
        // Always process Began first so we don't miss it even when other phases exist.
        foreach (Touch touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began)
            {
                if (IsPointerOverUI(touch.fingerId)) return;

                _touchTracking  = true;
                _swipeFired     = false;
                _touchFingerId  = touch.fingerId;
                _touchStartPos  = touch.position;

                SelectBoatAtScreenPoint(touch.position);
                return;
            }
        }

        if (!_touchTracking || _selectedBoat == null) return;

        // Find our tracked finger
        Touch? tracked = null;
        foreach (Touch t in Input.touches)
            if (t.fingerId == _touchFingerId) { tracked = t; break; }

        if (tracked == null) { _touchTracking = false; return; }

        Touch current = tracked.Value;

        // End / cancel — reset tracking but do NOT deselect the boat.
        // This lets the player lift their finger and swipe again.
        if (current.phase == TouchPhase.Ended || current.phase == TouchPhase.Canceled)
        {
            _touchTracking = false;
            _swipeFired    = false;
            return;
        }

        // We allow re-swiping after each finger lift, but only ONE swipe per press
        if (_swipeFired || _selectedBoat.IsMoving) return;

        Vector2 delta = current.position - _touchStartPos;
        if (delta.sqrMagnitude < swipeThresholdPixels * swipeThresholdPixels) return;

        TryMoveFromDelta(delta);
        _swipeFired = true;
    }

    // ── Shared movement helper ────────────────────────────────────────────────

    void TryMoveFromDelta(Vector2 delta)
    {
        if (_selectedBoat == null || _selectedBoat.IsMoving) return;

        Vector2Int dir;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            dir = delta.x > 0f ? Vector2Int.right : Vector2Int.left;
        else
            dir = delta.y > 0f ? Vector2Int.up : Vector2Int.down;

        _selectedBoat.TryMove(dir);
    }

    // ── Keyboard (editor convenience) ────────────────────────────────────────

    void HandleKeyboardInput()
    {
        if (_selectedBoat == null || _selectedBoat.IsMoving) return;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) _selectedBoat.TryMove(Vector2Int.right);
        if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) _selectedBoat.TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.UpArrow)    || Input.GetKeyDown(KeyCode.W)) _selectedBoat.TryMove(Vector2Int.up);
        if (Input.GetKeyDown(KeyCode.DownArrow)  || Input.GetKeyDown(KeyCode.S)) _selectedBoat.TryMove(Vector2Int.down);
    }

    // ── Boat selection ────────────────────────────────────────────────────────

    void SelectBoatAtScreenPoint(Vector2 screenPos)
    {
        if (gameCamera == null)
        {
            Debug.LogWarning("InputHandler: no camera assigned.");
            return;
        }

        Ray ray = gameCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            BoatMovement bm = hit.collider.GetComponentInParent<BoatMovement>();
            if (bm != null)
            {
                if (_selectedBoat != bm)
                {
                    Deselect();
                    Select(bm);
                }
                return;
            }
        }

        Deselect();
    }

    // ── Selection highlight ───────────────────────────────────────────────────

    void Select(BoatMovement boat)
    {
        _selectedBoat      = boat;
        _selectedRenderers = boat.GetComponentsInChildren<Renderer>();
        _originalColors    = new Color[_selectedRenderers.Length];

        for (int i = 0; i < _selectedRenderers.Length; i++)
        {
            _originalColors[i]              = _selectedRenderers[i].material.color;
            _selectedRenderers[i].material.color = HighlightColor;
        }
    }

    void Deselect()
    {
        if (_selectedBoat == null) return;

        if (_selectedRenderers != null && _originalColors != null)
            for (int i = 0; i < _selectedRenderers.Length; i++)
                if (_selectedRenderers[i] != null)
                    _selectedRenderers[i].material.color = _originalColors[i];

        _selectedBoat      = null;
        _selectedRenderers = null;
        _originalColors    = null;
    }

    // ── UI hit-test ───────────────────────────────────────────────────────────

    bool IsPointerOverUI(int fingerId)
    {
        if (EventSystem.current == null) return false;
        return fingerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(fingerId)
            : EventSystem.current.IsPointerOverGameObject();
    }
}