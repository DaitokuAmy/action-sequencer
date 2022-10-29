using System;
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
        }
    }
}
