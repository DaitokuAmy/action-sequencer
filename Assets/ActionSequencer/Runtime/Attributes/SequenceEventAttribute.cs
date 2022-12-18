using System;
using UnityEngine;

namespace ActionSequencer {
    /// <summary>
    /// SequenceEventにつけられるAttribute
    /// </summary>
    public class SequenceEventAttribute : Attribute {
        // Editor表示名
        public string DisplayName { get; private set; }

        // Editorテーマカラー(α:0だと自動生成)
        public Color ThemeColor { get; private set; }

        public SequenceEventAttribute(string displayName, string colorCode) {
            DisplayName = displayName;
            if (!ColorUtility.TryParseHtmlString(colorCode, out var color)) {
                color = Color.clear;
            }

            ThemeColor = color;
        }

        public SequenceEventAttribute(string displayName)
            : this(displayName, "") {
        }
    }
}