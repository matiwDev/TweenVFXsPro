using UnityEngine;
using UnityEditor;
using DG.Tweening;
using Creatush.TweenEffectsPro;

namespace Creatush.TweenEffectsPro.Editor
{
    public class EffectPreviewWindow : EditorWindow
    {
        private VFXBehaviour _effect;
        private Transform    _previewTarget;
        private Sequence     _activeSequence;
        private bool         _isPlaying;

        // Saved the moment Play is pressed — always reflects pre-play state
        private Vector3    _savedPos;
        private Quaternion _savedRot;
        private Vector3    _savedScale;
        private bool       _hasSavedState;

        private GUIStyle _titleStyle;

        [MenuItem("Window/Creatush/Effect Preview")]
        public static void Open() => GetWindow<EffectPreviewWindow>("Effect Preview");

        private void OnEnable()  => EditorApplication.update += Tick;
        private void OnDisable() { EditorApplication.update -= Tick; KillSequence(); }

        private void Tick()
        {
            if (!_isPlaying) return;
            DOTween.ManualUpdate(0.016f, 0.016f);
            Repaint();
        }

        private void OnGUI()
        {
            if (_titleStyle == null)
                _titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🎬 Effect Preview", _titleStyle);
            EditorGUILayout.LabelField(
                "Preview effects in Edit Mode — no Play Mode required.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var newEffect = (VFXBehaviour)EditorGUILayout.ObjectField(
                "Effect", _effect, typeof(VFXBehaviour), allowSceneObjects: true);
            if (newEffect != _effect) { KillSequence(); _effect = newEffect; }

            var newTarget = (Transform)EditorGUILayout.ObjectField(
                "Target", _previewTarget, typeof(Transform), allowSceneObjects: true);
            if (newTarget != _previewTarget) { KillSequence(); _previewTarget = newTarget; _hasSavedState = false; }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);

            EditorGUI.BeginDisabledGroup(_effect == null || _previewTarget == null);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(_isPlaying ? "⟳  Restart" : "▶  Play", GUILayout.Height(30)))
                StartPreview();

            if (_isPlaying && GUILayout.Button("■  Stop", GUILayout.Height(30), GUILayout.Width(80)))
            {
                KillSequence();
                RestoreState();
            }

            EditorGUI.BeginDisabledGroup(!_hasSavedState);
            if (GUILayout.Button("↺  Reset", GUILayout.Height(30), GUILayout.Width(70)))
                RestoreState();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);
            if (_isPlaying)
                EditorGUILayout.HelpBox("Playing — Scene View shows live changes.", MessageType.None);
            else if (_effect == null)
                EditorGUILayout.HelpBox("Assign a VFXBehaviour to preview.", MessageType.Info);
            else if (_previewTarget == null)
                EditorGUILayout.HelpBox("Assign a Target transform in the scene.", MessageType.Info);
            else
                EditorGUILayout.HelpBox("Ready. Press Play to preview.", MessageType.None);
        }

        private void StartPreview()
        {
            if (_effect == null || _previewTarget == null) return;
            if (Application.isPlaying)
            {
                Debug.LogWarning("[EffectPreview] Edit Mode only.");
                return;
            }

            // Kill running sequence first — don't touch transform yet
            KillSequence();

            // Save state NOW — always from the current position before any animation
            SaveState();

            Undo.RecordObject(_previewTarget, "Effect Preview");

            _activeSequence = _effect.BuildSequence(0, 1, _previewTarget);
            _activeSequence
                .SetUpdate(UpdateType.Manual)
                .OnComplete(() => { _isPlaying = false; Repaint(); })
                .OnKill(()     => { _isPlaying = false; Repaint(); });

            _activeSequence.Play();
            _isPlaying = true;
        }

        private void KillSequence()
        {
            _isPlaying = false;
            if (_activeSequence != null && _activeSequence.IsActive())
                _activeSequence.Kill();
            _activeSequence = null;
        }

        private void SaveState()
        {
            if (_previewTarget == null) return;
            _savedPos      = _previewTarget.localPosition;
            _savedRot      = _previewTarget.localRotation;
            _savedScale    = _previewTarget.localScale;
            _hasSavedState = true;
        }

        private void RestoreState()
        {
            if (_previewTarget == null || !_hasSavedState) return;
            Undo.RecordObject(_previewTarget, "Reset Preview Target");
            _previewTarget.localPosition = _savedPos;
            _previewTarget.localRotation = _savedRot;
            _previewTarget.localScale    = _savedScale;
        }
    }
}
