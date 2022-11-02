using System;
using System.Collections.Generic;
using System.Linq;
using ActionSequencer.Editor.Utils;
using PlasticPipe.Server;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

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
        private List<IDisposable> _disposables = new List<IDisposable>();

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
            Model.OnRemoveSignalEventModel += OnRemoveSignalEventModel;
            Model.OnRemoveRangeEventModel += OnRemoveRangeEventModel;
            Model.OnChangedLabel += OnChangedLabel;

            View.OnChangedLabel += OnChangedLabelView;
            
            // Rulerの情報反映
            TrackView.RulerView.MaskElement = TrackView.parent.parent;
            _disposables.Add(_editorModel.TimeToSize
                .Subscribe(_ =>
                {
                    TrackView.RulerView.MemorySize = SequenceEditorUtility.CalcMemorySize(_editorModel);
                }));
            _disposables.Add(_editorModel.CurrentTimeMode
                .Subscribe(timeMode =>
                {
                    TrackView.RulerView.ThickCycle = SequenceEditorUtility.GetThickCycle(timeMode);
                    TrackView.RulerView.MemorySize = SequenceEditorUtility.CalcMemorySize(_editorModel);
                }));
            
            // ラベル初期化
            OnChangedLabel(Model.Label);

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
        }
        
        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
            _disposables.Clear();
            
            Model.OnAddedRangeEventModel -= OnAddedRangeEventModel;
            Model.OnAddedSignalEventModel -= OnAddedSignalEventModel;
            Model.OnRemoveSignalEventModel -= OnRemoveSignalEventModel;
            Model.OnRemoveRangeEventModel -= OnRemoveRangeEventModel;
            Model.OnChangedLabel -= OnChangedLabel;

            View.OnChangedLabel -= OnChangedLabelView;

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
            var view = new SignalSequenceEventView();
            view.userData = model.Target;
            TrackView.Add(view);
                    
            var presenter = new SignalSequenceEventPresenter(model, view, _editorModel);
            _signalEventPresenters.Add(presenter);

            // 行数変更
            View.LineCount = Model.EventCount;
            
            // Viewのソート
            SortEventViews();
        }

        /// <summary>
        /// RangeEventModel追加時
        /// </summary>
        private void OnAddedRangeEventModel(RangeSequenceEventModel model)
        {
            var view = new RangeSequenceEventView();
            view.userData = model.Target;
            TrackView.Add(view);

            var presenter = new RangeSequenceEventPresenter(model, view, _editorModel);
            _rangeEventPresenters.Add(presenter);

            // 行数変更
            View.LineCount = Model.EventCount;
            
            // Viewのソート
            SortEventViews();
        }

        /// <summary>
        /// SignalEventModel削除時
        /// </summary>
        private void OnRemoveSignalEventModel(SignalSequenceEventModel model)
        {
            var presenter = _signalEventPresenters.FirstOrDefault(x => x.Model == model);
            if (presenter == null)
            {
                return;
            }
            TrackView.Remove(presenter.View);
            presenter.Dispose();
            _signalEventPresenters.Remove(presenter);

            // 行数変更(削除前なので-1)
            View.LineCount = Model.EventCount - 1;
        }

        /// <summary>
        /// RangeEventModel削除時
        /// </summary>
        private void OnRemoveRangeEventModel(RangeSequenceEventModel model)
        {
            var presenter = _rangeEventPresenters.FirstOrDefault(x => x.Model == model);
            if (presenter == null)
            {
                return;
            }
            TrackView.Remove(presenter.View);
            presenter.Dispose();
            _rangeEventPresenters.Remove(presenter);

            // 行数変更(削除前なので-1)
            View.LineCount = Model.EventCount - 1;
        }

        /// <summary>
        /// Label変更時
        /// </summary>
        private void OnChangedLabel(string label)
        {
            View.Label = label;
        }

        /// <summary>
        /// View経由でのLabel変更通知
        /// </summary>
        private void OnChangedLabelView(string label)
        {
            Model.Label = label;
        }
    }
}
