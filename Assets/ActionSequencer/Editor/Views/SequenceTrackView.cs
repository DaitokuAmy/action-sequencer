using System.Collections.Generic;
using ActionSequencer.Editor.VisualElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// Track用のView
    /// </summary>
    public class SequenceTrackView : VisualElement
    {
        private List<SequenceEventView> _eventViews = new List<SequenceEventView>();
        
        public RulerView RulerView { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceTrackView() {
            AddToClassList("track__box");
            
            // Rulerを追加
            RulerView = new RulerView();
            RulerView.LineColor = new Color(1.0f, 1.0f, 1.0f, 0.05f);
            RulerView.ThickLineColor = new Color(1.0f, 1.0f, 1.0f, 0.1f);
            RulerView.LineHeightRate = 1.0f;
            RulerView.ThickLineHeightRate = 1.0f;
            Add(RulerView);
        }
    }
}
