using System;
using System.Collections.Generic;
using System.Linq;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ActionSequencer.Editor.VisualElements;
using UnityEditor.Callbacks;
using ObjectField = UnityEditor.UIElements.ObjectField;
using ToolbarMenu = UnityEditor.UIElements.ToolbarMenu;
using ToolbarToggle = UnityEditor.UIElements.ToolbarToggle;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// Sequence編集用Window
    /// </summary>
    public class SequenceEditorWindow : EditorWindow
    {
        // リセット対策用SequenceClipキャッシュ
        [SerializeField]
        private SequenceClip _escapedClip;
        
        // Editor用のModel
        private SequenceEditorModel _editorModel;
        // TrackのPresenterリスト
        private readonly List<SequenceTrackPresenter> _trackPresenters = new List<SequenceTrackPresenter>();
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
        public static void Open(SequenceClip clip)
        {
            var window = CreateWindow<SequenceEditorWindow>(ObjectNames.NicifyVariableName(nameof(SequenceEditorWindow)));
            window.Setup(clip);
        }

        [MenuItem("Window/Sequence Tools/Sequence Editor Window")]
        private static void Open()
        {
            var window = GetWindow<SequenceEditorWindow>(ObjectNames.NicifyVariableName(nameof(SequenceEditorWindow)));
            window.Setup(null);
        }
        
        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID);
            if (asset is SequenceClip clip)
            {
                Open(clip);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Setup(SequenceClip clip, bool force = false)
        {
            _escapedClip = clip;
            
            if (!force && _editorModel.ClipModel?.Target == clip)
            {
                return;
            }

            // Clipの設定
            _editorModel.SetSequenceClip(clip);

            // Windowのタイトル変更
            titleContent = new GUIContent(clip != null ? clip.name : ObjectNames.NicifyVariableName(nameof(SequenceEditorWindow)));
            
            var root = rootVisualElement;
            var trackLabelList = root.Q<VisualElement>("TrackLabelList");
            var trackList = root.Q<VisualElement>("TrackList");
            trackLabelList.Clear();
            trackList.Clear();
            
            // Inspectorパネル初期化 ※InspectorElementはLayoutに不具合があったので未使用
            var inspectorView = root.Q<InspectorView>();
            inspectorView.ClearTarget();
            
            // ObjectField初期化
            var objectField = root.Q<ObjectField>("TargetObjectField");
            objectField.value = clip;

            _editorModel.OnChangedSelectedTargets += targets =>
            {
                inspectorView.SetTarget(targets);
            };

            if (_editorModel.ClipModel != null)
            {
                void OnAddedTrackModel(SequenceTrackModel model)
                {
                    var labelView = new SequenceTrackLabelView();
                    trackLabelList.Add(labelView);
                    var trackView = new SequenceTrackView();
                    trackList.Add(trackView);
                    var presenter = new SequenceTrackPresenter(model, labelView, trackView, _editorModel);
                    _trackPresenters.Add(presenter);
                }

                void OnRemoveTrackModel(SequenceTrackModel model)
                {
                    var presenter = _trackPresenters.FirstOrDefault(x => x.Model == model);
                    if (presenter == null)
                    {
                        return;
                    }
                    trackLabelList.Remove(presenter.View);
                    trackList.Remove(presenter.TrackView);
                    presenter.Dispose();
                    _trackPresenters.Remove(presenter);
                }

                _editorModel.ClipModel.OnAddedTrackModel += OnAddedTrackModel;
                _editorModel.ClipModel.OnRemovedTrackModel += OnRemoveTrackModel;
            
                // 既に登録済のModelを解釈
                for (var i = 0; i < _editorModel.ClipModel.TrackModels.Count; i++)
                {
                    var model = _editorModel.ClipModel.TrackModels[i];
                    OnAddedTrackModel(model);
                }
            }
        }

        /// <summary>
        /// ViewDataKeyの設定
        /// </summary>
        private void SetViewDataKey(VisualElement element)
        {
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
        private void OnEnable()
        {
            // EditorModel生成
            _editorModel = new SequenceEditorModel(rootVisualElement);
            
            // Xml, Style読み込み
            var root = rootVisualElement;
            var uxml = Resources.Load<VisualTreeAsset>("sequence_editor_window");
            uxml.CloneTree(root);
            var styleSheet = Resources.Load<StyleSheet>("sequence_editor_window");
            root.styleSheets.Add(styleSheet);

            // Scroll位置の同期
            var trackLabelList = root.Q<ScrollView>("TrackLabelList");
            var trackScrollView = root.Q<ScrollView>("TrackScrollView");
            var trackList = root.Q<VisualElement>("TrackList");
            trackLabelList.verticalScroller.valueChanged += x =>
            {
                trackScrollView.verticalScroller.value = x;
            };
            trackScrollView.verticalScroller.valueChanged += x =>
            {
                trackLabelList.verticalScroller.value = x;
            };
            
            // Timeline用Rulerの初期化
            var rulerArea = root.Q<VisualElement>("TrackRulerArea");
            _rulerView = root.Q<RulerView>("RulerView");
            trackList.contentContainer.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                _rulerView.style.width = evt.newRect.width;
            });
            trackScrollView.horizontalScroller.valueChanged += x =>
            {
                var pos = _rulerView.transform.position;
                pos.x = -x;
                _rulerView.transform.position = pos;
                _trackScrollOffsetX = x;
            };
            rulerArea.RegisterCallback<WheelEvent>(evt =>
            {
                // WheelによってTimeToSize変更
                _editorModel.TimeToSize.Value = Mathf.Clamp(_editorModel.TimeToSize.Value + evt.delta.y, 100, 500);
            });
            _rulerView.OnGetThickLabel += thickIndex =>
            {
                if (thickIndex % 2 != 0) {
                    return "";
                }

                var thickSeconds = SequenceEditorUtility.GetThickSeconds(_editorModel.CurrentTimeMode.Value);
                var seconds = thickIndex * thickSeconds;
                switch (_editorModel.CurrentTimeMode.Value)
                {
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
                .Subscribe(_ =>
                {
                    _rulerView.MemorySize = SequenceEditorUtility.CalcMemorySize(_editorModel);
                }));
            _disposables.Add(_editorModel.CurrentTimeMode
                .Subscribe(timeMode =>
                {
                    _rulerView.ThickCycle = SequenceEditorUtility.GetThickCycle(timeMode);
                    _rulerView.MemorySize = SequenceEditorUtility.CalcMemorySize(_editorModel);
                    _rulerView.RefreshLabels();
                }));
            
            // CreateMenu
            var createMenu = root.Q<ToolbarMenu>("CreateMenu");
            var signalTypes = TypeCache.GetTypesDerivedFrom<SignalSequenceEvent>();
            var rangeTypes = TypeCache.GetTypesDerivedFrom<RangeSequenceEvent>();
            foreach (var signalType in signalTypes)
            {
                var t = signalType;
                var displayName = SequenceEditorUtility.GetDisplayName(t);
                createMenu.menu.AppendAction($"Signal Event/{displayName}", _ =>
                {
                    var target = _editorModel.ClipModel?.Target;
                    if (target == null)
                    {
                        return;
                    }
                
                    // Track取得/生成
                    var trackModel = _editorModel.ClipModel.GetOrCreateTrack(signalType);
                    // Event生成
                    trackModel.AddEvent(signalType);
                });
            }
            foreach (var rangeType in rangeTypes)
            {
                var t = rangeType;
                var displayName = SequenceEditorUtility.GetDisplayName(t);
                createMenu.menu.AppendAction($"Range Event/{displayName}", _ =>
                {
                    var target = _editorModel.ClipModel?.Target;
                    if (target == null)
                    {
                        return;
                    }
                    
                    // Track取得/生成
                    var trackModel = _editorModel.ClipModel.GetOrCreateTrack(rangeType);
                    // Event生成
                    trackModel.AddEvent(rangeType);
                });
            }
            
            // Play/Pause
            var playPauseToggle = root.Q<ToolbarToggle>("PlayPauseToggle");
            playPauseToggle.RegisterValueChangedCallback(evt =>
            {
                // Play中
                if (evt.newValue)
                {
                    playPauseToggle.RemoveFromClassList("play_icon");
                    playPauseToggle.AddToClassList("pause_icon");
                }
                // Pause中
                else
                {
                    playPauseToggle.RemoveFromClassList("pause_icon");
                    playPauseToggle.AddToClassList("play_icon");
                }
            });
            
            // ObjectField
            var objectField = root.Q<ObjectField>("TargetObjectField");
            objectField.RegisterValueChangedCallback(evt =>
            {
                Setup(evt.newValue as SequenceClip);
            });
            
            // RulerMode
            var rulerMode = root.Q<DropdownField>("RulerMode");
            rulerMode.choices = new List<string>(Enum.GetNames(typeof(SequenceEditorModel.TimeMode)));
            rulerMode.RegisterValueChangedCallback(evt =>
            {
                _editorModel.CurrentTimeMode.Value = (SequenceEditorModel.TimeMode)rulerMode.index;
            });
            _disposables.Add(_editorModel.CurrentTimeMode
                .Subscribe(timeMode =>
                {
                    rulerMode.value = timeMode.ToString();
                }));

            // TimeFitToggle
            var timeFitToggle = root.Q<ToolbarToggle>("TimeFitToggle");
            timeFitToggle.RegisterValueChangedCallback(evt =>
            {
                _editorModel.TimeFit.Value = evt.newValue;
            });
            _disposables.Add(_editorModel.TimeFit
                .Subscribe(timeFit =>
                {
                    timeFitToggle.value = timeFit;
                }));

            // InspectorView
            var inspectorView = root.Q<InspectorView>();
            _disposables.Add(_editorModel.CurrentTimeMode
                .Subscribe(timeMode =>
                {
                    inspectorView.TimeMode = timeMode;
                }));
            
            // Seekbar
            _seekbarView = root.Q<VisualElement>("TrackSeekbar");
            
            // ViewDataKey
            // SetViewDataKey(trackLabelList);
            // SetViewDataKey(rulerScrollView);
            // SetViewDataKey(trackScrollView);
            var splitViews = root.Query<SplitView>().ToList();
            foreach (var element in splitViews)
            {
                SetViewDataKey(element);
            }

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            
            // 初期化
            Setup(_escapedClip, true);
        }

        /// <summary>
        /// 廃棄時処理
        /// </summary>
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;

            foreach (var disposable in _disposables)
            {
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
        private void OnUndoRedoPerformed()
        {
            // 本来はプロパティの変更を監視したいが、監視ができないので強引だが開き直す
            Setup(_escapedClip, true);
        }
    }
}