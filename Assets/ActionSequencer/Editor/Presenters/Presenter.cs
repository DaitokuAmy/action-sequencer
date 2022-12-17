using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// Presenter基底
    /// </summary>
    public abstract class Presenter<TModel, TView> : IDisposable
        where TView : VisualElement
    {
        private List<IDisposable> _disposables = new List<IDisposable>();
        
        public TModel Model { get; private set; }
        public TView View { get; private set; }
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Presenter(TModel model, TView view)
        {
            Model = model;
            View = view;
        }
        
        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public virtual void Dispose()
        {
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
