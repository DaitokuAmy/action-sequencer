using System;
using System.Collections.Generic;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Model基底
    /// </summary>
    public abstract class Model : IDisposable {
        private List<IDisposable> _disposables = new List<IDisposable>();

        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public virtual void Dispose() {
            foreach (var disposable in _disposables) {
                disposable.Dispose();
            }

            _disposables.Clear();
        }

        /// <summary>
        /// Disposableのリストに登録
        /// </summary>
        public void AddDisposable(IDisposable disposable) {
            _disposables.Add(disposable);
        }
    }
}