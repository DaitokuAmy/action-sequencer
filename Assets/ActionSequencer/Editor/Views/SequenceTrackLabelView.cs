using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SignalTrack用のView
    /// </summary>
    public class SequenceTrackLabelView : VisualElement
    {
        private int _lineCount = -1;
        private TextField _textFieldView;
        private List<VisualElement> _spacerViews = new List<VisualElement>();

        public event Action<string> OnChangedLabel;

        // 表示ラベル
        public string Label
        {
            get => _textFieldView.value;
            set => _textFieldView.value = value;
        }

        // 表示行数
        public int LineCount
        {
            get => _lineCount;
            set
            {
                var lineCount = Mathf.Max(1, value);
                if (lineCount == _lineCount)
                {
                    return;
                }

                _lineCount = lineCount;
                RefreshSpacers();
            }
        }
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceTrackLabelView(int lineCount) {
            AddToClassList("track_label__box");
            
            // TextField作成
            _textFieldView = new TextField();
            _textFieldView.AddToClassList("track_label__textfield");
            Add(_textFieldView);

            // 値の変化監視
            _textFieldView.RegisterValueChangedCallback(evt =>
            {
                OnChangedLabel?.Invoke(evt.newValue);
            });
            
            // 行数の設定
            LineCount = lineCount;
        }

        /// <summary>
        /// Spacerを再構築
        /// </summary>
        private void RefreshSpacers()
        {
            foreach (var spacer in _spacerViews)
            {
                Remove(spacer);
            }
            _spacerViews.Clear();

            for (var i = 1; i < LineCount; i++)
            {
                var spacer = new VisualElement();
                spacer.AddToClassList("track_label__spacer");
                Add(spacer);
                _spacerViews.Add(spacer);
            }
        } 
    }
}
