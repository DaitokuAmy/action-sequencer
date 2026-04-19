using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// RangeSequenceEvent の編集用キャッシュ
    /// </summary>
    internal sealed class RangeSequenceEventModel : SequenceEventModel {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="target">対応する RangeEvent</param>
        /// <param name="label">表示ラベル</param>
        /// <param name="active">有効状態</param>
        /// <param name="themeColor">テーマカラー</param>
        /// <param name="enterTime">開始時間</param>
        /// <param name="exitTime">終了時間</param>
        public RangeSequenceEventModel(
            RangeSequenceEvent target,
            string label,
            bool active,
            Color themeColor,
            float enterTime,
            float exitTime)
            : base(target, label, active, themeColor) {
            EnterTime = enterTime;
            ExitTime = exitTime;
        }

        /// <summary>対応する RangeEvent</summary>
        public new RangeSequenceEvent Target => (RangeSequenceEvent)base.Target;
        /// <summary>開始時間</summary>
        public float EnterTime { get; private set; }
        /// <summary>終了時間</summary>
        public float ExitTime { get; private set; }

        /// <summary>
        /// 開始時間を更新
        /// </summary>
        /// <param name="enterTime">更新後の開始時間</param>
        /// <returns>値が変化した場合は true</returns>
        public bool SetEnterTime(float enterTime) {
            var nextEnterTime = Mathf.Clamp(enterTime, 0.0f, ExitTime);
            if (Mathf.Approximately(EnterTime, nextEnterTime)) {
                return false;
            }

            EnterTime = nextEnterTime;
            return true;
        }

        /// <summary>
        /// 終了時間を更新
        /// </summary>
        /// <param name="exitTime">更新後の終了時間</param>
        /// <returns>値が変化した場合は true</returns>
        public bool SetExitTime(float exitTime) {
            var nextExitTime = Mathf.Max(exitTime, EnterTime);
            if (Mathf.Approximately(ExitTime, nextExitTime)) {
                return false;
            }

            ExitTime = nextExitTime;
            return true;
        }

        /// <summary>
        /// duration を保ちながら開始時間を移動
        /// </summary>
        /// <param name="enterTime">更新後の開始時間</param>
        /// <param name="snappedExitTime">吸着済みの終了時間</param>
        /// <returns>値が変化した場合は true</returns>
        public bool MoveEnterTime(float enterTime, float snappedExitTime) {
            var nextEnterTime = Mathf.Max(0.0f, enterTime);
            var nextExitTime = Mathf.Max(snappedExitTime, nextEnterTime);
            if (Mathf.Approximately(EnterTime, nextEnterTime) && Mathf.Approximately(ExitTime, nextExitTime)) {
                return false;
            }

            EnterTime = nextEnterTime;
            ExitTime = nextExitTime;
            return true;
        }

        /// <inheritdoc/>
        public override float GetStartTime() {
            return EnterTime;
        }

        /// <inheritdoc/>
        public override float GetEndTime() {
            return ExitTime;
        }
    }
}