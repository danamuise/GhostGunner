// NukeSequence.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NukeSequence : MonoBehaviour
{
    public ExplosionPool explosionPool;
    public float instanceDelay = 0.5f;
    public float explodeDuration = 5f;
    public GhostShooter gun;

    private Coroutine explosionRoutine;
    private readonly List<GameObject> activeExplosions = new List<GameObject>();

    private int totalExplosionsSpawned = 0;
    public int maxExplosions = 20;

    void Start()
    {
        SFXManager.Instance.StopMusic();
        SFXManager.Instance.Play("NukeSequenceSFX", 1.0f, 1.0f, 1.0f);

        // 🔒 Prevent enemies from advancing during the Nuke sequence
        TargetManager.SetAdvanceBlocked(true);
        Debug.Log("🚫 Target advance blocked — Nuke sequence started!");

        if (gun != null)
        {
            gun.DisableGun();
            Debug.Log("🔫 Gun disabled at the start of the Nuke Sequence!");
        }

        if (CameraShaker.Instance != null)
        {
            CameraShaker.Instance.Shake(10f, 0.05f);
            Debug.Log("💥 Camera shake triggered by Nuke Sequence!");
        }
    }

    public void InstantiateExplosions()
    {
        if (explosionRoutine != null)
            StopCoroutine(explosionRoutine);

        explosionRoutine = StartCoroutine(ExplosionRoutine());
    }

    private IEnumerator ExplosionRoutine()
    {
        float elapsedTime = 0f;
        totalExplosionsSpawned = 0;

        while (elapsedTime < explodeDuration && totalExplosionsSpawned < maxExplosions)
        {
            int explosionsToSpawn = Random.Range(3, 6);

            for (int i = 0; i < explosionsToSpawn && totalExplosionsSpawned < maxExplosions; i++)
            {
                float randomX = Random.Range(-2.9f, 2.9f);
                float randomY = Random.Range(-5.24f, 5.24f);
                Vector3 randomPosition = new Vector3(randomX, randomY, 0f);

                GameObject explosionObj = explosionPool.GetExplosion();
                explosionObj.transform.position = randomPosition;

                activeExplosions.Add(explosionObj);
                totalExplosionsSpawned++;

                // 🚀 Hide the explosion after 1.1 seconds
                StartCoroutine(DeactivateAfterDelay(explosionObj, 1.1f));
            }

            elapsedTime += instanceDelay;
            yield return new WaitForSeconds(instanceDelay);
        }

        CleanupExplosions();
    }

    private IEnumerator DeactivateAfterDelay(GameObject explosionObj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (explosionObj != null)
            explosionObj.SetActive(false);
    }

    private void CleanupExplosions()
    {
        activeExplosions.Clear(); // No destroy—let them finish naturally!
    }

    public void LoadChallengeLevel1()
    {
        // ✅ Unblock just before scene transition (next scene resets it too, but this is explicit)
        TargetManager.SetAdvanceBlocked(false);

        Debug.Log("🔄 Loading ChallengeLevel1 scene...");
        SceneManager.LoadScene("ChallengeLevel1");
    }
}
