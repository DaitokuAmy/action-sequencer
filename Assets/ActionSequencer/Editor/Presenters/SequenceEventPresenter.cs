using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceEvent用のPresenter基底
    /// </summary>
    public abstract class SequenceEventPresenter : Presenter<SequenceEventModel, SequenceEventView>
    {
        public SequenceTrackLabelElementView LabelElementView { get; private set; }
        protected SequenceEditorModel EditorModel { get; private set; }
        
        public SequenceEventPresenter(SequenceEventModel model, SequenceEventView view, SequenceTrackLabelElementView labelElementView, SequenceEditorModel editorModel)
            : base(model, view)
        {
            EditorModel = editorModel;
            LabelElementView = labelElementView;
            
            AddDisposable(EditorModel.ChangedSelectedTargetsSubject
                .Subscribe(ChangedSelectedTargetsSubject));
            
            // SerializedPropertyの変化監視
            View.Unbind();
            View.TrackPropertyValue(Model.SerializedObject.FindProperty("active"), prop =>
            {
                Model.Active = prop.boolValue;
            });

            // Label要素初期化
            LabelElementView.LabelColor = model.ThemeColor;
            LabelElementView.Label = Model.Label;

            // Label要素のイベント監視            
            AddDisposable(LabelElementView.ChangedSubject
                .Subscribe(OnChangedViewLabel));
            AddDisposable(LabelElementView.ClickedOptionSubject
                .Subscribe(ClickedOptionSubject));
            
            // イベント監視
            View.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            View.RegisterCallback<ValidateCommandEvent>(OnValidateCommandEvent);
            
            // アクティブ状態の監視
            AddDisposable(Model.ChangedActiveSubject
                .Subscribe(ChangedActiveSubject));
            AddDisposable(Model.ChangedLabelSubject
                .Subscribe(ChangedLabelSubject));

            ChangedActiveSubject(Model.Active);
            
            // 右クリック監視
            AddDisposable(View.OpenContextMenuSubject
                .Subscribe(OpenContextMenuSubject));
            
            // Drag監視
            View.Manipulator.OnDragStart += OnDragStart;
            View.Manipulator.OnDragging += OnDragging;

            AddDisposable(EditorModel.EventDragStartSubject
                .Subscribe(EventDragStartSubject));
            AddDisposable(EditorModel.EventDraggingSubject
                .Subscribe(EventDraggingSubject));
        }
        
        public override void Dispose()
        {
            base.Dispose();
            
            EditorModel.RemoveSelectedTarget(Model.Target);
            
            View.UnregisterCallback<MouseDownEvent>(OnMouseDownEvent);
            View.UnregisterCallback<ValidateCommandEvent>(OnValidateCommandEvent);
            
            View.Manipulator.OnDragStart -= OnDragStart;
            View.Manipulator.OnDragging -= OnDragging;
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

        private void EventDragStartSubject(Object target, SequenceEventManipulator.DragType dragType)
        {
            if (target == Model.Target || !View.Selected)
            {
                return;
            }

            OnDragStart(dragType, true);
        }

        private void EventDraggingSubject(Object target, SequenceEventManipulator.DragInfo info)
        {
            if (target == Model.Target || !View.Selected)
            {
                return;
            }

            OnDragging(info, true);
        }

        /// <summary>
        /// マウス押下時処理
        /// </summary>
        private void OnMouseDownEvent(MouseDownEvent evt)
        {
            if (evt.button != 0 && evt.button != 1)
            {
                return;
            }

            if (evt.commandKey || evt.ctrlKey)
            {
                EditorModel.AddSelectedTarget(Model.Target);
            }
            else
            {
                EditorModel.SetSelectedTarget(Model.Target);
            }
        }

        /// <summary>
        /// コマンド要求処理
        /// </summary>
        private void OnValidateCommandEvent(ValidateCommandEvent evt)
        {
            if (evt.commandName == "Duplicate")
            {
                DuplicateSelectedEvents();
            }
            else if (evt.commandName == "Delete" || evt.commandName == "SoftDelete")
            {
                DeleteSelectedEvents();
            }
        }

        /// <summary>
        /// 選択対象の変更通知
        /// </summary>
        private void ChangedSelectedTargetsSubject(Object[] targets)
        {
            var selected = targets.Contains(Model.Target);
            View.Selected = selected;
        }

        /// <summary>
        /// アクティブ状態の切り替え通知
        /// </summary>
        private void ChangedActiveSubject(bool active)
        {
            View.style.backgroundColor = active ? Model.ThemeColor : Color.gray;
        }
        
        /// <summary>
        /// ラベル変更時
        /// </summary>
        private void OnChangedViewLabel(string label) {
            Model.Label = label;
        }
        
        /// <summary>
        /// ラベル変更時
        /// </summary>
        private void ChangedLabelSubject(string label) {
            LabelElementView.Label = label;
        }
        
        /// <summary>
        /// オプション押下時
        /// </summary>
        private void ClickedOptionSubject() {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Reset Label"), false, () => {
                // ラベルのリセット
                Model.ResetLabel();
            });
            menu.AddItem(new GUIContent("Up"), false, () => {
                // todo:並び順変更
            });
            menu.AddItem(new GUIContent("Down"), false, () => {
                // todo:並び順変更
            });
            menu.ShowAsContext();
        }

        /// <summary>
        /// Editor上のサイズを時間に変換
        /// </summary>
        protected float SizeToTime(float position)
        {
            return position / EditorModel.TimeToSize.Value;
        }

        /// <summary>
        /// 時間をEditor上のサイズに変換
        /// </summary>
        protected float TimeToSize(float time)
        {
            return time * EditorModel.TimeToSize.Value;
        }

        /// <summary>
        /// コンテキストメニューを開いた時の処理
        /// </summary>
        private void OpenContextMenuSubject(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Duplicate", action =>
            {
                DuplicateSelectedEvents();
            });
            evt.menu.AppendAction("Delete", action =>
            {
                DeleteSelectedEvents();
            });
            evt.menu.AppendAction(Model.Active ? "Deactivate" : "Activate", action =>
            {
                SetActiveSelectedEvents(!Model.Active);
            });
        }

        /// <summary>
        /// 選択中のEventを複製
        /// </summary>
        private void DuplicateSelectedEvents()
        {
            var events = EditorModel.SelectedTargets
                .OfType<SequenceEvent>();
            foreach (var evt in events)
            {
                Model.TrackModel.DuplicateEvent(evt);
            }
        }

        /// <summary>
        /// 選択中のEventを削除
        /// </summary>
        private void DeleteSelectedEvents()
        {
            var events = EditorModel.SelectedTargets
                .OfType<SequenceEvent>();
            foreach (var evt in events)
            {
                Model.TrackModel.RemoveEvent(evt);
            }
        }

        /// <summary>
        /// 選択中のEventのアクティブ状態を設定
        /// </summary>
        private void SetActiveSelectedEvents(bool active)
        {
            var events = EditorModel.SelectedTargets
                .OfType<SequenceEvent>();
            foreach (var evt in events)
            {
                var serializedObj = new SerializedObject(evt);
                serializedObj.Update();
                serializedObj.FindProperty("active").boolValue = active;
                serializedObj.ApplyModifiedProperties();
            }
        }
    }
}
