using System.Collections.Generic;
using ActionSequencer.Editor.Utils;
using ActionSequencer.Editor.VisualElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Track用のView
    /// </summary>
    public class SequenceTrackView : VisualElement {
        public new class UxmlFactory : UxmlFactory<SequenceTrackView, UxmlTraits> {
        }

        private VisualElement _trackEventContainer;
        private List<SequenceEventView> _eventViews = new List<SequenceEventView>();

        public RulerView RulerView { get; private set; }
        public SequenceEventSpacerView SpacerView { get; private set; }
        public Subject ClickedSpacerSubject { get; } = new Subject();
        public override VisualElement contentContainer => _trackEventContainer;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceTrackView() {
            AddToClassList("track__box");

            // Rulerを追加
            RulerView = new RulerView();
            RulerView.LineColor = new Color(1.0f, 1.0f, 1.0f, 0.05f);
            RulerView.ThickLineColor = new Color(1.0f, 1.0f, 1.0f, 0.1f);
            RulerView.LineHeightRate = 1.0f;
            RulerView.ThickLineHeightRate = 1.0f;
            hierarchy.Add(RulerView);

            // Spacerを追加
            SpacerView = new SequenceEventSpacerView();
            hierarchy.Add(SpacerView);

            // 子要素の追加用コンテナ
            _trackEventContainer = new VisualElement();
            _trackEventContainer.name = "track-event-container";
            _trackEventContainer.AddToClassList("track__container");
            hierarchy.Add(_trackEventContainer);

            // Spacerのクリックを監視
            SpacerView.RegisterCallback<MouseDownEvent>(evt => {
                if (evt.button == 0) {
                    ClickedSpacerSubject.Invoke();
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
    }
}