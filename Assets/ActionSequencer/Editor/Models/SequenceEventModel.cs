using System.Reflection;
using UnityEngine;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceEvent用Model基底
    /// </summary>
    public abstract class SequenceEventModel : SerializedObjectModel
    {
        // テーマ色
        public Color ThemeColor { get; private set; }
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceEventModel(SequenceEvent target)
            : base(target) {
            SetupThemeColor();
        }

        /// <summary>
        /// テーマカラーの初期化
        /// </summary>
        private void SetupThemeColor() {
            // Attributeチェック
            if (Target.GetType().GetCustomAttribute(typeof(SequenceEventAttribute)) is SequenceEventAttribute attr) {
                if (attr.ThemeColor.a > float.Epsilon) {
                    ThemeColor = attr.ThemeColor;
                    return;
                }
            }
            
            // 無ければ自動生成
            var prevState = Random.state;
            Random.InitState(Target.GetType().Name.GetHashCode());
            ThemeColor = Random.ColorHSV(0.0f, 1.0f, 0.4f, 0.6f, 1.0f, 1.0f);
            Random.state = prevState;
        }
    }
}