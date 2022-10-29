using System;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// Model基底
    /// </summary>
    public abstract class Model : IDisposable
    {
        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}