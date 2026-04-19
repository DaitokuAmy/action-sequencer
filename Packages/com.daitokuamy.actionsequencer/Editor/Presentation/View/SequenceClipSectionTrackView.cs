using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Clip セクション見出し用のタイムライン View
    /// </summary>
    [UxmlElement]
    public sealed partial class SequenceClipSectionTrackView : VisualElement {
        private readonly Label _labelView;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceClipSectionTrackView() {
            AddToClassList("clip_section_track__box");

            var leftLine = new VisualElement();
            leftLine.AddToClassList("clip_section_track__line");
            hierarchy.Add(leftLine);

            _labelView = new Label();
            _labelView.AddToClassList("clip_section_track__text");
            hierarchy.Add(_labelView);

            var rightLine = new VisualElement();
            rightLine.AddToClassList("clip_section_track__line");
            hierarchy.Add(rightLine);
        }

        /// <summary>
        /// 表示名を更新
        /// </summary>
        /// <param name="displayName">表示する名前</param>
        public void SetDisplayName(string displayName) {
            _labelView.text = displayName;
        }
    }
}
