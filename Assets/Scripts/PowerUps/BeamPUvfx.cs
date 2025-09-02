// 8/31/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using Mono.Cecil.Cil;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.UIElements.UxmlAttributeDescription;

namespace MagicArsenal
{
    public class BeamPUvfx : MonoBehaviour
    {
        [Header("Beam Settings")]
        public GameObject beamLineRendererPrefab;
        public GameObject beamStartPrefab;
        public GameObject beamEndPrefab;

        [Header("Beam Targeting")]
        private GameObject gridSystem; // Reference to the GridSystem GameObject
        private TargetGridManager gridManager; // Reference to the TargetGridManager component
        public float effectDuration = 2f;

        [Header("Beam Configuration")]
        public List<GameObject> beamTargets; // List of beam target GameObjects (e.g., beamTarget1, beamTarget2, etc.)
        public float offScreenOffset = 10f; // Offset for off-screen placement
        public PowerUpData powerUpData;
        private Vector2 beamStartPos;
        private Transform beamPUParent;

        // 9/1/2025 AI-Tag
        // This was created with the help of Assistant, a Unity Artificial Intelligence product.

        private void Start()
        {
            gridSystem = GameObject.Find("GridSystem");

            // Get the TargetGridManager component from the GridSystem GameObject
            if (gridSystem != null)
            {
                gridManager = gridSystem.GetComponent<TargetGridManager>();
            }

            if (gridManager == null)
            {
                Debug.LogError("[BeamPUvfx] TargetGridManager is not assigned or found on the GridSystem!");
                return;
            }

            // Find the BeamPU GameObject under PowerUpManager
            PowerUpManager powerUpManager = FindObjectOfType<PowerUpManager>();
            if (powerUpManager != null)
            {
                Transform powerUpParent = powerUpManager.transform;
                beamPUParent = powerUpParent.Find("BeamPU");

                if (beamPUParent == null)
                {
                    Debug.LogError("[BeamPUvfx] BeamPU GameObject not found under PowerUpManager!");
                }
            }
            else
            {
                Debug.LogError("[BeamPUvfx] PowerUpManager not found in the scene!");
            }
        }


        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Bullet"))
            {
                // Log the current world position
                Vector3 worldPosition = transform.position;
                
                // Ensure gridManager is not null
                if (gridManager == null)
                {
                    Debug.LogError("[BeamPUvfx] gridManager is null! Cannot retrieve grid coordinates.");
                    return;
                }

                // Get the grid coordinates (row, column)
                if (gridManager.GetGridCoordinates(worldPosition, out int col, out int row))
                {
                    Debug.Log($"[BeamPUvfx] BeamPUtarget hit at World Position: {worldPosition} | Grid Coordinates: Row={row}, Column={col}");
                }
                else
                {
                    Debug.LogWarning($"[BeamPUvfx] BeamPUtarget hit at World Position: {worldPosition}, but grid coordinates could not be determined.");
                }

                // Instantiate the BeamStartPrefab at the worldPosition
                if (beamStartPrefab != null)
                {
                    GameObject beamStart = Instantiate(beamStartPrefab, worldPosition, Quaternion.identity, beamPUParent);
                    Debug.Log($"[BeamPUvfx] BeamStartPrefab instantiated at {worldPosition}");
                } else
                {
                    Debug.Log($"[BeamPUvfx] BeamStartPrefab not found");
                }

                    // Capture the position of the power-up target
                    beamStartPos = transform.position;

                // Determine BeamTargets based on the BeamStartPos
                InitializeBeamTargets();

                // Spawn the beam effects
                SpawnBeams();

                // Camera shake
                CameraShaker.Instance?.Shake(0.2f, 0.15f);

                // Pickup VFX/SFX
                PowerUpManager manager = FindObjectOfType<PowerUpManager>();
                if (manager != null && powerUpData != null)
                {
                    manager.PlayPickupEffects(transform.position, powerUpData);
                }

                // Destroy all children of BeamPU after the effect duration
                Invoke(nameof(DestroyBeamPUChildren), effectDuration);

                // Optionally destroy the power-up target after the beam effect
                Destroy(gameObject, effectDuration);
            }
        }

        private void InitializeBeamTargets()
        {
            // Get the grid coordinates of the BeamStartPos
            if (!gridManager.GetGridCoordinates(beamStartPos, out int col, out int row))
            {
                Debug.LogWarning("[BeamPUvfx] BeamStartPos is out of grid bounds! No beams will be spawned.");
                return;
            }

            // Clear the beamTargets list to avoid duplicates
            beamTargets.Clear();

            // Search for targets in all four directions
            SearchForTargetInDirection(col, row, 1, 0);  // Right
            SearchForTargetInDirection(col, row, -1, 0); // Left
            SearchForTargetInDirection(col, row, 0, 1);  // Up
            SearchForTargetInDirection(col, row, 0, -1); // Down

            // Log the beamTargets list for debugging
            Debug.Log($"[BeamPUvfx] BeamTargets initialized. Total targets: {beamTargets.Count}");
            foreach (var target in beamTargets)
            {
                Debug.Log($"[BeamPUvfx] BeamTarget: {target.name} at {target.transform.position}");
            }
        }

