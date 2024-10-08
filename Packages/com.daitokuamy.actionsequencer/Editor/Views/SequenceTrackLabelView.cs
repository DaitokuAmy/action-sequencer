using System;
using System.Collections.Generic;
using ActionSequencer.Editor.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SignalTrack用のView
    /// </summary>
    public class SequenceTrackLabelView : VisualElement {
        public new class UxmlFactory : UxmlFactory<SequenceTrackLabelView, UxmlTraits> {
        }

        private Foldout _foldout;
        private TextField _textFieldView;
        private Button _optionButton;
        private List<SequenceTrackLabelElementView> _elementViews = new List<SequenceTrackLabelElementView>();

        public Subject<string> ChangedLabelSubject { get; } = new Subject<string>();
        public Subject ClickedOptionSubject { get; } = new Subject();
        public Subject<bool> ChangedFoldoutSubject { get; } = new Subject<bool>();

        // 表示ラベル
        public string Label {
            get => _textFieldView.value;
            set => _textFieldView.value = value;
        }

        // フォルダリング状態
        public bool Foldout {
            get => _foldout.value;
            set => _foldout.value = value;
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
            _foldout.focusable = false;
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
            _textFieldView.RegisterValueChangedCallback(evt => { ChangedLabelSubject.Invoke(evt.newValue); });

            // ボタンの押下監視
            _optionButton.clicked += () => ClickedOptionSubject.Invoke();

            // フォルダの状態監視
            _foldout.RegisterValueChangedCallback(evt => { ChangedFoldoutSubject.Invoke(evt.newValue); });
        }

        /// <summary>
        /// 要素の削除
        /// </summary>
        public void ResetElements() {
            _elementViews.Clear();
            _foldout.Clear();
        }

        /// <summary>
        /// タブIndexを設定
        /// </summary>
        public int SetTabIndices(int offset) {
            _textFieldView.tabIndex = offset++;
            foreach (var elementView in _elementViews) {
                offset = elementView.SetTabIndices(offset);
            }

            return offset;
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