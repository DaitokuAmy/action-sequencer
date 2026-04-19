using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SignalSequenceEvent の編集用キャッシュ
    /// </summary>
    internal sealed class SignalSequenceEventModel : SequenceEventModel {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="target">対応する SignalEvent</param>
        /// <param name="label">表示ラベル</param>
        /// <param name="active">有効状態</param>
        /// <param name="themeColor">テーマカラー</param>
        /// <param name="time">現在時間</param>
        public SignalSequenceEventModel(
            SignalSequenceEvent target,
            string label,
            bool active,
            Color themeColor,
            float time)
            : base(target, label, active, themeColor) {
            Time = time;
        }

        /// <summary>対応する SignalEvent</summary>
        public new SignalSequenceEvent Target => (SignalSequenceEvent)base.Target;
        /// <summary>現在時間</summary>
        public float Time { get; private set; }
        /// <summary>View 上の表示幅に使う duration</summary>
        public float ViewDuration => Target != null ? Target.ViewDuration : 0.0f;

        /// <summary>
        /// 時間を更新
        /// </summary>
        /// <param name="time">更新後の時間</param>
        /// <returns>値が変化した場合は true</returns>
        public bool SetTime(float time) {
            var nextTime = Mathf.Max(0.0f, time);
            if (Mathf.Approximately(Time, nextTime)) {
                return false;
            }

            Time = nextTime;
            return true;
        }

        /// <inheritdoc/>
        public override float GetStartTime() {
            return Time;
        }

        /// <inheritdoc/>
        public override float GetEndTime() {
            return Time;
        }
    }
}