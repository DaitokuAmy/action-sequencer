using System;
using ActionSequencer.Editor.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceTrackLabelのEvent毎の要素View
    /// </summary>
    public class SequenceTrackLabelElementView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<SequenceTrackLabelElementView, UxmlTraits> {}
        
        private VisualElement _rootView;
        private VisualElement _colorView;
        private TextField _textFieldView;
        private Button _optionButton;

        public Subject<string> ChangedSubject { get; } = new Subject<string>();
        public Subject ClickedOptionSubject { get; } = new Subject();

        // 表示ラベル
        public string Label
        {
            get => _textFieldView.value;
            set => _textFieldView.value = value;
        }
        
        // ラベルカラー
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
            _textFieldView.RegisterValueChangedCallback(evt =>
            {
                ChangedSubject.Invoke(evt.newValue);
            });
            
            // ボタンの押下監視
            _optionButton.clicked += () => ClickedOptionSubject.Invoke();
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
