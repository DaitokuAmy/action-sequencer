using System;

namespace ActionSequencer.Editor.Utils {
    /// <summary>
    /// 通知機能付きプロパティ
    /// </summary>
    internal class ReactiveProperty<T> : IReadOnlyReactiveProperty<T> {
        private T _value;
        private bool _initialized;
        private Func<T, T> _preprocess;

        private event Action<T> OnChangedValueAction;

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
        /// コンストラクタ
        /// </summary>
        public ReactiveProperty(Func<T, T> preprocess = null) {
            _preprocess = preprocess;
        }

        /// <summary>
        /// コンストラクタ(初期値あり)
        /// </summary>
        public ReactiveProperty(T initValue, Func<T, T> preprocess = null)
            : this(preprocess) {
            _value = initValue;
            _initialized = true;
        }

        /// <summary>
        /// 実際の値
        /// </summary>
        public T Value {
            get => _initialized ? _value : default;
            set {
                if (_initialized && value.Equals(_value)) {
                    return;
                }

                _initialized = true;
                _value = _preprocess != null ? _preprocess.Invoke(value) : value;
                OnChangedValueAction?.Invoke(_value);
            }
        }

        /// <summary>
        /// 処理の監視(初期化済であれば、監視開始時に一度値が通知される)
        /// </summary>
        public IDisposable Subscribe(Action<T> func) {
            if (_initialized) {
                func.Invoke(Value);
            }

            OnChangedValueAction += func;
            return new DisposableAction(() => { OnChangedValueAction -= func; });
        }
    }

    /// <summary>
    /// 読み取り専用インターフェース
    /// </summary>
    internal interface IReadOnlyReactiveProperty<T> {
        T Value { get; }
        IDisposable Subscribe(Action<T> func);
    }
}