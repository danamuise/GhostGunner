using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicArsenal
{
    // BeamPUvfx.cs
    // - Multi-beam VFX manager (start VFX, line renderers, end VFX)
    // - PLUS FrostBeamPU cardinal sweep logic using TargetGridManager
    //
    // Usage:
    //   1) Assign TargetGridManager and set beam prefabs + BeamStartPos in inspector.
    //   2) On FrostBeamPU hit, call TriggerFrostBeamFromPU(worldHitPos).
    //
    // Notes:
    //   - End VFX are parented to internal "proxy" endpoints so they persist after targets are destroyed.
    //   - If no immediate neighbor in a direction, no beam is spawned in that direction (and Offshoot is disabled if assigned).
    //   - Scoring uses GameState.Instance.CurrentScore (fallback logs if not present).

    public class BeamPUvfx : MonoBehaviour
    {
        // -------------------- VFX: Prefabs & Scene refs --------------------
        [Header("Prefabs (from Magic Arsenal)")]
        [Tooltip("Prefab with a LineRenderer (the beam itself).")]
        public GameObject beamLineRendererPrefab;
        [Tooltip("VFX prefab at the beam origin.")]
        public GameObject beamStartPrefab;
        [Tooltip("VFX prefab at each beam end/target.")]
        public GameObject beamEndPrefab;

        [Header("Scene References")]
        [Tooltip("Where the Beam Start VFX should be instantiated.")]
        public Transform beamStartPos;

        [Tooltip("Auto-find up to four targets named beamTarget1..4 if enabled (legacy).")]
        public bool autoFindNamedTargets = true;

        [Tooltip("Targets the beams will end at. If empty and auto-find is on, will search for beamTarget1..4 by name.")]
        public List<Transform> beamTargets = new List<Transform>(4);

        // -------------------- Beam Visual Options --------------------
        [Header("Beam Visual Options")]
        [Tooltip("If > 0, this value overrides the prefab’s start width.")]
        public float baseWidth = -1f;
        public float textureScrollSpeed = 0f;  // +/- scroll along beam
        public float textureLengthScale = 1f;  // texture tiling scale (distance/this)

        [Header("Width Pulse Options")]
        [Tooltip("Multiplies the starting width to get the pulse max width.")]
        public float widthMultiplier = 1.5f;
        public float pulseSpeed = 1.0f;

        [Header("Runtime")]
        [SerializeField] private bool spawnOnStart = false; // false by default; FrostBeam will build when triggered
        [SerializeField] private bool drawGizmos = true;

        // -------------------- Optional: Cardinal Offshoots & Legacy target empties --------------------
        [Header("Optional Cardinal Offshoots (enable/disable per direction)")]
        public GameObject offshootUp;
        public GameObject offshootDown;
        public GameObject offshootLeft;
        public GameObject offshootRight;

        [Header("Optional Legacy BeamTarget Empties (toggled active if used)")]
        public bool manageLegacyBeamTargetEmpties = false;
        public Transform legacyBeamTargetUp;
        public Transform legacyBeamTargetDown;
        public Transform legacyBeamTargetLeft;
        public Transform legacyBeamTargetRight;

        // -------------------- Frost Beam PU: Grid + Logic --------------------
        [Header("FrostBeamPU Settings")]
        [Tooltip("Grid manager that knows rows/cols and world positions.")]
        public TargetGridManager gridManager;
        [Tooltip("How long the Frost Beam visual runs.")]
        public float effectDuration = 2.0f;

        [Tooltip("Award score as sum of destroyed target health.")]
        public bool awardByHealth = true;

        [Tooltip("Default health if no recognizable health component is present.")]
        public int defaultTargetHealth = 1;

        // --- internals ---
        private GameObject _startFX;

        private class BeamInstance
        {
            public GameObject beamGO;
            public LineRenderer line;
            public GameObject endFX;
            public Transform target;     // proxy endpoint transform

            // width pulse state
            public float originalWidth;
            public float customWidth;
            public float lerpValue;
            public bool pulseExpanding = true;
        }

        private readonly List<BeamInstance> _beams = new List<BeamInstance>(4);
        private readonly List<Transform> _endpointProxies = new List<Transform>(4); // created when FrostBeam builds beams
        private bool _sequenceRunning;

        private void Start()
        {
            if (spawnOnStart)
            {
                BuildBeams(); // usually not needed for FrostBeam; left for compatibility/testing
            }
        }

        // ======================== PUBLIC: Trigger Frost Beam ========================
        /// <summary>
        /// Call this when the FrostBeamPU is shot. Uses world position to anchor the effect.
        /// </summary>
        public void TriggerFrostBeamFromPU(Vector2 worldHitPosition)
        {
            if (_sequenceRunning)
                return;

            if (gridManager == null)
            {
                Debug.LogError("[BeamPUvfx] gridManager is not assigned.");
                return;
            }
            if (beamStartPos == null)
            {
                Debug.LogError("[BeamPUvfx] beamStartPos is not assigned.");
                return;
            }

            StartCoroutine(FrostBeamSequence(worldHitPosition));
        }

        // ======================== Frost Beam Sequence ========================
        private IEnumerator FrostBeamSequence(Vector2 startWorldPos)
        {
            _sequenceRunning = true;

            // 1) Set the BeamStart
            beamStartPos.position = new Vector3(startWorldPos.x, startWorldPos.y, 0f);

            // 2) Convert to grid indices
            if (!gridManager.GetGridCoordinates(startWorldPos, out int startCol, out int startRow))
            {
                Debug.LogWarning("[BeamPUvfx] Start position is outside grid; aborting Frost Beam.");
                _sequenceRunning = false;
                yield break;
            }

            // 3) Build direction chains (stop if no immediate neighbor)
            var leftChain = BuildChain(startCol, startRow, -1, 0, out bool leftOn);
            var rightChain = BuildChain(startCol, startRow, 1, 0, out bool rightOn);
            var upChain = BuildChain(startCol, startRow, 0, -1, out bool upOn);    // up = smaller row index
            var downChain = BuildChain(startCol, startRow, 0, 1, out bool downOn);  // down = larger row index

            // 4) Toggle offshoots / legacy targets
            SetOffshoots(upOn, downOn, leftOn, rightOn);
            if (manageLegacyBeamTargetEmpties)
                ToggleLegacyBeamTargetEmpties(upOn, downOn, leftOn, rightOn);

            // 5) Create beam endpoint proxies for farthest targets in each active direction
            beamTargets.Clear();
            ClearEndpointProxies();

            CreateProxyForChainEnd(leftChain, leftOn);
            CreateProxyForChainEnd(rightChain, rightOn);
            CreateProxyForChainEnd(upChain, upOn);
            CreateProxyForChainEnd(downChain, downOn);

            // 6) Build VFX (start + line renderers + end FX) to those proxies
            BuildBeams();

            // 7) Destroy all targets in active chains & award score
            int totalScore = 0;
            totalScore += DestroyChainAndScore(leftChain);
            totalScore += DestroyChainAndScore(rightChain);
            totalScore += DestroyChainAndScore(upChain);
            totalScore += DestroyChainAndScore(downChain);

            if (awardByHealth && totalScore > 0)
            {
                try
                {
                    GameState.Instance.CurrentScore += totalScore;
                    Debug.Log($"[BeamPUvfx/FrostBeam] Awarded {totalScore} points. New total: {GameState.Instance.CurrentScore}");
                }
                catch
                {
                    Debug.Log($"[BeamPUvfx/FrostBeam] Awarded {totalScore} points (GameState not found).");
                }
            }

            // 8) Hold visuals, then cleanup
            yield return new WaitForSeconds(effectDuration);

            DestroyAll();           // beams + end VFX + start VFX
            ClearEndpointProxies(); // proxy empties
            _sequenceRunning = false;
        }

        // Build a chain in dir (dCol, dRow). Returns list of cell (col,row,transform).
        private List<(int col, int row, Transform tfm)> BuildChain(int startCol, int startRow, int dCol, int dRow, out bool directionActive)
        {
            directionActive = false;
            var result = new List<(int, int, Transform)>();

            int col = startCol + dCol;
            int row = startRow + dRow;

            // immediate neighbor must exist (occupied) to activate this direction
            if (!gridManager.IsCellInBounds(col, row) || !gridManager.IsCellOccupied(col, row))
                return result;

            directionActive = true;

            // Walk while occupied
            while (gridManager.IsCellInBounds(col, row) && gridManager.IsCellOccupied(col, row))
            {
                var targetGO = gridManager.GetTargetAt(col, row);
                Transform t = targetGO != null ? targetGO.transform : null;
                result.Add((col, row, t));

                // advance
                col += dCol;
                row += dRow;
            }
            return result;
        }

        private void CreateProxyForChainEnd(List<(int col, int row, Transform tfm)> chain, bool active)
        {
            if (!active || chain == null || chain.Count == 0) return;

            var endCell = chain[chain.Count - 1];
            Vector2 endPos2 = gridManager.GetWorldPosition(endCell.col, endCell.row);
            var proxy = new GameObject($"BeamEndProxy_{endCell.col}_{endCell.row}").transform;
            proxy.SetParent(transform);
            proxy.position = new Vector3(endPos2.x, endPos2.y, 0f);

            _endpointProxies.Add(proxy);
            beamTargets.Add(proxy);
        }

        private int DestroyChainAndScore(List<(int col, int row, Transform tfm)> chain)
        {
            if (chain == null || chain.Count == 0) return 0;

            int score = 0;
            for (int i = 0; i < chain.Count; i++)
            {
                var (col, row, tfm) = chain[i];

                // Try to fetch health for scoring
                int hp = defaultTargetHealth;
                if (tfm != null)
                    hp = Mathf.Max(defaultTargetHealth, TryGetHealth(tfm.gameObject));

                score += awardByHealth ? hp : 0;

                // Kill/destroy + mark grid free
                if (tfm != null)
                {
                    // If target has a Kill method, use it, otherwise destroy.
                    var killed = TryKill(tfm.gameObject);
                    if (!killed)
                        Destroy(tfm.gameObject);
                }

                gridManager.MarkCellOccupied(col, row, false);
            }
            return score;
        }

        // -------------------- Offshoot/Legacy toggles --------------------
        private void SetOffshoots(bool upOn, bool downOn, bool leftOn, bool rightOn)
        {
            if (offshootUp) offshootUp.SetActive(upOn);
            if (offshootDown) offshootDown.SetActive(downOn);
            if (offshootLeft) offshootLeft.SetActive(leftOn);
            if (offshootRight) offshootRight.SetActive(rightOn);
        }

        private void ToggleLegacyBeamTargetEmpties(bool upOn, bool downOn, bool leftOn, bool rightOn)
        {
            if (legacyBeamTargetUp) legacyBeamTargetUp.gameObject.SetActive(upOn);
            if (legacyBeamTargetDown) legacyBeamTargetDown.gameObject.SetActive(downOn);
            if (legacyBeamTargetLeft) legacyBeamTargetLeft.gameObject.SetActive(leftOn);
            if (legacyBeamTargetRight) legacyBeamTargetRight.gameObject.SetActive(rightOn);
        }

        private void ClearEndpointProxies()
        {
            for (int i = 0; i < _endpointProxies.Count; i++)
            {
                var p = _endpointProxies[i];
                if (p) Destroy(p.gameObject);
            }
            _endpointProxies.Clear();
        }

        // ======================== VFX Build / Update / Destroy ========================
        /// <summary>Clears and rebuilds all beams/VFX based on current inspector settings.</summary>
        public void BuildBeams()
        {
            DestroyAll();

            if (beamStartPos == null)
            {
                Debug.LogError("[BeamPUvfx] BeamStartPos is not assigned.", this);
                return;
            }

            // Collect targets (auto-find or provided/proxies)
            var targets = GetTargets();
            if (targets.Count == 0)
            {
                Debug.LogWarning("[BeamPUvfx] No beam targets found/assigned. Nothing to build.", this);
                return;
            }

            // Spawn start VFX under BeamStartPos
            if (beamStartPrefab != null)
            {
                _startFX = Instantiate(beamStartPrefab, beamStartPos);
                _startFX.transform.localPosition = Vector3.zero;
                _startFX.transform.localRotation = Quaternion.identity;
            }

            // For each target: spawn beam + end VFX (parent end VFX to the target/proxy transform)
            foreach (var t in targets)
            {
                var bi = new BeamInstance { target = t };

                if (beamLineRendererPrefab == null)
                {
                    Debug.LogError("[BeamPUvfx] beamLineRendererPrefab is not assigned.", this);
                    continue;
                }

                bi.beamGO = Instantiate(beamLineRendererPrefab, transform);
                bi.beamGO.name = $"Beam_to_{t.name}";
                bi.line = bi.beamGO.GetComponent<LineRenderer>();
                if (bi.line == null)
                {
                    Debug.LogError("[BeamPUvfx] beamLineRendererPrefab has no LineRenderer.", bi.beamGO);
                    Destroy(bi.beamGO);
                    continue;
                }

#if UNITY_5_5_OR_NEWER
                bi.line.positionCount = 2;
#else
                bi.line.SetVertexCount(2);
#endif
                bi.line.useWorldSpace = true;

                // Clone material so tiling/offset edits don't affect shared
                if (bi.line.material != null)
                {
                    bi.line.material = new Material(bi.line.material);
                }

                // End VFX at target/proxy
                if (beamEndPrefab != null)
                {
                    bi.endFX = Instantiate(beamEndPrefab, t);
                    bi.endFX.transform.localPosition = Vector3.zero;
                    bi.endFX.transform.localRotation = Quaternion.identity;
                }

                // Width setup with inspector override
                bi.originalWidth = (baseWidth > 0f) ? baseWidth : bi.line.startWidth;
                bi.customWidth = bi.originalWidth * Mathf.Max(0f, widthMultiplier);

                _beams.Add(bi);
            }

            UpdateAllBeamsImmediate();
        }

        private List<Transform> GetTargets()
        {
            var list = new List<Transform>(4);

            // Use provided list first
            foreach (var t in beamTargets)
                if (t != null) list.Add(t);

            if (autoFindNamedTargets && list.Count < 4)
            {
                TryAddByName(list, "beamTarget1");
                TryAddByName(list, "beamTarget2");
                TryAddByName(list, "beamTarget3");
                TryAddByName(list, "beamTarget4");
            }

            // Dedup
            var unique = new HashSet<int>();
            var final = new List<Transform>(list.Count);
            foreach (var t in list)
            {
                if (t == null) continue;
                if (unique.Add(t.GetInstanceID()))
                    final.Add(t);
            }
            return final;
        }

        private void TryAddByName(List<Transform> list, string name)
        {
            if (list.Count >= 4) return;
            var found = GameObject.Find(name);
            if (found != null && found.transform != null && !list.Contains(found.transform))
                list.Add(found.transform);
        }

        private void Update()
        {
            if (_beams.Count == 0 || beamStartPos == null)
                return;

            // Position/orient start FX (toward first target)
            if (_startFX != null && _beams.Count > 0)
            {
                var first = _beams[0].target;
                if (first != null)
                {
                    _startFX.transform.position = beamStartPos.position;
                    _startFX.transform.rotation = Quaternion.LookRotation(first.position - beamStartPos.position, Vector3.up);
                }
            }

            // Update each beam
            foreach (var bi in _beams)
            {
                if (bi.target == null || bi.line == null) continue;

                var start = beamStartPos.position;
                var end = bi.target.position;

                // Line positions
                bi.line.SetPosition(0, start);
                bi.line.SetPosition(1, end);

                // Orient end FX
                if (bi.endFX != null)
                {
                    bi.endFX.transform.position = end;
                    bi.endFX.transform.rotation = Quaternion.LookRotation(start - end, Vector3.up);
                }

                // Texture tiling & scroll
                var dist = Vector3.Distance(start, end);
                if (bi.line.material != null)
                {
                    var scale = bi.line.material.mainTextureScale;
                    scale.x = textureLengthScale <= 0f ? 1f : dist / textureLengthScale;
                    bi.line.material.mainTextureScale = scale;

                    var offset = bi.line.material.mainTextureOffset;
                    offset.x -= Time.deltaTime * textureScrollSpeed;
                    bi.line.material.mainTextureOffset = offset;
                }

                // Width pulsing
                if (bi.pulseExpanding) bi.lerpValue += Time.deltaTime * pulseSpeed;
                else bi.lerpValue -= Time.deltaTime * pulseSpeed;

                if (bi.lerpValue >= 1f) { bi.pulseExpanding = false; bi.lerpValue = 1f; }
                else if (bi.lerpValue <= 0f) { bi.pulseExpanding = true; bi.lerpValue = 0f; }

                var currentWidth = Mathf.Lerp(bi.originalWidth, bi.customWidth, Mathf.Sin(bi.lerpValue * Mathf.PI));
                bi.line.startWidth = currentWidth;
                bi.line.endWidth = currentWidth;
            }
        }

        /// <summary>Immediately updates beam lines/orientation once (useful after moving targets in-editor).</summary>
        public void UpdateAllBeamsImmediate()
        {
            if (_beams.Count == 0 || beamStartPos == null) return;

            foreach (var bi in _beams)
            {
                if (bi.target == null || bi.line == null) continue;

                var start = beamStartPos.position;
                var end = bi.target.position;

                bi.line.SetPosition(0, start);
                bi.line.SetPosition(1, end);

                if (_startFX != null)
                {
                    _startFX.transform.position = start;
                    _startFX.transform.rotation = Quaternion.LookRotation(end - start, Vector3.up);
                }

                if (bi.endFX != null)
                {
                    bi.endFX.transform.position = end;
                    bi.endFX.transform.rotation = Quaternion.LookRotation(start - end, Vector3.up);
                }

                var dist = Vector3.Distance(start, end);
                if (bi.line.material != null)
                {
                    var scale = bi.line.material.mainTextureScale;
                    scale.x = textureLengthScale <= 0f ? 1f : dist / textureLengthScale;
                    bi.line.material.mainTextureScale = scale;
                }
            }
        }

        /// <summary>Clears all spawned objects and state (beams + start/end FX).</summary>
        public void DestroyAll()
        {
            for (int i = 0; i < _beams.Count; i++)
            {
                var bi = _beams[i];
                if (bi.endFX != null) Destroy(bi.endFX);
                if (bi.beamGO != null) Destroy(bi.beamGO);
            }
            _beams.Clear();

            if (_startFX != null)
            {
                Destroy(_startFX);
                _startFX = null;
            }
        }

        private void OnDisable()
        {
            // If you want beams to clear on disable, uncomment:
            // DestroyAll();
            // ClearEndpointProxies();
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || beamStartPos == null) return;

            Gizmos.DrawSphere(beamStartPos.position, 0.05f);

            var targets = Application.isPlaying ? _beams.ConvertAll(b => b.target) : beamTargets;
            foreach (var t in targets)
            {
                if (t == null) continue;
                Gizmos.DrawLine(beamStartPos.position, t.position);
                Gizmos.DrawWireSphere(t.position, 0.05f);
            }
        }

        // ======================== Utilities: Health / Kill ========================
        private int TryGetHealth(GameObject go)
        {
            // Try a few likely components/fields without hard dependency.
            // 1) GridTarget
            if (go.TryGetComponent(out GridTarget gt))
                return Mathf.Max(1, gt.Health);

            // 2) Common script names (Target, TargetHealth). Use reflection carefully.
            Component comp = go.GetComponent("TargetHealth") ?? go.GetComponent("Target") ?? go.GetComponent("Health");
            if (comp != null)
            {
                var type = comp.GetType();
                var f = type.GetField("Health") ?? type.GetField("health") ?? type.GetField("CurrentHealth") ?? type.GetField("currentHealth");
                if (f != null && f.FieldType == typeof(int))
                    return Mathf.Max(1, (int)f.GetValue(comp));

                var p = type.GetProperty("Health") ?? type.GetProperty("CurrentHealth");
                if (p != null && p.PropertyType == typeof(int))
                    return Mathf.Max(1, (int)p.GetValue(comp, null));
            }

            return Mathf.Max(1, defaultTargetHealth);
        }

        private bool TryKill(GameObject go)
        {
            // Look for Kill() on common components
            if (go.TryGetComponent(out GridTarget gt))
            {
                gt.Kill();
                return true;
            }

            // Reflection fallback: any Kill() method?
            var comps = go.GetComponents<MonoBehaviour>();
            foreach (var c in comps)
            {
                if (c == null) continue;
                var m = c.GetType().GetMethod("Kill", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (m != null && m.GetParameters().Length == 0)
                {
                    m.Invoke(c, null);
                    return true;
                }
            }
            return false;
        }
    }

    // Minimal GridTarget used in examples; remove if you already have one.
    public class GridTarget : MonoBehaviour
    {
        public int Row;
        public int Col;
        public int Health = 1;
        public void Kill() { Destroy(gameObject); }
    }
}
