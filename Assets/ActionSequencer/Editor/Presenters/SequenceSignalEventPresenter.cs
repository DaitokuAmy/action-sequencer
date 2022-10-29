using UnityEditor.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceSignalEvent用のPresenter
    /// </summary>
    public class SequenceSignalEventPresenter : SequenceEventPresenter
    {
        private SequenceSignalEventModel _model;
        private SequenceSignalEventView _view;

        private float _dragStartTime;
        
        public SequenceSignalEventPresenter(SequenceSignalEventModel model, SequenceSignalEventView view, SequenceEditorModel editorModel)
            : base(model, view, editorModel)
        {
            _model = model;
            _view = view;
            
            _view.Unbind();
            _view.TrackPropertyValue(Model.SerializedObject.FindProperty("time"), prop =>
            {
                _model.Time = prop.floatValue;
            });
            
            // Modelの時間変更監視
            _model.OnChangedTime += OnChangedTime;
            
            OnChangedTime(_model.Time);
        }
        
        public override void Dispose()
        {
            base.Dispose();
            _model.OnChangedTime -= OnChangedTime;
        }

        protected override void OnDragStart(SequenceEventManipulator.DragType dragType, bool otherEvent)
        {
            _dragStartTime = _model.Time;
        }
        protected override void OnDragging(SequenceEventManipulator.DragInfo info, bool otherEvent)
        {
            var deltaTime = SizeToTime(info.current - info.start);
            _model.Time = _dragStartTime + deltaTime;
        }

        private void OnChangedTime(float time)
        {
            // 位置として反映
            _view.style.marginLeft = TimeToSize(time);
            EditorModel.RootElement.MarkDirtyRepaint();
        }
    }
}
