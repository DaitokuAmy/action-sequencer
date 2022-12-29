using System;
using System.Collections.Generic;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ActionSequencer.Editor.VisualElements;
using UnityEditor.Callbacks;
using ObjectField = UnityEditor.UIElements.ObjectField;
using ToolbarMenu = UnityEditor.UIElements.ToolbarMenu;
using ToolbarToggle = UnityEditor.UIElements.ToolbarToggle;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Sequence編集用Window
    /// </summary>
    public class SequenceEditorWindow : EditorWindow {
        // リセット対策用SequenceClipキャッシュ
        [SerializeField]
        private SequenceClip _escapedClip;

        // Editor用のModel
        private SequenceEditorModel _editorModel;

        // TrackのPresenterリスト
        private SequenceClipPresenter _sequenceClipPresenter;

        // ルーラー表示用View
        private RulerView _rulerView;

        // シークバー表示用View
        private VisualElement _seekbarView;

        // スクロールオフセット値
        private float _trackScrollOffsetX;

        // OnDisableで廃棄されるDisposablesのリスト
        private List<IDisposable> _disposables = new List<IDisposable>();

        // 再生主
        private GameObject _controllerProviderOwner;
        private ISequenceControllerProvider _controllerProvider;

        /// <summary>
        /// Windowを開く処理
        /// </summary>
        public static void Open(SequenceClip clip) {
            var window =
                CreateWindow<SequenceEditorWindow>(ObjectNames.NicifyVariableName(nameof(SequenceEditorWindow)));
            window.Setup(clip);
        }

        [MenuItem("Window/Sequence Tools/Sequence Editor Window")]
        private static void Open() {
            var window = GetWindow<SequenceEditorWindow>(ObjectNames.NicifyVariableName(nameof(SequenceEditorWindow)));
            window.Setup(null);
        }

        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceID, int line) {
            var asset = EditorUtility.InstanceIDToObject(instanceID);
            if (asset is SequenceClip clip) {
                Open(clip);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Setup(SequenceClip clip, bool force = false) {
            _escapedClip = clip;

            if (!force && _editorModel.ClipModel?.Target == clip) {
                return;
            }

            // Clipの設定
            _editorModel.SetSequenceClip(clip);

            // Windowのタイトル変更
            titleContent =
                new GUIContent(clip != null ? clip.name : ObjectNames.NicifyVariableName(nameof(SequenceEditorWindow)));

            // Viewの整理
            var root = rootVisualElement;
            var trackLabelList = root.Q<VisualElement>("TrackLabelList");
            var trackList = root.Q<VisualElement>("TrackList");
            trackLabelList.Clear();
            trackList.Clear();

            // ObjectField初期化
            var objectField = root.Q<ObjectField>("TargetObjectField");
            objectField.value = clip;

            // Presenterの削除
            if (_sequenceClipPresenter != null) {
                _sequenceClipPresenter.Dispose();
                _sequenceClipPresenter = null;
            }

            // Presenterの生成
            if (_editorModel.ClipModel != null) {
                _sequenceClipPresenter =
                    new SequenceClipPresenter(_editorModel.ClipModel, trackLabelList, trackList, _editorModel);
            }
        }

        /// <summary>
        /// ViewDataKeyの設定
        /// </summary>
        private void SetViewDataKey(VisualElement element) {
            element.viewDataKey = $"{nameof(SequenceEditorWindow)}_{nameof(element.name)}";
        }

        /// <summary>
        /// ControllerProviderを更新
        /// </summary>
        private void UpdateControllerProvider() {
            if (!Application.isPlaying) {
                _controllerProviderOwner = null;
                _controllerProvider = null;
                return;
            }

            var activeObject = Selection.activeGameObject;
            if (activeObject != null && activeObject != _controllerProviderOwner) {
                var provider = activeObject.GetComponentInParent<ISequenceControllerProvider>();
                if (provider == null) {
                    return;
                }

                _controllerProviderOwner = activeObject;
                _controllerProvider = provider;
            }
        }

        /// <summary>
        /// アクティブ時処理
        /// </summary>
        private void OnEnable() {
            // EditorModel生成
            _editorModel = new SequenceEditorModel(rootVisualElement);

            // Xml, Style読み込み
            var root = rootVisualElement;
            var uxml = Resources.Load<VisualTreeAsset>("sequence_editor_window");
            uxml.CloneTree(root);
            var styleSheet = Resources.Load<StyleSheet>("sequence_editor_window");
            root.styleSheets.Add(styleSheet);

            // キー入力受け取れるように設定
            root.focusable = true;
            root.pickingMode = PickingMode.Position;

            // Scroll位置の同期
            var trackLabelList = root.Q<ScrollView>("TrackLabelList");
            var trackScrollView = root.Q<ScrollView>("TrackScrollView");
            var trackList = root.Q<VisualElement>("TrackList");
            trackLabelList.verticalScroller.valueChanged += x => { trackScrollView.verticalScroller.value = x; };
            trackScrollView.verticalScroller.valueChanged += x => { trackLabelList.verticalScroller.value = x; };

            // Timeline用Rulerの初期化
            var rulerArea = root.Q<VisualElement>("TrackRulerArea");
            _rulerView = root.Q<RulerView>("RulerView");
            trackList.contentContainer.RegisterCallback<GeometryChangedEvent>(evt => {
                _rulerView.style.width = evt.newRect.width;
            });
            trackScrollView.horizontalScroller.valueChanged += x => {
                var pos = _rulerView.transform.position;
                pos.x = -x;
                _rulerView.transform.position = pos;
                _trackScrollOffsetX = x;
            };
            rulerArea.RegisterCallback<WheelEvent>(evt => {
                // WheelによってTimeToSize変更
                _editorModel.TimeToSize.Value -= evt.delta.y;
            });
            _rulerView.OnGetThickLabel += thickIndex => {
                if (thickIndex % 2 != 0) {
                    return "";
                }

                var thickSeconds = SequenceEditorUtility.GetThickSeconds(_editorModel.CurrentTimeMode.Value);
                var seconds = thickIndex * thickSeconds;
                switch (_editorModel.CurrentTimeMode.Value) {
                    case SequenceEditorModel.TimeMode.Seconds:
                        return $"{seconds:0.0}";
                    case SequenceEditorModel.TimeMode.Frames30:
                        return $"{seconds * 30:0}";
                    case SequenceEditorModel.TimeMode.Frames60:
                        return $"{seconds * 60:0}";
                }

                return "";
            };
            _rulerView.MaskElement = rulerArea;
            _disposables.Add(_editorModel.TimeToSize
                .Subscribe(_ => { _rulerView.MemorySize = SequenceEditorUtility.CalcMemorySize(_editorModel); }));
            _disposables.Add(_editorModel.CurrentTimeMode
                .Subscribe(timeMode => {
                    _rulerView.ThickCycle = SequenceEditorUtility.GetThickCycle(timeMode);
                    _rulerView.MemorySize = SequenceEditorUtility.CalcMemorySize(_editorModel);
                    _rulerView.RefreshLabels();
                }));

            // CreateMenu
            var createMenu = root.Q<ToolbarMenu>("CreateMenu");
            createMenu.SetEnabled(_editorModel.ClipModel != null);
            _disposables.Add(_editorModel.ChangeClipModelSubject
                .Subscribe(model => { createMenu.SetEnabled(model != null); }));

            // Trackの生成
            createMenu.menu.AppendAction("Track", _ => {
                if (_editorModel.ClipModel != null) {
                    _editorModel.ClipModel.AddTrack();
                }
            });

            // Play/Pause
            var playPauseToggle = root.Q<ToolbarToggle>("PlayPauseToggle");
            playPauseToggle.RegisterValueChangedCallback(evt => {
                // Play中
                if (evt.newValue) {
                    playPauseToggle.RemoveFromClassList("play_icon");
                    playPauseToggle.AddToClassList("pause_icon");
                }
                // Pause中
                else {
                    playPauseToggle.RemoveFromClassList("pause_icon");
                    playPauseToggle.AddToClassList("play_icon");
                }
            });
            // todo:取り合えず無効
            playPauseToggle.style.display = DisplayStyle.None;

            // ObjectField
            var objectField = root.Q<ObjectField>("TargetObjectField");
            objectField.RegisterValueChangedCallback(evt => { Setup(evt.newValue as SequenceClip); });

            // RulerMode
            var rulerMode = root.Q<DropdownField>("RulerMode");
            rulerMode.choices = new List<string>(Enum.GetNames(typeof(SequenceEditorModel.TimeMode)));
            rulerMode.RegisterValueChangedCallback(evt => {
                _editorModel.CurrentTimeMode.Value = (SequenceEditorModel.TimeMode)rulerMode.index;
            });
            _disposables.Add(_editorModel.CurrentTimeMode
                .Subscribe(timeMode => { rulerMode.value = timeMode.ToString(); }));

            // TimeFitToggle
            var timeFitToggle = root.Q<ToolbarToggle>("TimeFitToggle");
            timeFitToggle.RegisterValueChangedCallback(evt => { _editorModel.TimeFit.Value = evt.newValue; });
            _disposables.Add(_editorModel.TimeFit
                .Subscribe(timeFit => { timeFitToggle.value = timeFit; }));

            // InspectorView
            var inspectorView = root.Q<InspectorView>();
            _disposables.Add(_editorModel.CurrentTimeMode
                .Subscribe(timeMode => inspectorView.TimeMode = timeMode));
            _disposables.Add(_editorModel.ChangedSelectedTargetsSubject
                .Subscribe(inspectorView.SetTarget));

            // Seekbar
            _seekbarView = root.Q<VisualElement>("TrackSeekbar");

            // Sizeのフィット
            var trackArea = root.Q<VisualElement>("TrackArea");
            root.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == KeyCode.F) {
                    if (_editorModel.SetBestTimeToSize(trackArea.layout.width - 20.0f)) {
                        trackScrollView.horizontalScroller.value = 0.0f;
                    }
                }
            });

            // ViewDataKey
            var splitViews = root.Query<SplitView>().ToList();
            foreach (var element in splitViews) {
                SetViewDataKey(element);
            }

            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            // 初期化
            Setup(_escapedClip, true);
        }

        /// <summary>
        /// 廃棄時処理
        /// </summary>
        private void OnDisable() {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;

            foreach (var disposable in _disposables) {
                disposable.Dispose();
            }

            _disposables.Clear();
            _editorModel?.Dispose();
            _editorModel = null;
        }

        /// <summary>
        /// GUI描画処理
        /// </summary>
        private void OnGUI() {
            if (_editorModel?.ClipModel == null) {
                return;
            }

            UpdateControllerProvider();

            // Seekbarの調整
            var clip = _editorModel.ClipModel.Target as SequenceClip;
            if (_seekbarView != null) {
                var sequenceController = _controllerProvider?.SequenceController;
                var time = sequenceController != null ? sequenceController.GetSequenceTime(clip) : -1.0f;
                _seekbarView.visible = time >= 0.0f;
                if (_seekbarView.visible) {
                    // 時間をSeekbarに反映
                    var left = time * _editorModel.TimeToSize.Value - _trackScrollOffsetX;
                    _seekbarView.style.marginLeft = left;

                    // ちょっと強引だけどleftが負の数なら非表示
                    if (left < 0.0f) {
                        _seekbarView.visible = false;
                    }
                }
            }
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        private void Update() {
            if (_controllerProvider != null) {
                Repaint();
            }
        }

        /// <summary>
        /// Undo/Redo通知
        /// </summary>
        private void OnUndoRedoPerformed() {
            // 本来はプロパティの変更を監視したいが、監視ができないので強引だが開き直す
            Setup(_escapedClip, true);
        }
    }
}