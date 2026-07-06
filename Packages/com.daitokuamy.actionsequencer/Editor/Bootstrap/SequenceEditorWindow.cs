using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Sequence 編集用 Window
    /// </summary>
    public sealed class SequenceEditorWindow : EditorWindow {
        [SerializeField]
        private SequenceClip _escapedClip;

        [SerializeField]
        private int _escapedIncludeClipIndex;

        private SequenceEditorSession _session;

        /// <summary>
        /// SequenceClip を開く
        /// </summary>
        /// <param name="clip">開く SequenceClip</param>
        /// <param name="index">includeClip index</param>
        public static void Open(SequenceClip clip, int index = -1) {
            var window = GetWindow<SequenceEditorWindow>(ObjectNames.NicifyVariableName(nameof(SequenceEditorWindow)));
            window.Setup(clip, index, true);
        }

        /// <summary>
        /// メニューから空の EditorWindow を開く
        /// </summary>
        [MenuItem("Window/Sequence Tools/Sequence Editor Window")]
        private static void Open() {
            var window = GetWindow<SequenceEditorWindow>(ObjectNames.NicifyVariableName(nameof(SequenceEditorWindow)));
            window.Setup(null, -1, true);
        }

        /// <summary>
        /// Asset オープン時に Window を開く
        /// </summary>
        #if UNITY_6000_5_OR_NEWER
        /// <param name="entityId">開かれた asset の entity id</param>
        /// <param name="line">対象行番号</param>
        /// <returns>SequenceClip を処理した場合は true</returns>
        [OnOpenAsset(0)]
        public static bool OnOpenAsset(EntityId entityId, int line) {
            return OpenAsset(EditorUtility.EntityIdToObject(entityId));
        }
        #else
        /// <param name="instanceId">開かれた asset の instance id</param>
        /// <param name="line">対象行番号</param>
        /// <returns>SequenceClip を処理した場合は true</returns>
        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceId, int line) {
            return OpenAsset(EditorUtility.InstanceIDToObject(instanceId));
        }
        #endif

        private static bool OpenAsset(UnityEngine.Object asset) {
            if (asset is not SequenceClip clip) {
                return false;
            }

            Open(clip);
            return true;
        }

        /// <summary>
        /// 有効化時の初期化
        /// </summary>
        private void OnEnable() {
            _session = new SequenceEditorSession();
            _session.StateChanged += OnSessionStateChanged;

            var root = rootVisualElement;
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.daitokuamy.actionsequencer/Editor/Layout/sequence_editor_window.uxml");
            uxml.CloneTree(root);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.daitokuamy.actionsequencer/Editor/Layout/sequence_editor_window.uss");
            root.styleSheets.Add(styleSheet);

            _session.Initialize(root);
            _session.Open(_escapedClip, _escapedIncludeClipIndex, true);
        }

        /// <summary>
        /// 無効化時の終了処理
        /// </summary>
        private void OnDisable() {
            if (_session != null) {
                _session.StateChanged -= OnSessionStateChanged;
                _session.Dispose();
                _session = null;
            }
        }

        /// <summary>
        /// IMGUI 更新
        /// </summary>
        private void OnGUI() {
            _session?.OnGui();
        }

        /// <summary>
        /// 毎フレーム更新
        /// </summary>
        private void Update() {
            if (_session?.IsRepaintRequired() == true) {
                Repaint();
            }
        }

        /// <summary>
        /// セッション状態を Window のシリアライズ対象へ反映
        /// </summary>
        private void OnSessionStateChanged() {
            if (_session == null) {
                return;
            }

            _session.GetWindowState(out _escapedClip, out _escapedIncludeClipIndex, out var title);
            titleContent = new GUIContent(title);
        }

        /// <summary>
        /// セッションを開き直す
        /// </summary>
        /// <param name="clip">開くルートの SequenceClip</param>
        /// <param name="includeClipIndex">選択する includeClip index</param>
        /// <param name="force">同一対象でも再初期化する場合は true</param>
        private void Setup(SequenceClip clip, int includeClipIndex, bool force = false) {
            _escapedClip = clip;
            _escapedIncludeClipIndex = includeClipIndex;

            if (_session == null) {
                return;
            }

            _session.Open(clip, includeClipIndex, force);
        }
    }
}
