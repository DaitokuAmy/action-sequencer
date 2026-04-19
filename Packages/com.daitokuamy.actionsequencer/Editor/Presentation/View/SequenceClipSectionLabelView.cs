using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Clip セクション見出し用のラベル View
    /// </summary>
    [UxmlElement]
    public sealed partial class SequenceClipSectionLabelView : VisualElement {
        private readonly Label _labelView;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceClipSectionLabelView() {
            AddToClassList("clip_section_label__box");

            _labelView = new Label();
            _labelView.AddToClassList("clip_section_label__text");
            hierarchy.Add(_labelView);
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
