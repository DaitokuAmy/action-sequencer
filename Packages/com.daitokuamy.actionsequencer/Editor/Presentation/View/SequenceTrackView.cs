using System;
using System.Collections.Generic;
using ActionSequencer.Editor.VisualElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Track用のView
    /// </summary>
    [UxmlElement]
    public sealed partial class SequenceTrackView : VisualElement {
        private VisualElement _trackEventContainer;
        private VisualElement _reorderIndicatorView;
        private readonly List<SequenceEventView> _eventViews = new();

        /// <summary>Track 全体のスペーサー要素</summary>
        public SequenceEventSpacerView SpacerView { get; private set; }
        /// <summary>スペーサークリック時に発火する</summary>
        public event Action SpacerClicked;
        /// <inheritdoc/>
        public override VisualElement contentContainer => _trackEventContainer;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceTrackView() {
            AddToClassList("track__box");

            // Spacerを追加
            SpacerView = new SequenceEventSpacerView();
            hierarchy.Add(SpacerView);

            // 子要素の追加用コンテナ
            _trackEventContainer = new VisualElement();
            _trackEventContainer.name = "track-event-container";
            _trackEventContainer.AddToClassList("track__container");
            hierarchy.Add(_trackEventContainer);

            _reorderIndicatorView = new VisualElement();
            _reorderIndicatorView.AddToClassList("track__reorder_indicator");
            _reorderIndicatorView.style.display = DisplayStyle.None;
            _reorderIndicatorView.pickingMode = PickingMode.Ignore;
            _trackEventContainer.Add(_reorderIndicatorView);

            // Spacerのクリックを監視
            SpacerView.RegisterCallback<MouseDownEvent>(evt => {
                if (evt.button == 0) {
                    SpacerClicked?.Invoke();
                }
            });
        }

        /// <summary>
        /// Track全体の幅を設定
        /// </summary>
        public void SetTrackArea(float min, float max) {
            SpacerView.style.width = max - min;
            SpacerView.style.marginLeft = min;
        }

        /// <summary>
        /// EventView追加
        /// </summary>
        public void AddEventView(SequenceEventView eventView) {
            _eventViews.Add(eventView);
            _trackEventContainer.Add(eventView);
        }

        /// <summary>
        /// EventView削除
        /// </summary>
        public void RemoveEventView(SequenceEventView eventView) {
            _eventViews.Remove(eventView);
            _trackEventContainer.Remove(eventView);
        }

        /// <summary>
        /// イベントのフォルダリング状態反映
        /// </summary>
        public void SetFoldout(bool foldout) {
            _trackEventContainer.style.display = foldout ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// 並び替え位置インジケータを表示
        /// </summary>
        /// <param name="insertIndex">差し込み予定 index</param>
        public void ShowReorderIndicator(int insertIndex) {
            if (_eventViews.Count == 0) {
                HideReorderIndicator();
                return;
            }

            insertIndex = Mathf.Clamp(insertIndex, 0, _eventViews.Count);

            var top = GetIndicatorTop(insertIndex);
            _reorderIndicatorView.style.top = top;
            _reorderIndicatorView.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// 並び替え位置インジケータを非表示
        /// </summary>
        public void HideReorderIndicator() {
            _reorderIndicatorView.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// 差し込み位置に応じたインジケータの Y 座標を返す
        /// </summary>
        /// <param name="insertIndex">差し込み予定 index</param>
        /// <returns>表示する Y 座標</returns>
        private float GetIndicatorTop(int insertIndex) {
            var top = 0.0f;
            for (var index = 0; index < insertIndex && index < _eventViews.Count; index++) {
                top += GetEventRowHeight(_eventViews[index]);
            }

            return top;
        }

        /// <summary>
        /// EventView 1 行ぶんの高さを返す
        /// </summary>
        /// <param name="eventView">対象の EventView</param>
        /// <returns>行全体の高さ</returns>
        private static float GetEventRowHeight(SequenceEventView eventView) {
            var resolvedStyle = eventView.resolvedStyle;
            return resolvedStyle.marginTop + resolvedStyle.height + resolvedStyle.marginBottom;
        }
    }
}
