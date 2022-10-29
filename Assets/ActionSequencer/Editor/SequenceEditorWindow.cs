using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ActionSequencer.Editor.VisualElements;
using UnityEditor.UIElements;
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
            
            // Clipの設定
            _editorModel.SetSequenceClip(clip);

            // Windowのタイトル変更
            titleContent = new GUIContent(clip != null ? clip.name : ObjectNames.NicifyVariableName(nameof(SequenceEditorWindow)));
            
            var root = rootVisualElement;
            var trackLabelList = root.Q<ListView>("TrackLabelList");
            var trackList = root.Q<ListView>("TrackList");
            trackLabelList.Bind(_editorModel.ClipModel.SerializedObject);
            trackLabelList.makeItem = () => {
                var trackLabelView = new SequenceTrackLabelView(0);
                return trackLabelView;
            };
            trackLabelList.bindItem = (element, i) => {
                var trackLabelView = (SequenceTrackLabelView)element;
                var track = clip.tracks[i];
                trackLabelView.Label = track.label;
                trackLabelView.LineCount = track.sequenceEvents.Length;
            };
            trackList.Bind(_editorModel.ClipModel.SerializedObject);
            trackList.makeItem = () => {
                var trackView = new SequenceTrackView();
                return trackView;
            };
            trackList.bindItem = (element, i) => {
                var trackView = (SequenceTrackView)element;
                var track = clip.tracks[i];
                trackView.Clear();
                foreach (var evt in track.sequenceEvents) {
                    var eventView = default(SequenceEventView);
                    if (evt is SequenceSignalEvent) {
                        eventView = new SequenceSignalEventView();
                    }
                    else {
                        eventView = new SequenceRangeEventView();
                    }
                    trackView.Add(eventView);
                }
            };
            
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

            _editorModel.OnChangedTimeToSize += timeToSize =>
            {
                _rulerView.MemorySize = timeToSize * 0.5f / _rulerView.ThickCycle;
            };
            _editorModel.TimeToSize = 100.0f;

            if (_editorModel.ClipModel != null)
            {
                void OnAddedTrackModel(SequenceTrackModel model)
                {
                    var view = new SequenceTrackLabelView(model.EventCount);
                    trackLabelList.Add(view);
                    var presenter = new SequenceTrackPresenter(model, view, _editorModel);
                    _trackPresenters.Add(presenter);
                }

                void OnRemoveTrackModel(SequenceTrackModel model)
                {
                    var presenter = _trackPresenters.FirstOrDefault(x => x.Model == model);
                    if (presenter == null)
                    {
                        return;
                    }
                    trackLabelList.Remove(presenter.LabelView);
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
            
            var root = rootVisualElement;
            var uxml = Resources.Load<VisualTreeAsset>("sequence_editor_window");
            uxml.CloneTree(root);
            var styleSheet = Resources.Load<StyleSheet>("sequence_editor_window");
            root.styleSheets.Add(styleSheet);

            // Scroll位置の同期
            var trackList = root.Q<ListView>("TrackList");
            var eventList = root.Q<ListView>("EventList");
            trackList.Q<ScrollView>().verticalScroller.valueChanged += x =>
            {
                eventList.Q<ScrollView>().verticalScroller.value = x;
            };
            eventList.Q<ScrollView>().verticalScroller.valueChanged += x =>
            {
                trackList.Q<ScrollView>().verticalScroller.value = x;
            };
            
            // Timelineのクリップ範囲指定
            var timelineScrollView = root.Q<ScrollView>("TimelineScrollView");
            _rulerView = root.Q<RulerView>("TimelineRulerView");
            _rulerView.OnGetThickLabel += thickIndex =>
            {
                if (thickIndex % 2 == 0)
                {
                    return $"{thickIndex * 0.5f:0.0}";
                }

                return "";
            };
            _rulerView.ThickCycle = 5;
            _rulerView.MaskElement = timelineScrollView;
            
            // CreateMenu
            var createMenu = root.Q<ToolbarMenu>("CreateMenu");
            createMenu.menu.AppendAction("Signal Event/Test 2", _ =>
            {
                var target = _editorModel.ClipModel?.Target;
                if (target == null)
                {
                    return;
                }
                
                // Track生成
                var track = CreateInstance<SequenceTrack>();
                AssetDatabase.AddObjectToAsset(track, target);
                Undo.RegisterCreatedObjectUndo(track, "Created Track");
                var trackModel = _editorModel.ClipModel.AddTrack(track);
                
                // Event生成
                var evt = CreateInstance<SampleSequenceSignalEvent>();
                AssetDatabase.AddObjectToAsset(evt, target);
                Undo.RegisterCreatedObjectUndo(evt, "Created Event");
                trackModel.AddSignalEvent(evt);
            });
            createMenu.menu.AppendAction("Range Event/Test 1", _ =>
            {
                var target = _editorModel.ClipModel?.Target;
                if (target == null)
                {
                    return;
                }
                
                // Track生成
                var track = CreateInstance<SequenceTrack>();
                AssetDatabase.AddObjectToAsset(track, target);
                Undo.RegisterCreatedObjectUndo(track, "Created Track");
                var trackModel = _editorModel.ClipModel.AddTrack(track);
                
                // Event生成 x 2
                var evt = CreateInstance<SampleSequenceRangeEvent>();
                AssetDatabase.AddObjectToAsset(evt, target);
                Undo.RegisterCreatedObjectUndo(evt, "Created Event");
                trackModel.AddRangeEvent(evt);
                evt = CreateInstance<SampleSequenceRangeEvent>();
                AssetDatabase.AddObjectToAsset(evt, target);
                Undo.RegisterCreatedObjectUndo(evt, "Created Event");
                trackModel.AddRangeEvent(evt);
            });
            
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