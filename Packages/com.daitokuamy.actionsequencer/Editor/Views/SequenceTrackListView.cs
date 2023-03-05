using ActionSequencer.Editor.VisualElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Track格納用のView
    /// </summary>
    public class SequenceTrackListView : VisualElement {
        public new class UxmlFactory : UxmlFactory<SequenceTrackListView, UxmlTraits> {
        }

        private VisualElement _trackContainer;
        private RulerView _rulerView;
        private VisualElement _gray;
        
        public override VisualElement contentContainer => _trackContainer;
        public RulerView RulerView => _rulerView;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceTrackListView() {
            // Ruler追加
            _rulerView = new RulerView();
            _rulerView.LineColor = new Color(1.0f, 1.0f, 1.0f, 0.05f);
            _rulerView.ThickLineColor = new Color(1.0f, 1.0f, 1.0f, 0.1f);
            _rulerView.LineHeightRate = 1.0f;
            _rulerView.ThickLineHeightRate = 1.0f;
            hierarchy.Add(_rulerView);
            
            // Gray追加
            _gray = new VisualElement();
            _gray.name = "track-list-gray";
            _gray.pickingMode = PickingMode.Ignore;
            _gray.AddToClassList("track_list__gray");
            hierarchy.Add(_gray);
            
            // 子要素の追加用コンテナ
            _trackContainer = new VisualElement();
            _trackContainer.name = "track-list-container";
            _trackContainer.AddToClassList("track_list__container");
            hierarchy.Add(_trackContainer);
        }
    }
}