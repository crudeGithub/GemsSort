using System.Collections;
using System.Collections.Generic;
using GemsSort.Views;
using UnityEngine;

namespace GemsSort.Services
{
    public sealed class GemsSortEffects : MonoBehaviour
    {
        [Header("Shine Sweep")]
        [Tooltip("Vertical tolerance used to group gems into a row during shine sweeps.")]
        [SerializeField] private float rowGroupingTolerance = 0.15f;
        [Tooltip("Delay between each row in a shine sweep.")]
        [SerializeField] private float rowSweepDelay = 0.045f;

        [Header("Pulse")]
        [SerializeField] private float pulseDuration = 0.16f;
        [SerializeField] private float pulseScale = 0.18f;

        public void Shine(DiamondView view)
        {
            if (view != null)
            {
                StartCoroutine(Pulse(view));
            }
        }

        public void ShineSweep(IEnumerable<DiamondView> views)
        {
            StartCoroutine(Sweep(views));
        }

        [Header("Win Celebration")]
        [SerializeField] private GameObject winCelebrationPrefab;
        [SerializeField] private Vector3 winCelebrationOffset = new Vector3(0f, 0f, -1f);
        [SerializeField] private float winCelebrationLifetime = 3.5f;

        public void PlayWinCelebration()
        {
            if (winCelebrationPrefab == null)
            {
                return;
            }

            var camera = Camera.main;
            Vector3 spawnPos = camera != null
                ? new Vector3(camera.transform.position.x, camera.transform.position.y, 0f) + winCelebrationOffset
                : winCelebrationOffset;

            var instance = Instantiate(winCelebrationPrefab, spawnPos, Quaternion.identity);
            if (winCelebrationLifetime > 0f)
            {
                Destroy(instance, winCelebrationLifetime);
            }
        }

        private IEnumerator Sweep(IEnumerable<DiamondView> views)
        {
            if (views == null) yield break;

            var list = new List<DiamondView>();
            foreach (var v in views)
            {
                if (v != null) list.Add(v);
            }

            if (list.Count == 0) yield break;

            // Sort descending by Y coordinate (highest visual element first, sweeping top-to-bottom)
            list.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));

            // Group into horizontal rows (tolerance of 0.15 world space units)
            var rows = new List<List<DiamondView>>();
            foreach (var view in list)
            {
                bool placed = false;
                foreach (var rowGroup in rows)
                {
                    if (Mathf.Abs(rowGroup[0].transform.position.y - view.transform.position.y) < rowGroupingTolerance)
                    {
                        rowGroup.Add(view);
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    rows.Add(new List<DiamondView> { view });
                }
            }

            foreach (var rowGroup in rows)
            {
                foreach (var view in rowGroup)
                {
                    StartCoroutine(Pulse(view));
                }
                yield return new WaitForSeconds(Mathf.Max(0.01f, rowSweepDelay));
            }
        }

        private IEnumerator Pulse(DiamondView view)
        {
            if (view == null) yield break;

            Transform target = view.transform;
            Vector3 baseScale = target.localScale;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, pulseDuration);

            view.SetSolveAnimationVisuals(true);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
                target.localScale = baseScale * (1f + t * pulseScale);
                yield return null;
            }

            target.localScale = baseScale;
            view.SetSolveAnimationVisuals(false);
        }
    }
}
