using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ActionSequencer
{
    /// <summary>
    /// SequenceEventにつけられるAttribute
    /// </summary>
    public class SequenceEventAttribute : Attribute
    {
        // Editor表示名
        public string DisplayName { get; private set; }
        // Editorテーマカラー(α:0だと自動生成)
        public Color ThemeColor { get; private set; }
        
        public SequenceEventAttribute(string displayName, Color themeColor)
        {
            DisplayName = displayName;
            ThemeColor = themeColor;
        }
        public SequenceEventAttribute(string displayName)
            : this(displayName, UnityEngine.Color.clear)
        {
        }
    }
}
