using System;

namespace ActionSequencer.Editor.Utils {
    /// <summary>
    /// 通知用クラス基底
    /// </summary>
    public abstract class SubjectBase {
        /// <summary>
        /// 廃棄時の処理記述用
        /// </summary>
        private class DisposableAction : IDisposable {
            private Action _action;

            public DisposableAction(Action action) {
                _action = action;
            }

            public void Dispose() {
                _action?.Invoke();
                _action = null;
            }
        }
        
        /// <summary>
        /// キャンセル用アクションの生成
        /// </summary>
        protected IDisposable CreateCancelAction(Action action) {
            return new DisposableAction(action.Invoke);
        }
    }
    
    /// <summary>
    /// 通知用クラス
    /// </summary>
    public class Subject : SubjectBase, IReadonlySubject {
        // 通知用アクション
        private event Action OnSendAction;

        /// <summary>
        /// 処理の通知
        /// </summary>
        public void Invoke() {
            OnSendAction?.Invoke();
        }

        /// <summary>
        /// 処理の監視(初期化済であれば、監視開始時に一度値が通知される)
        /// </summary>
        public IDisposable Subscribe(Action func) {
            OnSendAction += func;
            return CreateCancelAction(() => { OnSendAction -= func; });
        }
    }
    public class Subject<T> : SubjectBase, IReadonlySubject<T> {
        // 通知用アクション
        private event Action<T> OnSendAction;

        /// <summary>
        /// 処理の通知
        /// </summary>
        public void Invoke(T val) {
            OnSendAction?.Invoke(val);
        }

        /// <summary>
        /// 処理の監視(初期化済であれば、監視開始時に一度値が通知される)
        /// </summary>
        public IDisposable Subscribe(Action<T> func) {
            OnSendAction += func;
            return CreateCancelAction(() => { OnSendAction -= func; });
        }
    }
    public class Subject<T1, T2> : SubjectBase, IReadonlySubject<T1, T2> {
        // 通知用アクション
        private event Action<T1, T2> OnSendAction;

        /// <summary>
        /// 処理の通知
        /// </summary>
        public void Invoke(T1 val1, T2 val2) {
            OnSendAction?.Invoke(val1, val2);
        }

        /// <summary>
        /// 処理の監視(初期化済であれば、監視開始時に一度値が通知される)
        /// </summary>
        public IDisposable Subscribe(Action<T1, T2> func) {
            OnSendAction += func;
            return CreateCancelAction(() => { OnSendAction -= func; });
        }
    }

    /// <summary>
    /// 読み取り専用インターフェース
    /// </summary>
    public interface IReadonlySubject {
        IDisposable Subscribe(Action func);
    }
    public interface IReadonlySubject<T> {
        IDisposable Subscribe(Action<T> func);
    }
    public interface IReadonlySubject<T1, T2> {
        IDisposable Subscribe(Action<T1, T2> func);
    }
}