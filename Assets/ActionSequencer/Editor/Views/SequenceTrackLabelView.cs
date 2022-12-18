using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SignalTrack用のView
    /// </summary>
    public class SequenceTrackLabelView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<SequenceTrackLabelView, UxmlTraits> {
        }

        private Foldout _foldout;
        private TextField _textFieldView;
        private Button _optionButton;
        private List<SequenceTrackLabelElementView> _elementViews = new List<SequenceTrackLabelElementView>();

        public event Action<string> OnChangedLabel;
        public event Action OnClickedOption;
        public event Action<bool> OnChangedFoldout;

        // 表示ラベル
        public string Label
        {
            get => _textFieldView.value;
            set => _textFieldView.value = value;
        }
        // フォルダリング状態
        public bool Foldout {
            get => _foldout.value;
        }
        // コンテナ
        public override VisualElement contentContainer => _foldout.contentContainer;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceTrackLabelView() {
            AddToClassList("track_label__box");
            
            // Foldoutボタン作成
            _foldout = new Foldout();
            _foldout.AddToClassList("track_label__foldout");
            hierarchy.Add(_foldout);
                
            // TextField作成
            _textFieldView = new TextField();
            _textFieldView.AddToClassList("track_label__textfield");
            hierarchy.Add(_textFieldView);
            
            // ResetButton作成
            _optionButton = new Button();
            _optionButton.AddToClassList("track_label__option");
            _optionButton.focusable = false;
            hierarchy.Add(_optionButton);

            // 値の変化監視
            _textFieldView.RegisterValueChangedCallback(evt =>
            {
                OnChangedLabel?.Invoke(evt.newValue);
            });
            
            // ボタンの押下監視
            _optionButton.clicked += () => OnClickedOption?.Invoke();
            
            // フォルダの状態監視
            _foldout.RegisterValueChangedCallback(evt => {
                OnChangedFoldout?.Invoke(evt.newValue);
            });
        }

        /// <summary>
        /// 要素の削除
        /// </summary>
        public void ResetElements() {
            _elementViews.Clear();
            _foldout.Clear();
        }

        /// <summary>
        /// 要素の追加
        /// </summary>
        public SequenceTrackLabelElementView AddElement() {
            var elementView = new SequenceTrackLabelElementView();
            _elementViews.Add(elementView);
            _foldout.Add(elementView);
            return elementView;
        }

        /// <summary>
        /// 要素の削除
        /// </summary>
        public void RemoveElement(SequenceTrackLabelElementView elementView) {
            if (!_elementViews.Remove(elementView)) {
                return;
            }
            _foldout.Remove(elementView);
        }
    }
}
