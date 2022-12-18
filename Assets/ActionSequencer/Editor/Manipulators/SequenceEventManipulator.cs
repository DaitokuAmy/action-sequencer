using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceEventView用のManipulator
    /// </summary>
    public class SequenceEventManipulator : MouseManipulator {
        public enum DragType {
            LeftSide,
            Middle,
            RightSide
        }

        public struct DragInfo {
            public DragType type;
            public float start;
            public float current;
        }

        private readonly bool _resizable;

        private bool _dragging;
        private Vector2 _startMousePosition;
        private DragType _dragType;

        // ドラッグ開始通知
        public event Action<DragType> OnDragStart;

        // ドラッグ終了通知
        public event Action<DragType> OnDragExit;

        // ドラッグ情報通知
        public event Action<DragInfo> OnDragging;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceEventManipulator(bool resizable) {
            _resizable = resizable;

            // 左クリックで有効化する
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            activators.Add(new ManipulatorActivationFilter
                { button = MouseButton.LeftMouse, modifiers = EventModifiers.Command });
            activators.Add(new ManipulatorActivationFilter
                { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
        }

        /// <summary>
        /// Target登録時の処理
        /// </summary>
        protected override void RegisterCallbacksOnTarget() {
            if (_dragging) {
                _dragging = false;
                OnDragExit?.Invoke(_dragType);
            }

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
        }

        /// <summary>
        /// ターゲット登録解除時の処理
        /// </summary>
        protected override void UnregisterCallbacksFromTarget() {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
        }

        /// <summary>
        /// マウスダウン時
        /// </summary>
        private void OnMouseDown(MouseDownEvent evt) {
            // ドラッグタイプ(サイズ変更か移動か)
            DragType GetDragType(float localClickPos, float width) {
                if (!_resizable) {
                    return DragType.Middle;
                }

                var leftSidePos = Mathf.Min(10, width / 3);
                var rightSidePos = width - Mathf.Min(10, width / 3);

                if (localClickPos < leftSidePos) {
                    return DragType.LeftSide;
                }

                if (localClickPos > rightSidePos) {
                    return DragType.RightSide;
                }

                return DragType.Middle;
            }

            if (CanStartManipulation(evt) && !_dragging) {
                _dragType = GetDragType(evt.localMousePosition.x, target.style.width.value.value);
                _startMousePosition = evt.mousePosition;
                _dragging = true;
                OnDragStart?.Invoke(_dragType);
                target.CaptureMouse();
            }
        }

        /// <summary>
        /// マウスアップ時
        /// </summary>
        private void OnMouseUp(MouseUpEvent evt) {
            // 有効化条件を満たすか
            if (_dragging) {
                target.ReleaseMouse();
                _dragging = false;
                OnDragExit?.Invoke(_dragType);
            }
        }

        /// <summary>
        /// マウスロスト時
        /// </summary>
        private void OnMouseCaptureOut(MouseCaptureOutEvent evt) {
            if (_dragging) {
                target.ReleaseMouse();
                _dragging = false;
                OnDragExit?.Invoke(_dragType);
            }
        }

        /// <summary>
        /// マウス移動時
        /// </summary>
        private void OnMouseMove(MouseMoveEvent evt) {
            if (_dragging) {
                // 移動量を反映
                OnDragging?.Invoke(new DragInfo {
                    type = _dragType,
                    start = _startMousePosition.x,
                    current = evt.mousePosition.x,
                });
            }
        }
    }
}