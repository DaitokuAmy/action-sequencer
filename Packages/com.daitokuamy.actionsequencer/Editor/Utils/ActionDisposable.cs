using System;

namespace ActionSequencer.Editor.Utils {
    /// <summary>
    /// 廃棄時処理記述用
    /// </summary>
    public class ActionDisposable : IDisposable {
        private Action _action;

        public ActionDisposable(Action action) {
            _action = action;
        }

        public void Dispose() {
            _action?.Invoke();
            _action = null;
        }
    }
}