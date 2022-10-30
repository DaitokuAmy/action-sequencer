using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceTrack用のPresenter
    /// </summary>
    public class SequenceTrackPresenter : Presenter<SequenceTrackModel, SequenceTrackLabelView>
    {
        private SequenceEditorModel _editorModel;
        private List<SequenceSignalEventPresenter> _signalEventPresenters = new List<SequenceSignalEventPresenter>();
        private List<SequenceRangeEventPresenter> _rangeEventPresenters = new List<SequenceRangeEventPresenter>();
        
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
            
            View.Unbind();
            View.TrackSerializedObjectValue(Model.SerializedObject, obj =>
            {
                //Model.RefreshEvents();
            });
            
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
        private void SortEvents()
        {
            var sequenceTrack = Model.Target as SequenceTrack;
            if (sequenceTrack == null)
            {
                return;
            }
            
            var eventList = new List<SequenceEvent>(sequenceTrack.sequenceEvents);
            View.Sort((a, b) =>
            {
                var sequenceEventA = a.userData as SequenceEvent;
                var sequenceEventB = b.userData as SequenceEvent;
                var indexA = eventList.IndexOf(sequenceEventA);
                var indexB = eventList.IndexOf(sequenceEventB);
                return indexB - indexA;
            });
        }

        /// <summary>
        /// SignalEventModel追加時
        /// </summary>
        private void OnAddedSignalEventModel(SequenceSignalEventModel model)
        {
            var view = new SequenceSignalEventView();
            view.userData = model.Target;
            TrackView.Add(view);
                    
            var presenter = new SequenceSignalEventPresenter(model, view, _editorModel);
            _signalEventPresenters.Add(presenter);

            // 行数変更
            View.LineCount = Model.EventCount;
        }

        /// <summary>
        /// RangeEventModel追加時
        /// </summary>
        private void OnAddedRangeEventModel(SequenceRangeEventModel model)
        {
            var view = new SequenceRangeEventView();
            view.userData = model.Target;
            TrackView.Add(view);

            var presenter = new SequenceRangeEventPresenter(model, view, _editorModel);
            _rangeEventPresenters.Add(presenter);

            // 行数変更
            View.LineCount = Model.EventCount;
        }

        /// <summary>
        /// SignalEventModel削除時
        /// </summary>
        private void OnRemoveSignalEventModel(SequenceSignalEventModel model)
        {
            var presenter = _signalEventPresenters.FirstOrDefault(x => x.Model == model);
            if (presenter == null)
            {
                return;
            }
            TrackView.Remove(presenter.View);
            presenter.Dispose();
            _signalEventPresenters.Remove(presenter);

            // 行数変更
            View.LineCount = Model.EventCount;
        }

        /// <summary>
        /// RangeEventModel削除時
        /// </summary>
        private void OnRemoveRangeEventModel(SequenceRangeEventModel model)
        {
            var presenter = _rangeEventPresenters.FirstOrDefault(x => x.Model == model);
            if (presenter == null)
            {
                return;
            }
            TrackView.Remove(presenter.View);
            presenter.Dispose();
            _rangeEventPresenters.Remove(presenter);

            // 行数変更
            View.LineCount = Model.EventCount;
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
