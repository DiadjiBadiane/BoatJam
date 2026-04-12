// Assets/Scripts/UI/BoatBuoyancy.cs
// Applies gentle bobbing and rocking to every boat so they look like
// they're floating on water — even when stationary.
// Auto-bootstraps via SceneManager.sceneLoaded (no manual setup).

using UnityEngine;
using UnityEngine.SceneManagement;

public class BoatBuoyancy : MonoBehaviour
{
    // ── Auto-bootstrap ────────────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TrySpawnInCurrentScene();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TrySpawnInCurrentScene();

    static void TrySpawnInCurrentScene()
    {
        if (GridManager.Instance == null && Object.FindObjectOfType<GridManager>() == null) return;
        if (Object.FindObjectOfType<BoatBuoyancy>() != null) return;

        var go = new GameObject("BoatBuoyancy");
        go.AddComponent<BoatBuoyancy>();
    }

    // ── Config ────────────────────────────────────────────────────────────────
    const float BOB_AMPLITUDE  = 0.006f;  // very small vertical displacement
    const float BOB_SPEED      = 0.3f;    // very slow heave
    const float ROLL_AMPLITUDE = 0.35f;   // degrees of side-to-side tilt
    const float ROLL_SPEED     = 0.25f;   // gentle rocking
    const float PITCH_AMPLITUDE = 0.2f;   // degrees of front-to-back tilt
    const float PITCH_SPEED    = 0.18f;   // slowest axis for realism

    // ── Per-boat state ────────────────────────────────────────────────────────
    struct BoatState
    {
        public BoatMovement boat;
        public float phaseOffset;       // random per-boat so they don't sync
        public Vector3 baseLocalEuler;  // original rotation
    }

    BoatState[] _boats;
    int _count;

    void LateUpdate()
    {
        // Lazy-gather boats each frame (handles spawns/despawns without events)
        var allBoats = Object.FindObjectsOfType<BoatMovement>();
        if (allBoats.Length != _count)
            RebuildList(allBoats);

        float t = Time.time;

        for (int i = 0; i < _count; i++)
        {
            ref var s = ref _boats[i];
            if (s.boat == null) continue;

            float phase = s.phaseOffset;
            Transform tr = s.boat.transform;

            // ── Vertical bob ──────────────────────────────────────────
            float bob = Mathf.Sin((t * BOB_SPEED + phase) * Mathf.PI * 2f) * BOB_AMPLITUDE;
            Vector3 pos = tr.position;
            // We don't store a "base Y" — BoatMovement already drives position.
            // Instead we store it per-frame via a tag component approach:
            // Apply bob as a small offset on top of whatever position BoatMovement set.
            // LateUpdate runs after Update, so BoatMovement has already set position.
            pos.y += bob;
            tr.position = pos;

            // ── Rocking rotation ──────────────────────────────────────
            float roll  = Mathf.Sin((t * ROLL_SPEED  + phase * 1.3f) * Mathf.PI * 2f) * ROLL_AMPLITUDE;
            float pitch = Mathf.Sin((t * PITCH_SPEED + phase * 0.7f) * Mathf.PI * 2f) * PITCH_AMPLITUDE;

            Vector3 euler = s.baseLocalEuler;
            // Roll around the boat's length axis, pitch around the cross axis
            if (s.boat.isHorizontal)
            {
                euler.z += roll;   // side-to-side
                euler.x += pitch;  // front-to-back
            }
            else
            {
                euler.x += roll;
                euler.z += pitch;
            }
            tr.localEulerAngles = euler;
        }
    }

    void RebuildList(BoatMovement[] allBoats)
    {
        _count = allBoats.Length;
        _boats = new BoatState[_count];
        for (int i = 0; i < _count; i++)
        {
            _boats[i] = new BoatState
            {
                boat = allBoats[i],
                // Deterministic phase from position so it stays stable across rebuilds
                phaseOffset = Mathf.Abs(
                    Mathf.Sin(allBoats[i].transform.position.x * 17.3f +
                              allBoats[i].transform.position.z * 31.7f)) * 10f,
                baseLocalEuler = allBoats[i].transform.localEulerAngles
            };
        }
    }
}
