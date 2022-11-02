using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ActionSequencer.Editor.VisualElements;
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

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Setup(SequenceClip clip)
        {
            _escapedClip = clip;
            
            if (_editorModel.ClipModel?.Target == clip)
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
            inspectorView.Clear();
            
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
                    var labelView = new SequenceTrackLabelView(model.EventCount);
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
                _editorModel.ClipModel.OnRemoveTrackModel += OnRemoveTrackModel;
            
                // 既に登録済のModelを解釈
                for (var i = 0; i < _editorModel.ClipModel.TrackModels.Count; i++)
                {
                    var model = _editorModel.ClipModel.TrackModels[i];
                    OnAddedTrackModel(model);
                }
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
            
            // Track領域
            var trackArea = root.Q<VisualElement>("TrackArea");
            trackArea.RegisterCallback<WheelEvent>(evt =>
            {
                // WheelによってTimeToSize変更
                _editorModel.TimeToSize.Value = Mathf.Clamp(_editorModel.TimeToSize.Value + evt.delta.y, 100, 500);
            });
            
            // Timeline用Rulerの初期化
            var rulerScrollView = root.Q<ScrollView>("TrackRulerScrollView");
            var labelInterval = 1;
            _rulerView = root.Q<RulerView>("RulerView");
            trackList.contentContainer.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                _rulerView.style.width = evt.newRect.width;
            });
            trackScrollView.horizontalScroller.valueChanged += x =>
            {
                rulerScrollView.horizontalScroller.value = x;
            };
            _rulerView.OnGetThickLabel += thickIndex =>
            {
                if (thickIndex % labelInterval != 0)
                {
                    return "";
                }
                switch (_editorModel.CurrentTimeMode.Value)
                {
                    case SequenceEditorModel.TimeMode.Seconds:
                        return $"{thickIndex * 0.5f:0.0}";
                    case SequenceEditorModel.TimeMode.Frames30:
                        return (thickIndex * 15).ToString();
                    case SequenceEditorModel.TimeMode.Frames60:
                        return (thickIndex * 30).ToString();
                }

                return "";
            };
            _rulerView.MaskElement = rulerScrollView;
            _editorModel.TimeToSize
                .Subscribe(timeToSize =>
                {
                    _rulerView.MemorySize = timeToSize * 0.5f / _rulerView.ThickCycle;
                });
            _editorModel.CurrentTimeMode
                .Subscribe(timeMode =>
                {
                    switch (timeMode)
                    {
                        case SequenceEditorModel.TimeMode.Seconds:
                            labelInterval = 2;
                            _rulerView.ThickCycle = 10;
                            break;
                        case SequenceEditorModel.TimeMode.Frames30:
                            labelInterval = 2;
                            _rulerView.ThickCycle = 15;
                            break;
                        case SequenceEditorModel.TimeMode.Frames60:
                            labelInterval = 2;
                            _rulerView.ThickCycle = 15;
                            break;
                    }
                    _rulerView.RefreshLabels();
                    _rulerView.MemorySize = _editorModel.TimeToSize.Value * 0.5f / _rulerView.ThickCycle;
                });
            
            // CreateMenu
            var createMenu = root.Q<ToolbarMenu>("CreateMenu");
            var signalTypes = TypeCache.GetTypesDerivedFrom<SignalSequenceEvent>();
            var rangeTypes = TypeCache.GetTypesDerivedFrom<RangeSequenceEvent>();
            foreach (var signalType in signalTypes)
            {
                var t = signalType;
                createMenu.menu.AppendAction($"Signal Event/{t.Name}", _ =>
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
                createMenu.menu.AppendAction($"Range Event/{t.Name}", _ =>
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
                switch (evt.newValue)
                {
                    case nameof(SequenceEditorModel.TimeMode.Seconds):
                        _editorModel.CurrentTimeMode.Value = SequenceEditorModel.TimeMode.Seconds;
                        break;
                    case nameof(SequenceEditorModel.TimeMode.Frames30):
                        _editorModel.CurrentTimeMode.Value = SequenceEditorModel.TimeMode.Frames30;
                        break;
                    case nameof(SequenceEditorModel.TimeMode.Frames60):
                        _editorModel.CurrentTimeMode.Value = SequenceEditorModel.TimeMode.Frames60;
                        break;
                }
            });
            _editorModel.CurrentTimeMode
                .Subscribe(timeMode =>
                {
                    rulerMode.value = timeMode.ToString();
                });

            // TimeFitToggle
            var timeFitToggle = root.Q<ToolbarToggle>("TimeFitToggle");
            timeFitToggle.RegisterValueChangedCallback(evt =>
            {
                _editorModel.TimeFit.Value = evt.newValue;
            });
            _editorModel.TimeFit
                .Subscribe(timeFit =>
                {
                    timeFitToggle.value = timeFit;
                });
            timeFitToggle.value = true;

            // EditorModelの状態初期化
            _editorModel.TimeToSize.Value = 200.0f;
            _editorModel.CurrentTimeMode.Value = SequenceEditorModel.TimeMode.Seconds;
            
            // 初期化
            Setup(_escapedClip);
        }

        /// <summary>
        /// 廃棄時処理
        /// </summary>
        private void OnDisable()
        {
            _editorModel?.Dispose();
            _editorModel = null;
        }
    }
}