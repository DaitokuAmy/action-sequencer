using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceTrackLabelのEvent毎の要素View
    /// </summary>
    [UxmlElement]
    public sealed partial class SequenceTrackLabelElementView : VisualElement {
        private const string DefaultLabelClassName = "track_label__element_textfield--default";

        private VisualElement _rootView;
        private VisualElement _colorView;
        private TextField _textFieldView;
        private Button _optionButton;

        /// <summary>ラベル変更時に発火する</summary>
        public event Action<string> LabelChanged;
        /// <summary>オプションボタン押下時に発火する</summary>
        public event Action OptionClicked;

        /// <summary>ラベルカラー</summary>
        public Color LabelColor {
            set => _colorView.style.backgroundColor = value;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceTrackLabelElementView() {
            AddToClassList("track_label__element_box");

            // Header作成
            _rootView = new VisualElement();
            _rootView.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            Add(_rootView);

            // ColorLabel作成
            _colorView = new VisualElement();
            _colorView.name = "ColorLabel";
            _colorView.AddToClassList("track_label__element_color");
            _rootView.Add(_colorView);

            // TextField作成
            _textFieldView = new TextField();
            _textFieldView.AddToClassList("track_label__element_textfield");
            _rootView.Add(_textFieldView);

            // OptionButton作成
            _optionButton = new Button();
            _optionButton.AddToClassList("track_label__option");
            _optionButton.focusable = false;
            _rootView.Add(_optionButton);

            // 値の変化監視
            _textFieldView.RegisterValueChangedCallback(evt => { LabelChanged?.Invoke(evt.newValue); });

            // ボタンの押下監視
            _optionButton.clicked += () => OptionClicked?.Invoke();
        }

        /// <summary>
        /// ラベル表示を更新
        /// </summary>
        /// <param name="label">表示するラベル</param>
        /// <param name="usesDefaultLabel">デフォルトラベル表示の場合は true</param>
        public void SetLabel(string label, bool usesDefaultLabel) {
            _textFieldView.SetValueWithoutNotify(label);
            _textFieldView.EnableInClassList(DefaultLabelClassName, usesDefaultLabel);
            _textFieldView.tooltip = usesDefaultLabel ? "Default Label" : string.Empty;
        }

        /// <summary>
        /// タブIndexを設定
        /// </summary>
        public int SetTabIndices(int offset) {
            _textFieldView.tabIndex = offset++;
            return offset;
        }
    }
}
