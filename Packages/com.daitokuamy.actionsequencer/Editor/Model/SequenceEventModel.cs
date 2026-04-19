using ActionSequencer.Editor.Utils;
using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceEvent の編集用キャッシュ
    /// </summary>
    internal abstract class SequenceEventModel {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="target">対応する Event</param>
        /// <param name="label">表示ラベル</param>
        /// <param name="active">有効状態</param>
        /// <param name="themeColor">テーマカラー</param>
        protected SequenceEventModel(SequenceEvent target, string label, bool active, Color themeColor) {
            Target = target;
            Label = label;
            Active = active;
            ThemeColor = themeColor;
        }

        /// <summary>対応する Event</summary>
        public SequenceEvent Target { get; }
        /// <summary>所属する TrackModel</summary>
        public SequenceTrackModel TrackModel { get; internal set; }
        /// <summary>表示ラベル</summary>
        public string Label { get; private set; }
        /// <summary>有効状態</summary>
        public bool Active { get; private set; }
        /// <summary>テーマカラー</summary>
        public Color ThemeColor { get; }

        /// <summary>
        /// ラベルを更新
        /// </summary>
        /// <param name="label">更新後のラベル</param>
        /// <returns>値が変化した場合は true</returns>
        public bool SetLabel(string label) {
            if (Label == label) {
                return false;
            }

            Label = label;
            return true;
        }

        /// <summary>
        /// 既定ラベルに戻す
        /// </summary>
        /// <returns>値が変化した場合は true</returns>
        public bool ResetLabel() {
            return SetLabel(SequenceEditorUtility.GetDisplayName(Target.GetType()));
        }

        /// <summary>
        /// 有効状態を更新
        /// </summary>
        /// <param name="active">更新後の状態</param>
        /// <returns>値が変化した場合は true</returns>
        public bool SetActive(bool active) {
            if (Active == active) {
                return false;
            }

            Active = active;
            return true;
        }

        /// <summary>
        /// 開始時間を取得
        /// </summary>
        /// <returns>開始時間</returns>
        public abstract float GetStartTime();
        /// <summary>
        /// 終了時間を取得
        /// </summary>
        /// <returns>終了時間</returns>
        public abstract float GetEndTime();
    }
}