        private void AddBeamTargetIfOccupied(int col, int row)
        {
            if (gridManager.IsCellInBounds(col, row) && gridManager.IsCellOccupied(col, row))
            {
                Vector2 targetPos = gridManager.GetWorldPosition(col, row);
                beamTargets.Add(new GameObject { transform = { position = targetPos } });
                Debug.Log("Adding to BeamTargets");
            }
        }


        private void SpawnBeams()
        {
            Debug.Log($"[BeamPUvfx] SpawnBeams called. Total beamTargets: {beamTargets.Count}");

            gameObject.SetActive(false);

            // Spawn beams to each target
            foreach (GameObject target in beamTargets)
            {
                if (target == null) continue; // Skip invalid targets

                Vector2 targetPos = target.transform.position;

                // Spawn the beam line renderer
                if (beamLineRendererPrefab != null)
                {
                    GameObject beam = Instantiate(beamLineRendererPrefab, beamPUParent); // Parent to BeamPU
                    LineRenderer lineRenderer = beam.GetComponent<LineRenderer>();
                    if (lineRenderer != null)
                    {
                        lineRenderer.SetPosition(0, beamStartPos);
                        lineRenderer.SetPosition(1, targetPos);
                    }
                    Debug.Log($"[BeamPUvfx] Beam instantiated from {beamStartPos} to {targetPos}");
                }

                // Spawn the beam end effect
                if (beamEndPrefab != null)
                {
                    GameObject beamEnd = Instantiate(beamEndPrefab, targetPos, Quaternion.identity, beamPUParent); // Parent to BeamPU
                    Debug.Log($"[BeamPUvfx] BeamEnd instantiated at {targetPos}");
                }
            }

            // Call DestroyTargets after all beams are spawned
            DestroyTargets();
        }


        private void SearchForTargetInDirection(int startCol, int startRow, int colStep, int rowStep)
        {
            int col = startCol;
            int row = startRow;

            Debug.Log($"[BeamPUvfx] Searching from col={startCol}, row={startRow} in direction colStep={colStep}, rowStep={rowStep}");

            while (gridManager.IsCellInBounds(col, row))
            {
                // Move to the next cell in the specified direction
                col += colStep;
                row += rowStep;

                // Check if the cell is occupied
                if (gridManager.IsCellOccupied(col, row))
                {
                    GameObject potentialTarget = gridManager.GetTargetAt(col, row);

                    // Validate the target: Check if it has the correct tag or component
                    if (potentialTarget != null && potentialTarget.CompareTag("Target"))
                    {
                        if (!beamTargets.Contains(potentialTarget))
                        {
                            beamTargets.Add(potentialTarget);
                            Debug.Log($"[BeamPUvfx] Found valid target at col={col}, row={row}: {potentialTarget.name}");
                        }
                        return; // Stop searching in this direction after finding a valid target
                    }
                    else
                    {
                        Debug.Log($"[BeamPUvfx] Ignored invalid target at col={col}, row={row}: {potentialTarget?.name}");
                    }
                }
            }

            // If no target is found, log the result
            Debug.Log($"[BeamPUvfx] No valid target found in direction colStep={colStep}, rowStep={rowStep}");
        }

        // 9/1/2025 AI-Tag
        // This was created with the help of Assistant, a Unity Artificial Intelligence product.

        private void DestroyTargets()
        {
            foreach (GameObject target in beamTargets)
            {
                if (target == null) continue;

                TargetBehavior targetBehavior = target.GetComponent<TargetBehavior>();
                if (targetBehavior != null)
                {
                    // Destroy the target and award points
                    targetBehavior.ApplyDamage(targetBehavior.GetCurrentHealth(), DamageSource.BeamPowerUp);
                    Debug.Log($"[BeamPUvfx] Destroyed target: {target.name}");
                }
                else
                {
                    // If no TargetBehavior is found, destroy the target directly
                    Destroy(target);
                    Debug.LogWarning($"[BeamPUvfx] Destroyed target without TargetBehavior: {target.name}");
                }
            }

            // Clear the beamTargets list after destruction
            beamTargets.Clear();
        }

        // 9/1/2025 AI-Tag
        // This was created with the help of Assistant, a Unity Artificial Intelligence product.

        private void DestroyBeamPUChildren()
        {
            if (beamPUParent != null)
            {
                foreach (Transform child in beamPUParent)
                {
                    Destroy(child.gameObject);
                }
                Debug.Log("[BeamPUvfx] All children of BeamPU destroyed.");
            }
            else
            {
                Debug.LogWarning("[BeamPUvfx] BeamPU parent is null. No children to destroy.");
            }
        }
    }
}