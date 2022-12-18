using System;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ActionSequencer.Editor.Utils {
    /// <summary>
    /// SequenceEditor用のユーティリティ
    /// </summary>
    public static class SequenceEditorUtility {
        /// <summary>
        /// SequenceEventの表示名を取得
        /// </summary>
        public static string GetDisplayName(Type eventType) {
            var attr = eventType.GetCustomAttribute(typeof(SequenceEventAttribute)) as SequenceEventAttribute;
            var displayName = attr != null ? attr.DisplayName : "";
            return string.IsNullOrWhiteSpace(displayName) ? eventType.Name : displayName;
        }

        /// <summary>
        /// SequenceEventのテーマカラーを取得
        /// </summary>
        public static Color GetThemeColor(Type eventType) {
            // Attributeチェック
            if (eventType.GetCustomAttribute(typeof(SequenceEventAttribute)) is SequenceEventAttribute attr) {
                if (attr.ThemeColor.a > float.Epsilon) {
                    return attr.ThemeColor;
                }
            }

            // 無ければ自動生成
            var prevState = Random.state;
            Random.InitState(eventType.Name.GetHashCode());
            var themeColor = Random.ColorHSV(0.0f, 1.0f, 0.4f, 0.4f, 0.9f, 0.9f);
            Random.state = prevState;
            return themeColor;
        }

        /// <summary>
        /// 現在のルーラーメモリサイズ計算
        /// </summary>
        public static float CalcMemorySize(SequenceEditorModel editorModel) {
            var timeMode = editorModel.CurrentTimeMode.Value;
            return editorModel.TimeToSize.Value * GetThickSeconds(timeMode) / GetThickCycle(timeMode);
        }

        /// <summary>
        /// 1 Thickで何秒を表すか取得
        /// </summary>
        public static float GetThickSeconds(SequenceEditorModel.TimeMode timeMode) {
            switch (timeMode) {
                case SequenceEditorModel.TimeMode.Seconds:
                    return 0.5f;
                case SequenceEditorModel.TimeMode.Frames30:
                    return 0.5f;
                case SequenceEditorModel.TimeMode.Frames60:
                    return 0.5f;
            }

            return 1.0f;
        }

        /// <summary>
        /// ThickCycleの取得
        /// </summary>
        public static int GetThickCycle(SequenceEditorModel.TimeMode timeMode) {
            switch (timeMode) {
                case SequenceEditorModel.TimeMode.Seconds:
                    return 10;
                case SequenceEditorModel.TimeMode.Frames30:
                    return 15;
                case SequenceEditorModel.TimeMode.Frames60:
                    return 15;
            }

            return 10;
        }
    }
}