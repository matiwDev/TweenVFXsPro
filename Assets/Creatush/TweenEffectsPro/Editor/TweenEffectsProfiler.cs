using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
using DG.Tweening;
using Creatush.TweenEffectsPro;

namespace Creatush.TweenEffectsPro.Editor
{
    /// <summary>
    /// Measures the CPU cost of BuildSequence() and Play() per effect.
    /// Shows live tween count while in Play Mode.
    /// Open via: Window > Creatush > TweenEffects Profiler
    /// </summary>
    public class TweenEffectsProfiler : EditorWindow
    {
        private struct SampleResult
        {
            public string effectName;
            public double buildMs;
            public double playMs;
            public int    tweenDelta;
        }

        private readonly List<SampleResult> _results = new List<SampleResult>();
        private VFXBehaviour _effect;
        private Transform    _sampleTarget;
        private Vector2      _scroll;
        private int          _totalPlaying;

        // Cached styles
        private GUIStyle _titleStyle;
        private GUIStyle _headerStyle;

        [MenuItem("Window/Creatush/TweenEffects Profiler")]
        public static void Open() => GetWindow<TweenEffectsProfiler>("TweenEffects Profiler");

        // Refresh tween counter automatically every editor update tick while in Play Mode
        private void OnInspectorUpdate()
        {
            if (!Application.isPlaying) return;
            int current = DOTween.TotalPlayingTweens();
            if (current == _totalPlaying) return;
            _totalPlaying = current;
            Repaint();
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;
            _titleStyle  = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            _headerStyle = new GUIStyle(EditorStyles.miniLabel)
                { fontStyle = FontStyle.Bold, normal = { textColor = Color.gray } };
        }

        private void OnGUI()
        {
            EnsureStyles();

            // ── Header ────────────────────────────────────────────────────────
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("⚡ TweenEffects Profiler", _titleStyle);
            EditorGUILayout.LabelField(
                "Measures CPU cost of BuildSequence() and Play(). Requires Play Mode.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);
            DrawSeparator();

            // ── Sampler ───────────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("🎯 Sample", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _effect = (VFXBehaviour)EditorGUILayout.ObjectField(
                "Effect", _effect, typeof(VFXBehaviour), allowSceneObjects: true);

            _sampleTarget = (Transform)EditorGUILayout.ObjectField(
                "Target", _sampleTarget, typeof(Transform), allowSceneObjects: true);

            // Resolve sample target: use field if set, otherwise fall back to effect's own transform
            Transform resolvedTarget = _sampleTarget != null
                ? _sampleTarget
                : (_effect != null ? _effect.transform : null);

            bool canSample = Application.isPlaying && _effect != null && resolvedTarget != null;

            EditorGUI.BeginDisabledGroup(!canSample);
            if (GUILayout.Button("▶  Sample Now", GUILayout.Height(28)))
                Sample(resolvedTarget);
            EditorGUI.EndDisabledGroup();

            // Helpful status line so the user knows exactly why the button is greyed
            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to sample.", MessageType.Info);
            else if (_effect == null)
                EditorGUILayout.HelpBox("Assign an Effect.", MessageType.Info);

            EditorGUILayout.EndVertical();
            DrawSeparator();

            // ── Live counter ──────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("📊 Live Tweens", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            Color countColor = _totalPlaying > 100 ? Color.yellow : Color.white;
            var   countStyle = new GUIStyle(EditorStyles.boldLabel)
                { normal = { textColor = Application.isPlaying ? countColor : Color.gray } };

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Playing:", GUILayout.Width(60));
            GUILayout.Label(Application.isPlaying ? _totalPlaying.ToString() : "—", countStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("(auto-refreshes every editor tick)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            DrawSeparator();

            // ── Results ───────────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📋 Results", EditorStyles.boldLabel);
            if (_results.Count > 0 && GUILayout.Button("Clear", GUILayout.Width(50)))
                _results.Clear();
            EditorGUILayout.EndHorizontal();

            if (_results.Count == 0)
            {
                EditorGUILayout.HelpBox("No samples yet.", MessageType.None);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Effect",      _headerStyle, GUILayout.Width(150));
            GUILayout.Label("Build (ms)",  _headerStyle, GUILayout.Width(70));
            GUILayout.Label("Play (ms)",   _headerStyle, GUILayout.Width(70));
            GUILayout.Label("Tween Δ",     _headerStyle, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(300));
            foreach (var r in _results)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label(r.effectName, GUILayout.Width(150));
                GUILayout.Label(r.buildMs.ToString("0.000"),
                    Coloured(r.buildMs, 1.0, 5.0), GUILayout.Width(70));
                GUILayout.Label(r.playMs.ToString("0.000"),
                    Coloured(r.playMs, 0.5, 2.0), GUILayout.Width(70));
                GUILayout.Label($"+{r.tweenDelta}",
                    Coloured(r.tweenDelta, 5, 20), GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void Sample(Transform target)
        {
            int before = DOTween.TotalPlayingTweens();

            var swBuild = Stopwatch.StartNew();
            Sequence seq = _effect.BuildSequence(0, 1, target);
            swBuild.Stop();

            var swPlay = Stopwatch.StartNew();
            seq.Play();
            swPlay.Stop();

            _totalPlaying = DOTween.TotalPlayingTweens();

            _results.Add(new SampleResult
            {
                effectName = _effect.GetType().Name,
                buildMs    = swBuild.Elapsed.TotalMilliseconds,
                playMs     = swPlay.Elapsed.TotalMilliseconds,
                tweenDelta = _totalPlaying - before,
            });

            Repaint();
        }

        private static GUIStyle Coloured(double v, double warn, double danger)
        {
            Color c = v >= danger ? new Color(1f, 0.3f, 0.3f)
                    : v >= warn   ? new Color(1f, 0.85f, 0.2f)
                    : Color.white;
            return new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = c } };
        }

        private static GUIStyle Coloured(int v, int warn, int danger)
            => Coloured((double)v, warn, danger);

        private static void DrawSeparator()
        {
            var r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(0.3f, 0.3f, 0.3f));
        }
    }
}
