using ActionSequencer.Editor.VisualElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Track格納用のView
    /// </summary>
    [UxmlElement]
    public sealed partial class SequenceTrackListView : VisualElement {
        private VisualElement _trackContainer;
        private RulerView _rulerView;
        private VisualElement _gray;

        /// <inheritdoc/>
        public override VisualElement contentContainer => _trackContainer;
        /// <summary>上部のルーラー表示</summary>
        public RulerView RulerView => _rulerView;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceTrackListView() {
            // Ruler追加
            _rulerView = new RulerView();
            _rulerView.ShowLabels = false;
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

        /// <summary>
        /// ルーラーのマスク要素を設定
        /// </summary>
        /// <param name="maskElement">描画範囲に使用する要素</param>
        public void SetRulerMask(VisualElement maskElement) {
            _rulerView.MaskElement = maskElement;
        }

        /// <summary>
        /// ルーラーの表示幅を更新
        /// </summary>
        /// <param name="width">表示幅</param>
        public void SetRulerWidth(float width) {
            _rulerView.style.width = width;
        }

        /// <summary>
        /// 横スクロール量に応じてルーラー位置を更新
        /// </summary>
        /// <param name="offset">横スクロール量</param>
        public void SetRulerOffset(float offset) {
            _rulerView.style.translate = new Translate(0.0f, 0.0f);
        }
    }
}
