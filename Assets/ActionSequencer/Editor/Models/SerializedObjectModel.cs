using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SerializedObjectを保持するModel
    /// </summary>
    public abstract class SerializedObjectModel : Model
    {
        public Object Target { get; private set; }
        public SerializedObject SerializedObject { get; private set; }
    
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SerializedObjectModel(Object target)
        {
            Target = target;
            SerializedObject = new SerializedObject(target);
        }

        /// <summary>
        /// 対象のObjectにDirtyフラグを立てる
        /// </summary>
        protected void SetDirty()
        {
            EditorUtility.SetDirty(Target);
        }
    }
}