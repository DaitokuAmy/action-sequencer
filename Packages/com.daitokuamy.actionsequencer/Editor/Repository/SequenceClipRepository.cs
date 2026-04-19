using System;
using System.Collections.Generic;
using System.Linq;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceClip の永続化境界
    /// </summary>
    internal sealed class SequenceClipRepository {
        /// <summary>
        /// プレビュー設定の保存データ
        /// </summary>
        [Serializable]
        private sealed class PreviewUserData {
            public string Guid = string.Empty;
            public long LocalId;
            public float OffsetTime;
        }

        /// <summary>
        /// Asset から ClipModel を構築
        /// </summary>
        /// <param name="clip">読み込み対象の SequenceClip</param>
        /// <returns>構築した ClipModel</returns>
        public SequenceClipModel Load(SequenceClip clip) {
            if (clip == null) {
                return null;
            }

            var trackModels = clip.tracks
                .Where(track => track != null)
                .Select((track, index) => CreateTrackModel(clip, track, index))
                .ToArray();

            return new SequenceClipModel(clip, clip.frameRate, clip.filterData, trackModels);
        }

        /// <summary>
        /// root clip と include clips を統合した ClipModel を構築
        /// </summary>
        /// <param name="rootClip">読み込み対象の root SequenceClip</param>
        /// <returns>構築した ClipModel</returns>
        public SequenceClipModel LoadComposite(SequenceClip rootClip) {
            if (rootClip == null) {
                return null;
            }

            var sections = new List<SequenceClipSectionModel>();
            var visitedClips = new HashSet<SequenceClip>();
            CollectSectionsRecursive(rootClip, sections, visitedClips);

            return new CompositeSequenceClipModel(rootClip, rootClip.frameRate, rootClip.filterData, sections);
        }

        /// <summary>
        /// 破損参照を掃除
        /// </summary>
        /// <param name="clip">対象の SequenceClip</param>
        public void CleanBrokenReferences(SequenceClip clip) {
            if (clip == null) {
                return;
            }

            var serializedObject = new SerializedObject(clip);
            serializedObject.Update();
            var tracksProperty = serializedObject.FindProperty("tracks");
            var cleanAsset = false;

            for (var trackIndex = 0; trackIndex < tracksProperty.arraySize; trackIndex++) {
                var trackProperty = tracksProperty.GetArrayElementAtIndex(trackIndex);
                if (trackProperty.objectReferenceValue == null) {
                    tracksProperty.DeleteArrayElementAtIndex(trackIndex);
                    trackIndex--;
                    cleanAsset = true;
                    continue;
                }

                var trackObject = new SerializedObject(trackProperty.objectReferenceValue);
                trackObject.Update();
                var eventsProperty = trackObject.FindProperty("sequenceEvents");
                for (var eventIndex = 0; eventIndex < eventsProperty.arraySize; eventIndex++) {
                    var eventProperty = eventsProperty.GetArrayElementAtIndex(eventIndex);
                    if (eventProperty.objectReferenceValue != null) {
                        continue;
                    }

                    eventsProperty.DeleteArrayElementAtIndex(eventIndex);
                    eventIndex--;
                    cleanAsset = true;
                }

                trackObject.ApplyModifiedPropertiesWithoutUndo();
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            if (cleanAsset) {
                RemoveMissingSubAssets(clip);
            }
        }

        /// <summary>
        /// フレームレートを永続化
        /// </summary>
        /// <param name="clip">対象の SequenceClip</param>
        /// <param name="timeMode">保存する時間表示モード</param>
        public void SetFrameRate(SequenceClip clip, SequenceEditorModel.TimeMode timeMode) {
            if (clip == null) {
                return;
            }

            Undo.RecordObject(clip, "Change Frame Rate");
            clip.frameRate = timeMode switch {
                SequenceEditorModel.TimeMode.Seconds => -1,
                SequenceEditorModel.TimeMode.Frames30 => 30,
                SequenceEditorModel.TimeMode.Frames60 => 60,
                _ => clip.frameRate,
            };
            EditorUtility.SetDirty(clip);
        }

        /// <summary>
        /// プレビュー設定を importer userData へ保存
        /// </summary>
        /// <param name="sequenceClip">保存先の SequenceClip</param>
        /// <param name="animationClip">保存する AnimationClip</param>
        /// <param name="offsetTime">保存するオフセット時間</param>
        public void SavePreviewData(SequenceClip sequenceClip, AnimationClip animationClip, float offsetTime) {
            if (sequenceClip == null) {
                return;
            }

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sequenceClip));
            if (importer == null) {
                return;
            }

            var userData = new PreviewUserData {
                OffsetTime = offsetTime,
            };

            if (animationClip != null &&
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(animationClip, out var guid, out long localId)) {
                userData.Guid = guid;
                userData.LocalId = localId;
            }

            importer.userData = JsonUtility.ToJson(userData, true);
            importer.SaveAndReimport();
        }

        /// <summary>
        /// importer userData からプレビュー設定を読み込む
        /// </summary>
        /// <param name="sequenceClip">読み込み元の SequenceClip</param>
        /// <returns>保存されている AnimationClip とオフセット時間</returns>
        public (AnimationClip, float) LoadPreviewData(SequenceClip sequenceClip) {
            if (sequenceClip == null) {
                return (null, 0.0f);
            }

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sequenceClip));
            if (importer == null || string.IsNullOrEmpty(importer.userData)) {
                return (null, 0.0f);
            }

            if (!TryLoadPreviewUserData(importer.userData, out var userData)) {
                SavePreviewData(sequenceClip, null, 0.0f);
                return (null, 0.0f);
            }

            if (string.IsNullOrEmpty(userData.Guid)) {
                return (null, userData.OffsetTime);
            }

            var targetAnimationClip = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(userData.Guid))
                .FirstOrDefault(asset => {
                    return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out _, out long localId) &&
                           localId == userData.LocalId;
                }) as AnimationClip;
            return (targetAnimationClip, userData.OffsetTime);
        }

        /// <summary>
        /// Track を作成
        /// </summary>
        /// <param name="clip">作成先の SequenceClip</param>
        /// <returns>作成した Track</returns>
        public SequenceTrack CreateTrack(SequenceClip clip) {
            if (clip == null) {
                return null;
            }

            var track = ScriptableObject.CreateInstance<SequenceTrack>();
            track.name = nameof(SequenceTrack);
            track.label = "Track";
            AssetDatabase.AddObjectToAsset(track, clip);
            Undo.RegisterCreatedObjectUndo(track, "Create Track");

            Undo.RecordObject(clip, "Create Track");
            var tracks = new List<SequenceTrack>(clip.tracks) { track };
            clip.tracks = tracks.ToArray();
            EditorUtility.SetDirty(clip);
            return track;
        }

        /// <summary>
        /// Track を削除
        /// </summary>
        /// <param name="clip">対象の SequenceClip</param>
        /// <param name="track">削除対象の Track</param>
        public void DeleteTrack(SequenceClip clip, SequenceTrack track) {
            if (clip == null || track == null) {
                return;
            }

            var trackIndex = Array.IndexOf(clip.tracks, track);
            if (trackIndex < 0) {
                return;
            }

            foreach (var sequenceEvent in track.sequenceEvents.Where(x => x != null).ToArray()) {
                DeleteEvent(track, sequenceEvent);
            }

            Undo.RecordObject(clip, "Delete Track");
            var tracks = new List<SequenceTrack>(clip.tracks);
            tracks.RemoveAt(trackIndex);
            clip.tracks = tracks.ToArray();
            EditorUtility.SetDirty(clip);

            Undo.DestroyObjectImmediate(track);
        }

        /// <summary>
        /// Track を並び替え
        /// </summary>
        /// <param name="clip">対象の SequenceClip</param>
        /// <param name="track">移動対象の Track</param>
        /// <param name="targetIndex">移動先 index</param>
        public void MoveTrack(SequenceClip clip, SequenceTrack track, int targetIndex) {
            if (clip == null || track == null) {
                return;
            }

            var tracks = new List<SequenceTrack>(clip.tracks);
            var currentIndex = tracks.IndexOf(track);
            if (currentIndex < 0 || targetIndex < 0 || targetIndex >= tracks.Count) {
                return;
            }

            Undo.RecordObject(clip, "Move Track");
            tracks.RemoveAt(currentIndex);
            tracks.Insert(targetIndex, track);
            clip.tracks = tracks.ToArray();
            EditorUtility.SetDirty(clip);
        }

        /// <summary>
        /// Track ラベルを変更
        /// </summary>
        /// <param name="track">変更対象の Track</param>
        /// <param name="label">変更後のラベル</param>
        public void RenameTrack(SequenceTrack track, string label) {
            if (track == null) {
                return;
            }

            Undo.RecordObject(track, "Rename Track");
            track.label = label;
            EditorUtility.SetDirty(track);
        }

        /// <summary>
        /// Event を作成
        /// </summary>
        /// <param name="track">作成先の Track</param>
        /// <param name="eventType">作成する Event 型</param>
        /// <returns>作成した Event</returns>
        public SequenceEvent CreateEvent(SequenceTrack track, Type eventType) {
            if (track == null || eventType == null || !eventType.IsSubclassOf(typeof(SequenceEvent))) {
                return null;
            }

            var sequenceEvent = ScriptableObject.CreateInstance(eventType) as SequenceEvent;
            if (sequenceEvent == null) {
                return null;
            }

            sequenceEvent.name = eventType.Name;
            sequenceEvent.label = string.Empty;
            AssetDatabase.AddObjectToAsset(sequenceEvent, track);
            Undo.RegisterCreatedObjectUndo(sequenceEvent, "Create Event");

            Undo.RecordObject(track, "Create Event");
            var events = new List<SequenceEvent>(track.sequenceEvents) { sequenceEvent };
            track.sequenceEvents = events.ToArray();
            EditorUtility.SetDirty(track);
            return sequenceEvent;
        }

        /// <summary>
        /// Event を複製
        /// </summary>
        /// <param name="track">複製先の Track</param>
        /// <param name="sourceEvent">複製元の Event</param>
        /// <returns>複製した Event</returns>
        public SequenceEvent DuplicateEvent(SequenceTrack track, SequenceEvent sourceEvent) {
            return DuplicateEvents(track, new[] { sourceEvent }).FirstOrDefault();
        }

        /// <summary>
        /// Event 群を複製
        /// </summary>
        /// <param name="track">複製先の Track</param>
        /// <param name="sourceEvents">複製元の Event 一覧</param>
        /// <returns>複製した Event 一覧</returns>
        public SequenceEvent[] DuplicateEvents(SequenceTrack track, IReadOnlyList<SequenceEvent> sourceEvents) {
            if (track == null || sourceEvents == null || sourceEvents.Count == 0) {
                return Array.Empty<SequenceEvent>();
            }

            var currentEvents = new List<SequenceEvent>(track.sequenceEvents);
            var sourceEventSet = sourceEvents
                .Where(x => x != null)
                .ToHashSet();
            var orderedSourceEvents = currentEvents
                .Where(sourceEventSet.Contains)
                .ToArray();
            if (orderedSourceEvents.Length == 0) {
                return Array.Empty<SequenceEvent>();
            }

            var insertIndex = currentEvents.FindLastIndex(sourceEventSet.Contains) + 1;
            var groupId = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();

            var duplicatedEvents = new List<SequenceEvent>(orderedSourceEvents.Length);
            foreach (var sourceEvent in orderedSourceEvents) {
                var clonedEvent = Object.Instantiate(sourceEvent);
                clonedEvent.name = sourceEvent.name;
                AssetDatabase.AddObjectToAsset(clonedEvent, track);
                Undo.RegisterCreatedObjectUndo(clonedEvent, "Duplicate Event");
                currentEvents.Insert(insertIndex, clonedEvent);
                duplicatedEvents.Add(clonedEvent);
                insertIndex++;
            }

            Undo.RecordObject(track, "Duplicate Event");
            track.sequenceEvents = currentEvents.ToArray();
            EditorUtility.SetDirty(track);

            Undo.CollapseUndoOperations(groupId);
            return duplicatedEvents.ToArray();
        }

        /// <summary>
        /// Event を削除
        /// </summary>
        /// <param name="track">対象の Track</param>
        /// <param name="sequenceEvent">削除対象の Event</param>
        public void DeleteEvent(SequenceTrack track, SequenceEvent sequenceEvent) {
            DeleteEvents(new[] { sequenceEvent });
        }

        /// <summary>
        /// Event 群を削除
        /// </summary>
        /// <param name="sequenceEvents">削除対象の Event 一覧</param>
        public void DeleteEvents(IReadOnlyList<SequenceEvent> sequenceEvents) {
            if (sequenceEvents == null || sequenceEvents.Count == 0) {
                return;
            }

            var targetEvents = sequenceEvents
                .Where(x => x != null)
                .Select(sequenceEvent => new {
                    SequenceEvent = sequenceEvent,
                    Track = FindTrack(sequenceEvent),
                })
                .Where(x => x.Track != null)
                .ToArray();
            if (targetEvents.Length == 0) {
                return;
            }

            var groupId = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();

            foreach (var groupedEvents in targetEvents.GroupBy(x => x.Track)) {
                var track = groupedEvents.Key;
                var events = new List<SequenceEvent>(track.sequenceEvents);
                if (!groupedEvents.Any(x => events.Contains(x.SequenceEvent))) {
                    continue;
                }

                Undo.RecordObject(track, "Delete Event");
                events.RemoveAll(sequenceEvent => groupedEvents.Any(x => x.SequenceEvent == sequenceEvent));
                track.sequenceEvents = events.ToArray();
                EditorUtility.SetDirty(track);
            }

            foreach (var targetEvent in targetEvents) {
                Undo.DestroyObjectImmediate(targetEvent.SequenceEvent);
            }

            Undo.CollapseUndoOperations(groupId);
        }

        /// <summary>
        /// Event を保持している Track を検索
        /// </summary>
        /// <param name="sequenceEvent">検索対象の Event</param>
        /// <returns>保持している Track</returns>
        private SequenceTrack FindTrack(SequenceEvent sequenceEvent) {
            var assetPath = AssetDatabase.GetAssetPath(sequenceEvent);
            if (string.IsNullOrEmpty(assetPath)) {
                return null;
            }

            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<SequenceTrack>()
                .FirstOrDefault(track => track.sequenceEvents.Contains(sequenceEvent));
        }

        /// <summary>
        /// Event を別 Track へ移動
        /// </summary>
        /// <param name="sourceTrack">移動元の Track</param>
        /// <param name="targetTrack">移動先の Track</param>
        /// <param name="sequenceEvent">移動対象の Event</param>
        /// <param name="targetIndex">移動先 index</param>
        public void MoveEvent(SequenceTrack sourceTrack, SequenceTrack targetTrack, SequenceEvent sequenceEvent, int targetIndex) {
            if (sourceTrack == null || targetTrack == null || sequenceEvent == null) {
                return;
            }

            if (sourceTrack == targetTrack) {
                var currentIndex = Array.IndexOf(sourceTrack.sequenceEvents, sequenceEvent);
                if (currentIndex < 0) {
                    return;
                }

                var clampedTargetIndex = Mathf.Clamp(targetIndex, 0, sourceTrack.sequenceEvents.Length - 1);
                if (currentIndex == clampedTargetIndex) {
                    return;
                }

                Undo.RecordObject(sourceTrack, "Move Event");
                var serializedObject = new SerializedObject(sourceTrack);
                serializedObject.Update();

                var eventsProperty = serializedObject.FindProperty("sequenceEvents");
                eventsProperty.MoveArrayElement(currentIndex, clampedTargetIndex);

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(sourceTrack);
                return;
            }

            var sourceEvents = new List<SequenceEvent>(sourceTrack.sequenceEvents);
            if (!sourceEvents.Remove(sequenceEvent)) {
                return;
            }

            var targetEvents = new List<SequenceEvent>(targetTrack.sequenceEvents);
            var insertIndex = Mathf.Clamp(targetIndex, 0, targetEvents.Count);

            Undo.RecordObject(sourceTrack, "Move Event");
            Undo.RecordObject(targetTrack, "Move Event");

            targetEvents.Insert(insertIndex, sequenceEvent);
            sourceTrack.sequenceEvents = sourceEvents.ToArray();
            targetTrack.sequenceEvents = targetEvents.ToArray();
            EditorUtility.SetDirty(sourceTrack);
            EditorUtility.SetDirty(targetTrack);
        }

        /// <summary>
        /// Event を同一 Track 内で移動
        /// </summary>
        /// <param name="track">対象の Track</param>
        /// <param name="sequenceEvent">移動対象の Event</param>
        /// <param name="targetIndex">移動先 index</param>
        public void MoveEvent(SequenceTrack track, SequenceEvent sequenceEvent, int targetIndex) {
            MoveEvent(track, track, sequenceEvent, targetIndex);
        }

        /// <summary>
        /// Event 群を貼り付け
        /// </summary>
        /// <param name="track">貼り付け先の Track</param>
        /// <param name="json">コピー済み JSON</param>
        /// <returns>貼り付けた Event 一覧</returns>
        public SequenceEvent[] PasteEvents(SequenceTrack track, string json) {
            if (track == null || string.IsNullOrEmpty(json)) {
                return Array.Empty<SequenceEvent>();
            }

            var copyData = new CopyData();
            JsonUtility.FromJsonOverwrite(json, copyData);
            if (copyData.SequenceEvents == null || copyData.SequenceEvents.Length == 0) {
                return Array.Empty<SequenceEvent>();
            }

            var groupId = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();

            var pastedEvents = new List<SequenceEvent>();
            var currentEvents = new List<SequenceEvent>(track.sequenceEvents);
            foreach (var sourceEvent in copyData.SequenceEvents.Where(x => x != null)) {
                var pastedEvent = Object.Instantiate(sourceEvent);
                pastedEvent.name = sourceEvent.name;
                AssetDatabase.AddObjectToAsset(pastedEvent, track);
                Undo.RegisterCreatedObjectUndo(pastedEvent, "Paste Event");
                currentEvents.Add(pastedEvent);
                pastedEvents.Add(pastedEvent);
            }

            Undo.RecordObject(track, "Paste Event");
            track.sequenceEvents = currentEvents.ToArray();
            EditorUtility.SetDirty(track);

            Undo.CollapseUndoOperations(groupId);
            return pastedEvents.ToArray();
        }

        /// <summary>
        /// Event ラベルを変更
        /// </summary>
        /// <param name="sequenceEvent">変更対象の Event</param>
        /// <param name="label">変更後のラベル</param>
        public void RenameEvent(SequenceEvent sequenceEvent, string label) {
            if (sequenceEvent == null) {
                return;
            }

            Undo.RecordObject(sequenceEvent, "Rename Event");
            sequenceEvent.label = label;
            EditorUtility.SetDirty(sequenceEvent);
        }

        /// <summary>
        /// Event の有効状態を変更
        /// </summary>
        /// <param name="sequenceEvent">変更対象の Event</param>
        /// <param name="active">変更後の状態</param>
        public void SetEventActive(SequenceEvent sequenceEvent, bool active) {
            if (sequenceEvent == null) {
                return;
            }

            Undo.RecordObject(sequenceEvent, "Toggle Event");
            sequenceEvent.active = active;
            EditorUtility.SetDirty(sequenceEvent);
        }

        /// <summary>
        /// SignalEvent の時間を変更
        /// </summary>
        /// <param name="sequenceEvent">変更対象の Event</param>
        /// <param name="time">変更後の時間</param>
        public void SetSignalTime(SignalSequenceEvent sequenceEvent, float time) {
            if (sequenceEvent == null) {
                return;
            }

            Undo.RecordObject(sequenceEvent, "Move Event");
            sequenceEvent.time = Mathf.Max(0.0f, time);
            EditorUtility.SetDirty(sequenceEvent);
        }

        /// <summary>
        /// RangeEvent の時間範囲を変更
        /// </summary>
        /// <param name="sequenceEvent">変更対象の Event</param>
        /// <param name="enterTime">開始時間</param>
        /// <param name="exitTime">終了時間</param>
        public void SetRangeTimes(RangeSequenceEvent sequenceEvent, float enterTime, float exitTime) {
            if (sequenceEvent == null) {
                return;
            }

            Undo.RecordObject(sequenceEvent, "Resize Event");
            sequenceEvent.enterTime = Mathf.Clamp(enterTime, 0.0f, exitTime);
            sequenceEvent.exitTime = Mathf.Max(exitTime, sequenceEvent.enterTime);
            EditorUtility.SetDirty(sequenceEvent);
        }

        /// <summary>
        /// Clip からセクション用モデルを生成
        /// </summary>
        /// <param name="clip">変換元の Clip</param>
        /// <returns>生成した SectionModel</returns>
        private SequenceClipSectionModel CreateSectionModel(SequenceClip clip) {
            var trackModels = clip.tracks
                .Where(track => track != null)
                .Select((track, index) => CreateTrackModel(clip, track, index))
                .ToArray();
            return new SequenceClipSectionModel(clip, clip.name, trackModels);
        }

        /// <summary>
        /// Clip と include clips を再帰的に section 一覧へ追加する
        /// </summary>
        /// <param name="clip">追加対象の Clip</param>
        /// <param name="sections">追加先の section 一覧</param>
        /// <param name="visitedClips">追加済み Clip 集合</param>
        private void CollectSectionsRecursive(
            SequenceClip clip,
            List<SequenceClipSectionModel> sections,
            HashSet<SequenceClip> visitedClips) {
            if (clip == null || !visitedClips.Add(clip)) {
                return;
            }

            sections.Add(CreateSectionModel(clip));

            foreach (var includeClip in clip.includeClips.Where(x => x != null)) {
                CollectSectionsRecursive(includeClip, sections, visitedClips);
            }
        }

        /// <summary>
        /// Track から編集用モデルを生成
        /// </summary>
        /// <param name="ownerClip">所属する SequenceClip</param>
        /// <param name="track">変換元の Track</param>
        /// <param name="ownerTrackIndex">所属 clip 内での index</param>
        /// <returns>生成した TrackModel</returns>
        private SequenceTrackModel CreateTrackModel(SequenceClip ownerClip, SequenceTrack track, int ownerTrackIndex) {
            var eventModels = track.sequenceEvents
                .Where(sequenceEvent => sequenceEvent != null)
                .Select(sequenceEvent => CreateEventModel(sequenceEvent))
                .ToArray();
            return new SequenceTrackModel(ownerClip, track, ownerTrackIndex, track.label, eventModels);
        }

        /// <summary>
        /// Event から編集用モデルを生成
        /// </summary>
        /// <param name="sequenceEvent">変換元の Event</param>
        /// <returns>生成した EventModel</returns>
        private SequenceEventModel CreateEventModel(SequenceEvent sequenceEvent) {
            var themeColor = SequenceEditorUtility.GetThemeColor(sequenceEvent.GetType());
            return sequenceEvent switch {
                SignalSequenceEvent signalEvent => new SignalSequenceEventModel(
                    signalEvent,
                    signalEvent.label,
                    signalEvent.active,
                    themeColor,
                    signalEvent.time),
                RangeSequenceEvent rangeEvent => new RangeSequenceEventModel(
                    rangeEvent,
                    rangeEvent.label,
                    rangeEvent.active,
                    themeColor,
                    rangeEvent.enterTime,
                    rangeEvent.exitTime),
                _ => throw new NotSupportedException(sequenceEvent.GetType().FullName),
            };
        }

        /// <summary>
        /// 文字列化されたプレビュー設定を読み込む
        /// </summary>
        /// <param name="json">読み込み対象の JSON</param>
        /// <param name="userData">読み込んだプレビュー設定</param>
        /// <returns>読み込みに成功した場合は true</returns>
        private bool TryLoadPreviewUserData(string json, out PreviewUserData userData) {
            userData = new PreviewUserData();

            try {
                JsonUtility.FromJsonOverwrite(json, userData);
                return true;
            }
            catch {
                userData = null;
                return false;
            }
        }

        /// <summary>
        /// 参照切れ sub asset を一度複製経由で除去
        /// </summary>
        /// <param name="targetAsset">クリーンアップ対象の asset</param>
        private void RemoveMissingSubAssets(UnityEngine.Object targetAsset) {
            var targetName = targetAsset.name;
            var newInstance = ScriptableObject.CreateInstance(targetAsset.GetType());
            EditorUtility.CopySerialized(targetAsset, newInstance);

            var oldPath = AssetDatabase.GetAssetPath(targetAsset);
            var newPath = oldPath.Replace(".asset", "CLONE.asset");
            AssetDatabase.CreateAsset(newInstance, newPath);
            AssetDatabase.ImportAsset(newPath);

            var assets = AssetDatabase.LoadAllAssetsAtPath(oldPath);
            foreach (var asset in assets) {
                if (asset == null || asset == targetAsset) {
                    continue;
                }

                AssetDatabase.RemoveObjectFromAsset(asset);
                AssetDatabase.AddObjectToAsset(asset, newInstance);
            }

            newInstance.name = targetName;

            EditorUtility.SetDirty(newInstance);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(oldPath);
            AssetDatabase.ImportAsset(newPath);

            var directoryName = System.IO.Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            var globalOldPath = System.IO.Path.Combine(directoryName, oldPath);
            var globalNewPath = System.IO.Path.Combine(directoryName, newPath);

            System.IO.File.Delete(globalOldPath);
            System.IO.File.Delete(globalNewPath + ".meta");
            System.IO.File.Move(globalNewPath, globalOldPath);

            AssetDatabase.Refresh();
        }
    }
}
