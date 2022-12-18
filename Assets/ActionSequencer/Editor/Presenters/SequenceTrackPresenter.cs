using System;
using System.Collections.Generic;
using System.Linq;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceTrack用のPresenter
    /// </summary>
    public class SequenceTrackPresenter : Presenter<SequenceTrackModel, SequenceTrackLabelView> {
        private SequenceEditorModel _editorModel;
        private List<SequenceEventPresenter> _eventPresenters = new List<SequenceEventPresenter>();

        public SequenceTrackView TrackView { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceTrackPresenter(SequenceTrackModel model, SequenceTrackLabelView view,
            SequenceTrackView trackView, SequenceEditorModel editorModel)
            : base(model, view) {
            TrackView = trackView;
            _editorModel = editorModel;

            AddDisposable(Model.AddedEventModelSubject
                .Subscribe(AddedEventModelSubject));
            AddDisposable(Model.RemovedEventModelSubject
                .Subscribe(RemovedEventModelSubject));
            AddDisposable(Model.MovedEventModelSubject
                .Subscribe(MovedEventModelSubject));
            AddDisposable(Model.ChangedLabelSubject
                .Subscribe(ChangedLabelSubject));
            AddDisposable(Model.ChangedEventTimeSubject
                .Subscribe(OnChangedEventTime));

            AddDisposable(View.ChangedLabelSubject
                .Subscribe(ChangedLabelSubjectView));
            AddDisposable(View.ClickedOptionSubject
                .Subscribe(ClickedOptionSubject));
            AddDisposable(View.ChangedFoldoutSubject
                .Subscribe(ChangedFoldoutSubject));

            // TimeToSize監視
            AddDisposable(_editorModel.TimeToSize
                .Subscribe(x => { OnChangedEventTime(); }));

            // Rulerの情報反映
            TrackView.RulerView.MaskElement = TrackView.parent.parent;
            AddDisposable(_editorModel.TimeToSize
                .Subscribe(_ => {
                    TrackView.RulerView.MemorySize = SequenceEditorUtility.CalcMemorySize(_editorModel);
                }));
            AddDisposable(_editorModel.CurrentTimeMode
                .Subscribe(timeMode => {
                    TrackView.RulerView.ThickCycle = SequenceEditorUtility.GetThickCycle(timeMode);
                    TrackView.RulerView.MemorySize = SequenceEditorUtility.CalcMemorySize(_editorModel);
                }));

            // 既に登録済のModelを解釈
            for (var i = 0; i < Model.EventModels.Count; i++) {
                var eventModel = Model.EventModels[i];
                AddedEventModelSubject(eventModel);
            }

            ChangedLabelSubject(Model.Label);
            ChangedFoldoutSubject(View.Foldout);
            OnChangedEventTime();
        }

        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public override void Dispose() {
            base.Dispose();

            foreach (var presenter in _eventPresenters) {
                presenter.Dispose();
            }
        }

        /// <summary>
        /// EventのViewをソートする
        /// </summary>
        private void SortEventViews() {
            var sequenceTrack = Model.Target as SequenceTrack;
            if (sequenceTrack == null) {
                return;
            }

            var eventList = new List<SequenceEvent>(sequenceTrack.sequenceEvents);
            int Compare(VisualElement a, VisualElement b) {
                var sequenceEventA = a.userData as SequenceEvent;
                var sequenceEventB = b.userData as SequenceEvent;
                var indexA = eventList.IndexOf(sequenceEventA);
                var indexB = eventList.IndexOf(sequenceEventB);
                return indexA - indexB;
            }

            View.Sort(Compare);
            TrackView.Sort(Compare);
        }

        /// <summary>
        /// EventModel追加時
        /// </summary>
        private void AddedEventModelSubject(SequenceEventModel model) {
            // TrackLabelの要素を追加
            var element = View.AddElement();
            element.userData = model.Target;
            
            // Event用のPresenter構築
            if (model is SignalSequenceEventModel signalEventModel) {
                var view = new SignalSequenceEventView();
                view.userData = model.Target;
                TrackView.AddEventView(view);
                var presenter = new SignalSequenceEventPresenter(signalEventModel, view, element, Model, _editorModel);
                _eventPresenters.Add(presenter);
            }
            else if (model is RangeSequenceEventModel rangeEventModel) {
                var view = new RangeSequenceEventView();
                view.userData = model.Target;
                TrackView.AddEventView(view);
                var presenter = new RangeSequenceEventPresenter(rangeEventModel, view, element, Model, _editorModel);
                _eventPresenters.Add(presenter);
            }

            // Viewのソート
            SortEventViews();

            // Track幅計算しなおし
            OnChangedEventTime();
        }

        /// <summary>
        /// EventModel削除時
        /// </summary>
        private void RemovedEventModelSubject(SequenceEventModel model) {
            var presenter = _eventPresenters.FirstOrDefault(x => x.Model == model);
            if (presenter == null) {
                return;
            }

            // EventView削除
            TrackView.RemoveEventView(presenter.View);

            // Label要素を削除
            View.RemoveElement(presenter.LabelElementView);

            // Presenter削除
            presenter.Dispose();
            _eventPresenters.Remove(presenter);

            // Track幅計算しなおし
            OnChangedEventTime();
        }

        /// <summary>
        /// EventModel移動時
        /// </summary>
        private void MovedEventModelSubject() {
            // Viewのソート
            SortEventViews();
        }

        /// <summary>
        /// Label変更時
        /// </summary>
        private void ChangedLabelSubject(string label) {
            View.Label = label;
        }

        /// <summary>
        /// Track内部のEventの時間変化時通知
        /// </summary>
        private void OnChangedEventTime() {
            var minTime = float.MaxValue;
            var maxTime = 0.0f;
            foreach (var presenter in _eventPresenters) {
                if (presenter.Model is SignalSequenceEventModel signalEventModel) {
                    minTime = Mathf.Min(minTime, signalEventModel.Time);
                    maxTime = Mathf.Max(maxTime, signalEventModel.Time);
                }
                else if (presenter.Model is RangeSequenceEventModel rangeEventModel) {
                    minTime = Mathf.Min(minTime, rangeEventModel.EnterTime);
                    maxTime = Mathf.Max(maxTime, rangeEventModel.ExitTime);
                }
            }

            minTime = Mathf.Min(minTime, maxTime);

            var min = minTime * _editorModel.TimeToSize.Value;
            var max = maxTime * _editorModel.TimeToSize.Value;
            TrackView.SetTrackArea(min, max);
        }

        /// <summary>
        /// View経由でのLabel変更通知
        /// </summary>
        private void ChangedLabelSubjectView(string label) {
            Model.Label = label;
        }

        /// <summary>
        /// フォルダリング状態の変化通知
        /// </summary>
        private void ChangedFoldoutSubject(bool foldout) {
            TrackView.SetFoldout(foldout);
        }

        /// <summary>
        /// View経由でのDefaultLabelボタン押下通知
        /// </summary>
        private void ClickedOptionSubject() {
            var menu = new GenericMenu();

            // Order
            menu.AddItem(new GUIContent("Up"), false, () => {
                _editorModel.ClipModel.MovePrevTrack(Model);
            });
            menu.AddItem(new GUIContent("Down"), false, () => {
                _editorModel.ClipModel.MoveNextTrack(Model);
            });
            
            menu.AddSeparator("");

            // Create
            var signalTypes = TypeCache.GetTypesDerivedFrom<SignalSequenceEvent>();
            var rangeTypes = TypeCache.GetTypesDerivedFrom<RangeSequenceEvent>();
            foreach (var signalType in signalTypes) {
                var t = signalType;
                var displayName = SequenceEditorUtility.GetDisplayName(t);
                menu.AddItem(new GUIContent($"Create Event/Signal/{displayName}"), false, () => {
                    var target = _editorModel.ClipModel?.Target;
                    if (target == null) {
                        return;
                    }

                    // Event生成
                    Model.AddEvent(signalType);
                });
            }

            foreach (var rangeType in rangeTypes) {
                var t = rangeType;
                var displayName = SequenceEditorUtility.GetDisplayName(t);
                menu.AddItem(new GUIContent($"Create Event/Range/{displayName}"), false, () => {
                    var target = _editorModel.ClipModel?.Target;
                    if (target == null) {
                        return;
                    }

                    // Event生成
                    Model.AddEvent(rangeType);
                });
            }
            
            menu.AddSeparator("");
            
            // トラック削除
            menu.AddItem(new GUIContent("Delete Track"), false, () => {
                _editorModel.ClipModel.RemoveTrack(Model.Target as SequenceTrack);
            });

            menu.ShowAsContext();
        }
    }
}