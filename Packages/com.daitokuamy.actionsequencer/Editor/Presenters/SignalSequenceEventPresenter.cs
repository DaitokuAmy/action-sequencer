using System;
using System.Collections.Generic;
using UnityEditor.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SignalSequenceEvent用のPresenter
    /// </summary>
    internal class SignalSequenceEventPresenter : SequenceEventPresenter {
        private SignalSequenceEventModel _model;
        private SignalSequenceEventView _view;

        private float _dragStartTime;

        public SignalSequenceEventPresenter(SignalSequenceEventModel model, SignalSequenceEventView view,
            SequenceTrackLabelElementView labelElementView, SequenceTrackModel trackModel,
            SequenceEditorModel editorModel)
            : base(model, view, labelElementView, trackModel, editorModel) {
            _model = model;
            _view = view;

            _view.TrackPropertyValue(Model.SerializedObject.FindProperty("time"),
                prop => {
                    _model.Time = prop.floatValue;
                });
            _view.TrackSerializedObjectValue(Model.SerializedObject, obj => {
                SetViewDuration(_model.ViewDuration);
            });

            labelElementView.LabelColor = model.ThemeColor;

            AddDisposable(EditorModel.TimeToSize
                .Subscribe(_ => ChangedTimeSubject(_model.Time)));

            // Modelの変更監視
            AddDisposable(_model.ChangedTimeSubject
                .Subscribe(ChangedTimeSubject));

            ChangedTimeSubject(_model.Time);
            SetViewDuration(_model.ViewDuration);
        }

        protected override void OnDragStart(SequenceEventManipulator.DragType dragType, bool otherEvent) {
            _dragStartTime = _model.Time;
        }

        protected override void OnDragging(SequenceEventManipulator.DragInfo info, bool otherEvent) {
            var deltaTime = SizeToTime(info.current - info.start);
            _model.Time = EditorModel.GetAbsorptionTime(_dragStartTime + deltaTime);
        }

        private void ChangedTimeSubject(float time) {
            // 位置として反映
            _view.Position = TimeToSize(time);
            EditorModel.RootElement.MarkDirtyRepaint();
        }

        private void SetViewDuration(float duration) {
            // 幅として反映
            _view.Width = TimeToSize(duration);
            EditorModel.RootElement.MarkDirtyRepaint();
        }
    }
}