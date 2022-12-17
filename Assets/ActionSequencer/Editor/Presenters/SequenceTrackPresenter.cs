using System;
using System.Collections.Generic;
using System.Linq;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceTrack用のPresenter
    /// </summary>
    public class SequenceTrackPresenter : Presenter<SequenceTrackModel, SequenceTrackLabelView>
    {
        private SequenceEditorModel _editorModel;
        private List<SignalSequenceEventPresenter> _signalEventPresenters = new List<SignalSequenceEventPresenter>();
        private List<RangeSequenceEventPresenter> _rangeEventPresenters = new List<RangeSequenceEventPresenter>();

        public SequenceTrackView TrackView { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceTrackPresenter(SequenceTrackModel model, SequenceTrackLabelView view, SequenceTrackView trackView, SequenceEditorModel editorModel)
            : base(model, view) {
            TrackView = trackView;
            _editorModel = editorModel;

            Model.OnAddedRangeEventModel += OnAddedRangeEventModel;
            Model.OnAddedSignalEventModel += OnAddedSignalEventModel;
            Model.OnRemovedSignalEventModel += RemovedSignalEventModel;
            Model.OnRemovedRangeEventModel += RemovedRangeEventModel;
            Model.OnChangedLabel += OnChangedLabel;
            Model.OnChangedEventTime += OnChangedEventTime;

            View.OnChangedLabel += OnChangedLabelView;
            View.OnClickedOption += OnClickedOption;
            View.OnChangedFoldout += OnChangedFoldout;

            // TimeToSize監視
            AddDisposable(_editorModel.TimeToSize
                .Subscribe(x => {
                    OnChangedEventTime();
                }));
            
            // Rulerの情報反映
            TrackView.RulerView.MaskElement = TrackView.parent.parent;
            AddDisposable(_editorModel.TimeToSize
                .Subscribe(_ =>
                {
                    TrackView.RulerView.MemorySize = SequenceEditorUtility.CalcMemorySize(_editorModel);
                }));
            AddDisposable(_editorModel.CurrentTimeMode
                .Subscribe(timeMode =>
                {
                    TrackView.RulerView.ThickCycle = SequenceEditorUtility.GetThickCycle(timeMode);
                    TrackView.RulerView.MemorySize = SequenceEditorUtility.CalcMemorySize(_editorModel);
                }));

            // 既に登録済のModelを解釈
            for (var i = 0; i < Model.SignalEventModels.Count; i++)
            {
                var eventModel = Model.SignalEventModels[i];
                OnAddedSignalEventModel(eventModel);
            }
            for (var i = 0; i < Model.RangeEventModels.Count; i++)
            {
                var eventModel = Model.RangeEventModels[i];
                OnAddedRangeEventModel(eventModel);
            }
            
            OnChangedLabel(Model.Label);
            OnChangedFoldout(View.Foldout);
            OnChangedEventTime();
        }
        
        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            
            Model.OnAddedRangeEventModel -= OnAddedRangeEventModel;
            Model.OnAddedSignalEventModel -= OnAddedSignalEventModel;
            Model.OnRemovedSignalEventModel -= RemovedSignalEventModel;
            Model.OnRemovedRangeEventModel -= RemovedRangeEventModel;
            Model.OnChangedLabel -= OnChangedLabel;
            Model.OnChangedEventTime -= OnChangedEventTime;

            View.OnChangedLabel -= OnChangedLabelView;
            View.OnClickedOption -= OnClickedOption;

            foreach (var presenter in _signalEventPresenters)
            {
                presenter.Dispose();
            }

            foreach (var presenter in _rangeEventPresenters)
            {
                presenter.Dispose();
            }
        }

        /// <summary>
        /// EventのViewをソートする
        /// </summary>
        private void SortEventViews()
        {
            var sequenceTrack = Model.Target as SequenceTrack;
            if (sequenceTrack == null)
            {
                return;
            }
            
            var eventList = new List<SequenceEvent>(sequenceTrack.sequenceEvents);
            TrackView.Sort((a, b) =>
            {
                var sequenceEventA = a.userData as SequenceEvent;
                var sequenceEventB = b.userData as SequenceEvent;
                var indexA = eventList.IndexOf(sequenceEventA);
                var indexB = eventList.IndexOf(sequenceEventB);
                return indexA - indexB;
            });
        }

        /// <summary>
        /// SignalEventModel追加時
        /// </summary>
        private void OnAddedSignalEventModel(SignalSequenceEventModel model)
        {
            // EventView作成
            var view = new SignalSequenceEventView();
            view.userData = model.Target;
            TrackView.AddEventView(view);
            
            // TrackLabelの要素を追加
            var element = View.AddElement();

            // Presenter作成
            var presenter = new SignalSequenceEventPresenter(model, view, element, _editorModel);
            _signalEventPresenters.Add(presenter);
            
            // Viewのソート
            SortEventViews();
            
            // Track幅計算しなおし
            OnChangedEventTime();
        }

        /// <summary>
        /// RangeEventModel追加時
        /// </summary>
        private void OnAddedRangeEventModel(RangeSequenceEventModel model)
        {
            // EventView作成
            var view = new RangeSequenceEventView();
            view.userData = model.Target;
            TrackView.AddEventView(view);
            
            // TrackLabelの要素を追加
            var element = View.AddElement();

            // Presenter作成
            var presenter = new RangeSequenceEventPresenter(model, view, element, _editorModel);
            _rangeEventPresenters.Add(presenter);
            
            // Viewのソート
            SortEventViews();
            
            // Track幅計算しなおし
            OnChangedEventTime();
        }

        /// <summary>
        /// SignalEventModel削除時
        /// </summary>
        private void RemovedSignalEventModel(SignalSequenceEventModel model)
        {
            var presenter = _signalEventPresenters.FirstOrDefault(x => x.Model == model);
            if (presenter == null)
            {
                return;
            }
            
            // EventView削除
            TrackView.RemoveEventView(presenter.View);
            
            // Label要素を削除
            View.RemoveElement(presenter.LabelElementView);
            
            // Presenter削除
            presenter.Dispose();
            _signalEventPresenters.Remove(presenter);
            
            // Eventがなくなった場合、自身を削除
            if (Model.EventCount <= 0)
            {
                _editorModel.ClipModel.RemoveTrack(Model.Target as SequenceTrack);
            }
            
            // Track幅計算しなおし
            OnChangedEventTime();
        }

        /// <summary>
        /// RangeEventModel削除時
        /// </summary>
        private void RemovedRangeEventModel(RangeSequenceEventModel model)
        {
            var presenter = _rangeEventPresenters.FirstOrDefault(x => x.Model == model);
            if (presenter == null)
            {
                return;
            }
            
            // EventView削除
            TrackView.RemoveEventView(presenter.View);
            
            // Label要素を削除
            View.RemoveElement(presenter.LabelElementView);
            
            // Presenter削除
            presenter.Dispose();
            _rangeEventPresenters.Remove(presenter);
            
            // Eventがなくなった場合、自身を削除
            if (Model.EventCount <= 0)
            {
                _editorModel.ClipModel.RemoveTrack(Model.Target as SequenceTrack);
            }
            
            // Track幅計算しなおし
            OnChangedEventTime();
        }

        /// <summary>
        /// Label変更時
        /// </summary>
        private void OnChangedLabel(string label)
        {
            View.Label = label;
        }

        /// <summary>
        /// Track内部のEventの時間変化時通知
        /// </summary>
        private void OnChangedEventTime() {
            var maxTime = 0.0f;
            foreach (var presenter in _signalEventPresenters) {
                if (presenter.Model is SignalSequenceEventModel model) {
                    maxTime = Mathf.Max(maxTime, model.Time);
                }
            }
            foreach (var presenter in _rangeEventPresenters) {
                if (presenter.Model is RangeSequenceEventModel model) {
                    maxTime = Mathf.Max(maxTime, model.ExitTime);
                }
            }

            var width = maxTime * _editorModel.TimeToSize.Value;
            TrackView.SetTrackWidth(width);
        }

        /// <summary>
        /// View経由でのLabel変更通知
        /// </summary>
        private void OnChangedLabelView(string label)
        {
            Model.Label = label;
        }

        /// <summary>
        /// フォルダリング状態の変化通知
        /// </summary>
        private void OnChangedFoldout(bool foldout)
        {
            TrackView.SetFoldout(foldout);
        }

        /// <summary>
        /// View経由でのDefaultLabelボタン押下通知
        /// </summary>
        private void OnClickedOption()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Up"), false, () => {
                // todo:並び順変更
            });
            menu.AddItem(new GUIContent("Down"), false, () => {
                // todo:並び順変更
            });
            menu.ShowAsContext();
        }
    }
}
