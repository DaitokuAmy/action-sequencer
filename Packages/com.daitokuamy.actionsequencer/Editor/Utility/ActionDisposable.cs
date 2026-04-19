using System;

namespace ActionSequencer.Editor.Utils {
    /// <summary>
    /// 廃棄時処理記述用
    /// </summary>
    public sealed class ActionDisposable : IDisposable {
        private Action _action;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="action">廃棄時に実行する処理</param>
        public ActionDisposable(Action action) {
            _action = action;
        }

        /// <inheritdoc/>
        public void Dispose() {
            _action?.Invoke();
            _action = null;
        }
    }
}