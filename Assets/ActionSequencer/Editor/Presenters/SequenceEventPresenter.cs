using System.Linq;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceEvent用のPresenter基底
    /// </summary>
    public abstract class SequenceEventPresenter : Presenter<SerializedObjectModel, SequenceEventView>
    {
        protected SequenceEditorModel EditorModel { get; private set; }
        
        public SequenceEventPresenter(SerializedObjectModel model, SequenceEventView view, SequenceEditorModel editorModel)
            : base(model, view)
        {
            EditorModel = editorModel;
            
            LabelView.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            EditorModel.OnChangedSelectedTargets += OnChangedSelectedTargets;
            
            // Drag監視
            LabelView.Manipulator.OnDragStart += OnDragStart;
            LabelView.Manipulator.OnDragging += OnDragging;

            EditorModel.OnEventDragStart += OnEventDragStart;
            EditorModel.OnEventDragging += OnEventDragging;
        }
        
        public override void Dispose()
        {
            base.Dispose();
            
            LabelView.UnregisterCallback<MouseDownEvent>(OnMouseDownEvent);
            
            LabelView.Manipulator.OnDragStart -= OnDragStart;
            LabelView.Manipulator.OnDragging -= OnDragging;

            EditorModel.OnEventDragStart -= OnEventDragStart;
            EditorModel.OnEventDragging -= OnEventDragging;
        }

        private void OnDragStart(SequenceEventManipulator.DragType dragType)
        {
            OnDragStart(dragType, false);
            EditorModel.StartDragEvent(Model.Target, dragType);
        }
        protected abstract void OnDragStart(SequenceEventManipulator.DragType dragType, bool otherEvent);
        private void OnDragging(SequenceEventManipulator.DragInfo info)
        {
            OnDragging(info, false);
            EditorModel.DraggingEvent(Model.Target, info);
        }
        protected abstract void OnDragging(SequenceEventManipulator.DragInfo info, bool otherEvent);

        private void OnEventDragStart(Object target, SequenceEventManipulator.DragType dragType)
        {
            if (target == Model.Target || !LabelView.Selected)
            {
                return;
            }

            OnDragStart(dragType, true);
        }

        private void OnEventDragging(Object target, SequenceEventManipulator.DragInfo info)
        {
            if (target == Model.Target || !LabelView.Selected)
            {
                return;
            }

            OnDragging(info, true);
        }

        private void OnMouseDownEvent(MouseDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            if (evt.commandKey || evt.ctrlKey)
            {
                EditorModel.AddTarget(Model.Target);
            }
            else
            {
                EditorModel.SetTarget(Model.Target);
            }
        }

        private void OnChangedSelectedTargets(Object[] targets)
        {
            var selected = targets.Contains(Model.Target);
            LabelView.Selected = selected;
        }

        /// <summary>
        /// Editor上のサイズを時間に変換
        /// </summary>
        protected float SizeToTime(float position)
        {
            return position / EditorModel.TimeToSize;
        }

        /// <summary>
        /// 時間をEditor上のサイズに変換
        /// </summary>
        protected float TimeToSize(float time)
        {
            return time * EditorModel.TimeToSize;
        }
    }
}
