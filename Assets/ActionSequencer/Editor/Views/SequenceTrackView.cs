using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SignalTrack用のView
    /// </summary>
    public class SequenceTrackView : VisualElement
    {
        private int _lineCount = -1;
        private TextField _textFieldView;
        private List<VisualElement> _spacerViews = new List<VisualElement>();

        public event Action<string> OnChangedLabel; 

        public VisualElement EventBox { get; private set; }

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
        public SequenceTrackView(VisualElement eventPanel, int lineCount)
        {
            EventBox = new VisualElement();
            EventBox.AddToClassList("event_box");
            eventPanel.Add(EventBox);
            
            AddToClassList("track_box");
            
            // TextField作成
            _textFieldView = new TextField();
            _textFieldView.AddToClassList("track_textfield");
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
                spacer.AddToClassList("track_spacer");
                Add(spacer);
                _spacerViews.Add(spacer);
            }
        } 
    }
}
