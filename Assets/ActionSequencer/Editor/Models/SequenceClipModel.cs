using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceEditor用のModel
    /// </summary>
    public class SequenceClipModel : SerializedObjectModel
    {
        private SerializedProperty _tracks;
        private List<SequenceTrackModel> _trackModels = new List<SequenceTrackModel>();
        
        public event Action<SequenceTrackModel> OnAddedTrackModel;
        public event Action<SequenceTrackModel> OnRemoveTrackModel;

        public IReadOnlyList<SequenceTrackModel> TrackModels => _trackModels;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceClipModel(SequenceClip target)
            : base(target)
        {
            _tracks = SerializedObject.FindProperty("tracks");
            
            RefreshTracks();
        }

        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            ClearTrackModels();
        }

        /// <summary>
        /// Trackの状態を再構築
        /// </summary>
        public void RefreshTracks()
        {
            ClearTrackModels();
            
            for (var i = 0; i < _tracks.arraySize; i++)
            {
                var sequenceTrack = _tracks.GetArrayElementAtIndex(i).objectReferenceValue as SequenceTrack;
                if (sequenceTrack != null)
                {
                    var model = new SequenceTrackModel(sequenceTrack);
                    _trackModels.Add(model);
                    OnAddedTrackModel?.Invoke(model);
                }
            }
        }

        /// <summary>
        /// Trackの追加
        /// </summary>
        public SequenceTrackModel AddTrack(SequenceTrack track)
        {
            SerializedObject.Update();
            _tracks.arraySize++;
            _tracks.GetArrayElementAtIndex(_tracks.arraySize - 1).objectReferenceValue = track;
            SerializedObject.ApplyModifiedProperties();
            var model = new SequenceTrackModel(track);
            _trackModels.Add(model);
            OnAddedTrackModel?.Invoke(model);
            return model;
        }
        
        /// <summary>
        /// Trackの削除
        /// </summary>
        public void RemoveTrack(SequenceTrack track)
        {
            var model = _trackModels.FirstOrDefault(x => x.Target == track);
            if (model == null)
            {
                return;
            }
            
            SerializedObject.Update();
            for (var i = _tracks.arraySize - 1; i >= 0; i--)
            {
                var element = _tracks.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != model.Target)
                {
                    continue;
                }
                
                element.DeleteArrayElementAtIndex(i);
            }
            SerializedObject.ApplyModifiedProperties();
            
            OnRemoveTrackModel?.Invoke(model);
            _trackModels.Remove(model);
        }

        /// <summary>
        /// TrackModelの削除(SerializedObjectからは消えない)
        /// </summary>
        private void ClearTrackModels()
        {
            foreach (var model in _trackModels)
            {
                OnRemoveTrackModel?.Invoke(model);
                model.Dispose();
            }
            _trackModels.Clear();
        }
    }
}