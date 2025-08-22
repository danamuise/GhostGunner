using System.Collections.Generic;
using UnityEngine;

namespace MagicArsenal
{
    // Filename: beamPUvfx.cs
    // Purpose: Spawn 1–4 beams from BeamStartPos to beamTarget1..4.
    // - Instantiates one Beam Start VFX under BeamStartPos
    // - Instantiates a LineRenderer beam + Beam End VFX at each target
    // - Updates positions, texture scroll/scale, and width pulsing per-beam
    public class BeamPUvfx : MonoBehaviour
    {
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

        [Tooltip("Auto-find up to four targets named beamTarget1..4 if enabled.")]
        public bool autoFindNamedTargets = true;

        [Tooltip("Targets the beams will end at. If empty and auto-find is on, will search for beamTarget1..4 by name.")]
        public List<Transform> beamTargets = new List<Transform>(4);

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
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private bool drawGizmos = true;



        // --- internals ---
        private GameObject _startFX;

        private class BeamInstance
        {
            public GameObject beamGO;
            public LineRenderer line;
            public GameObject endFX;
            public Transform target;

            // width pulse state
            public float originalWidth;
            public float customWidth;
            public float lerpValue;
            public bool pulseExpanding = true;
        }

        private readonly List<BeamInstance> _beams = new List<BeamInstance>(4);

        private void Start()
        {
            if (spawnOnStart)
            {
                BuildBeams();
            }
        }

        /// <summary>
        /// Clears and rebuilds all beams/VFX based on current inspector settings.
        /// </summary>
        public void BuildBeams()
        {
            DestroyAll();

            if (beamStartPos == null)
            {
                Debug.LogError("[beamPUvfx] BeamStartPos is not assigned.", this);
                return;
            }

            // Collect targets (auto-find if requested)
            var targets = GetTargets();
            if (targets.Count == 0)
            {
                Debug.LogWarning("[beamPUvfx] No beam targets found/assigned. Nothing to build.", this);
                return;
            }

            // Spawn start VFX under BeamStartPos
            if (beamStartPrefab != null)
            {
                _startFX = Instantiate(beamStartPrefab, beamStartPos);
                _startFX.transform.localPosition = Vector3.zero;
                _startFX.transform.localRotation = Quaternion.identity;
                Debug.Log("[beamPUvfx] Spawned Beam Start VFX under BeamStartPos.", this);
            }
            else
            {
                Debug.LogWarning("[beamPUvfx] Beam Start Prefab not assigned; skipping start VFX.", this);
            }

            // For each target: spawn beam + end VFX
            foreach (var t in targets)
            {
                var bi = new BeamInstance { target = t };

                if (beamLineRendererPrefab == null)
                {
                    Debug.LogError("[beamPUvfx] beamLineRendererPrefab is not assigned.", this);
                    continue;
                }

                bi.beamGO = Instantiate(beamLineRendererPrefab, transform);
                bi.beamGO.name = $"Beam_to_{t.name}";
                bi.line = bi.beamGO.GetComponent<LineRenderer>();
                if (bi.line == null)
                {
                    Debug.LogError("[beamPUvfx] beamLineRendererPrefab has no LineRenderer.", bi.beamGO);
                    Destroy(bi.beamGO);
                    continue;
                }

#if UNITY_5_5_OR_NEWER
                bi.line.positionCount = 2;
#else
        bi.line.SetVertexCount(2);
#endif
                bi.line.useWorldSpace = true;

                // Clone material instance so tiling/offset don't affect shared material
                if (bi.line.material != null)
                {
                    bi.line.material = new Material(bi.line.material);
                }

                // End VFX at target
                if (beamEndPrefab != null)
                {
                    bi.endFX = Instantiate(beamEndPrefab, t);
                    bi.endFX.transform.localPosition = Vector3.zero;
                    bi.endFX.transform.localRotation = Quaternion.identity;
                }

                // --- Width setup with inspector override ---
                if (baseWidth > 0f)
                {
                    bi.originalWidth = baseWidth;
                }
                else
                {
                    bi.originalWidth = bi.line.startWidth; // use prefab default
                }
                bi.customWidth = bi.originalWidth * Mathf.Max(0f, widthMultiplier);

                _beams.Add(bi);
            }

            Debug.Log($"[beamPUvfx] Built {_beams.Count} beam(s).", this);

            // Initial orient
            UpdateAllBeamsImmediate();
        }


        private List<Transform> GetTargets()
        {
            var list = new List<Transform>(4);

            // Use provided list first
            foreach (var t in beamTargets)
            {
                if (t != null) list.Add(t);
            }

            if (autoFindNamedTargets)
            {
                // Fill missing with named lookups
                TryAddByName(list, "beamTarget1");
                TryAddByName(list, "beamTarget2");
                TryAddByName(list, "beamTarget3");
                TryAddByName(list, "beamTarget4");
            }

            // Dedup (by instance id)
            var unique = new HashSet<int>();
            var final = new List<Transform>(4);
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

            // Position/orient start FX toward an average direction (optional)
            if (_startFX != null && _beams.Count > 0)
            {
                // Look at first valid target for simple orientation
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

                // Aim end/start FX
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

        /// <summary>
        /// Immediately updates beam lines/orientation once (useful after moving targets in-editor).
        /// </summary>
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

        /// <summary>
        /// Clears all spawned objects and state.
        /// </summary>
        public void DestroyAll()
        {
            // beams + ends
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
            // (Optional) keep things around; if you prefer, destroy on disable:
            // DestroyAll();
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || beamStartPos == null) return;

            Gizmos.DrawSphere(beamStartPos.position, 0.05f);

            // Show intended links
            var targets = Application.isPlaying ? _beams.ConvertAll(b => b.target) : beamTargets;
            foreach (var t in targets)
            {
                if (t == null) continue;
                Gizmos.DrawLine(beamStartPos.position, t.position);
                Gizmos.DrawWireSphere(t.position, 0.05f);
            }
        }
    }
}
