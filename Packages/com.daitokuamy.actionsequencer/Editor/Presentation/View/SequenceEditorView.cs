using System.Collections.Generic;
using ActionSequencer.Editor.VisualElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using ObjectField = UnityEditor.UIElements.ObjectField;
using ToolbarMenu = UnityEditor.UIElements.ToolbarMenu;
using ToolbarToggle = UnityEditor.UIElements.ToolbarToggle;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceEditor 全体の View
    /// </summary>
    internal sealed class SequenceEditorView {
        private readonly Label _helpFitAllLabel;
        private readonly Label _helpFitSelectionLabel;
        private readonly Label _helpMoveEventLabel;
        private readonly Label _helpDuplicateLabel;
        private readonly Label _helpCopyPasteLabel;
        private readonly Label _helpDeleteLabel;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="root">Window の UI ルート</param>
        public SequenceEditorView(VisualElement root) {
            Root = root;
            Root.focusable = true;
            Root.pickingMode = PickingMode.Position;

            TrackLabelListView = Root.Q<ScrollView>("TrackLabelList");
            TrackScrollView = Root.Q<ScrollView>("TrackScrollView");
            TrackListView = Root.Q<SequenceTrackListView>("TrackList");
            TrackAreaView = Root.Q<VisualElement>("TrackArea");
            TrackRulerAreaView = Root.Q<VisualElement>("TrackRulerArea");
            SeekbarView = Root.Q<VisualElement>("TrackSeekbar");
            TargetObjectField = Root.Q<ObjectField>("TargetObjectField");
            IncludeClipField = Root.Q<DropdownField>("IncludeClipField");
            RulerModeField = Root.Q<DropdownField>("RulerMode");
            TimeFitToggle = Root.Q<ToolbarToggle>("TimeFitToggle");
            CreateMenu = Root.Q<ToolbarMenu>("CreateMenu");
            PlayPauseToggle = Root.Q<ToolbarToggle>("PlayPauseToggle");
            RefreshButton = Root.Q<Button>("RefreshButton");
            InspectorView = Root.Q<InspectorView>("Inspector");
            PreviewView = Root.Q<AnimationClipView>("Preview");

            _helpFitAllLabel = Root.Q<Label>("HelpFitAll");
            _helpFitSelectionLabel = Root.Q<Label>("HelpFitSelection");
            _helpMoveEventLabel = Root.Q<Label>("HelpMoveEvent");
            _helpDuplicateLabel = Root.Q<Label>("HelpDuplicate");
            _helpCopyPasteLabel = Root.Q<Label>("HelpCopyPaste");
            _helpDeleteLabel = Root.Q<Label>("HelpDelete");

            UpdateShortcutHelp();
        }

        /// <summary>UI ルート</summary>
        public VisualElement Root { get; }
        /// <summary>Track ラベル一覧の ScrollView</summary>
        public ScrollView TrackLabelListView { get; }
        /// <summary>Track 一覧の ScrollView</summary>
        public ScrollView TrackScrollView { get; }
        /// <summary>Track 一覧の View</summary>
        public SequenceTrackListView TrackListView { get; }
        /// <summary>Track 領域の View</summary>
        public VisualElement TrackAreaView { get; }
        /// <summary>Track ルーラー領域の View</summary>
        public VisualElement TrackRulerAreaView { get; }
        /// <summary>シークバー View</summary>
        public VisualElement SeekbarView { get; }
        /// <summary>ターゲット SequenceClip 選択欄</summary>
        public ObjectField TargetObjectField { get; }
        /// <summary>includeClip 選択欄</summary>
        public DropdownField IncludeClipField { get; }
        /// <summary>時間表示モード選択欄</summary>
        public DropdownField RulerModeField { get; }
        /// <summary>time fit 切り替えトグル</summary>
        public ToolbarToggle TimeFitToggle { get; }
        /// <summary>作成メニュー</summary>
        public ToolbarMenu CreateMenu { get; }
        /// <summary>再生トグル</summary>
        public ToolbarToggle PlayPauseToggle { get; }
        /// <summary>更新ボタン</summary>
        public Button RefreshButton { get; }
        /// <summary>Inspector 表示 View</summary>
        public InspectorView InspectorView { get; }
        /// <summary>Preview 表示 View</summary>
        public AnimationClipView PreviewView { get; }

        /// <summary>
        /// SplitView の永続キーを初期化
        /// </summary>
        public void InitializeSplitViewKeys() {
            foreach (var splitView in Root.Query<SplitView>().ToList()) {
                splitView.viewDataKey = $"{nameof(SequenceEditorWindow)}_{nameof(splitView.name)}";
            }
        }

        /// <summary>
        /// ツールバーの有効状態を更新
        /// </summary>
        /// <param name="hasClip">編集中クリップを持つ場合は true</param>
        /// <param name="timeMode">現在の時間表示モード</param>
        /// <param name="timeFit">現在の time fit 状態</param>
        public void UpdateToolbarState(bool hasClip, SequenceEditorModel.TimeMode timeMode, bool timeFit) {
            CreateMenu.SetEnabled(hasClip);
            RulerModeField.SetEnabled(hasClip);
            TimeFitToggle.SetEnabled(hasClip);
            RulerModeField.value = timeMode.ToString();
            TimeFitToggle.value = timeFit;
        }

        /// <summary>
        /// includeClip 選択欄の内容を更新
        /// </summary>
        /// <param name="rootClip">ルートの SequenceClip</param>
        /// <param name="includeClipIndex">選択中の includeClip index</param>
        public void UpdateIncludeClipField(SequenceClip rootClip, int includeClipIndex) {
            IncludeClipField.style.display = DisplayStyle.None;
            IncludeClipField.choices.Clear();
            IncludeClipField.index = -1;
        }

        /// <summary>
        /// Preview 表示対象を更新
        /// </summary>
        /// <param name="previewClip">表示する AnimationClip</param>
        /// <param name="offsetTime">表示するオフセット時間</param>
        public void UpdatePreview(AnimationClip previewClip, float offsetTime) {
            PreviewView.ChangeTarget(previewClip);
            PreviewView.ChangeOffsetTime(offsetTime);
        }

        /// <summary>
        /// Inspector 表示を更新
        /// </summary>
        /// <param name="timeMode">表示に使う時間モード</param>
        /// <param name="selectedTargets">現在の選択対象</param>
        public void UpdateInspector(SequenceEditorModel.TimeMode timeMode, IReadOnlyList<Object> selectedTargets) {
            InspectorView.TimeMode = timeMode;
            InspectorView.SetTarget(selectedTargets);
        }

        /// <summary>
        /// シークバー位置を更新
        /// </summary>
        /// <param name="left">左端位置</param>
        /// <param name="visible">表示する場合は true</param>
        public void UpdateSeekbar(float left, bool visible) {
            SeekbarView.visible = visible;
            if (!visible) {
                return;
            }

            SeekbarView.style.marginLeft = left;
        }

        /// <summary>
        /// 実行環境に応じて HelpBar のショートカット表記を更新
        /// </summary>
        public void UpdateShortcutHelp() {
            var modifierKeyText = Application.platform == RuntimePlatform.OSXEditor ? "Cmd" : "Ctrl";
            _helpFitAllLabel.text = "A : Fit All";
            _helpFitSelectionLabel.text = "F : Fit Selection";
            _helpMoveEventLabel.text = "Shift + Up / Down : Move Event";
            _helpDuplicateLabel.text = $"{modifierKeyText} + D : Duplicate";
            _helpCopyPasteLabel.text = $"{modifierKeyText} + C / {modifierKeyText} + V : Copy / Paste";
            _helpDeleteLabel.text = "Delete : Delete Event";
        }
    }
}
