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
        public TView LabelView { get; private set; }
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Presenter(TModel model, TView labelView)
        {
            Model = model;
            LabelView = labelView;
        }
        
        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}
