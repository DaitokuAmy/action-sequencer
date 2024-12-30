using System;
using System.Linq;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ActionSequencer.Editor.Utils {
    /// <summary>
    /// SequenceEditor用のユーティリティ
    /// </summary>
    internal static class SequenceEditorUtility {
        private static ActionSequencerSettings s_settings;
        
        /// <summary>Assets配下に存在する設定ファイルへの参照</summary>
        private static ActionSequencerSettings Settings {
            get {
                if (s_settings == null) {
                    var foundGuids = AssetDatabase.FindAssets($"t:{nameof(ActionSequencerSettings)}");
                    if (foundGuids.Length > 0) {
                        s_settings = AssetDatabase.LoadAssetAtPath<ActionSequencerSettings>(AssetDatabase.GUIDToAssetPath(foundGuids[0]));
                    }
                }

                return s_settings;
            }
        }

        /// <summary>
        /// SequenceEventの表示名を取得
        /// </summary>
        public static string GetDisplayName(Type eventType) {
            var settings = Settings;
            var displayName = "";
            if (settings != null && settings.TryGetSequenceEventTypeInfo(eventType.FullName, out var info)) {
                displayName = info.label;
            }

            return string.IsNullOrWhiteSpace(displayName) ? eventType.Name : displayName;
        }

        /// <summary>
        /// SequenceEventのテーマカラーを取得
        /// </summary>
        public static Color GetThemeColor(Type eventType) {
            var settings = Settings;
            var themeColor = Color.clear;
            if (settings != null && settings.TryGetSequenceEventTypeInfo(eventType.FullName, out var info)) {
                themeColor = info.color;
            }

            // 無ければ自動生成
            if (themeColor.a <= float.Epsilon) {
                var prevState = Random.state;
                Random.InitState(eventType.Name.GetHashCode());
                themeColor = Random.ColorHSV(0.0f, 1.0f, 0.4f, 0.4f, 0.9f, 0.9f);
                Random.state = prevState;
            }

            return themeColor;
        }

        /// <summary>
        /// 現在のルーラーメモリサイズ計算
        /// </summary>
        public static float CalcMemorySize(SequenceEditorModel editorModel) {
            var timeMode = editorModel.CurrentTimeMode.Value;
            return editorModel.TimeToSize.Value * GetThickSeconds(timeMode) / GetMemoryCycles(timeMode)[0];
        }

        /// <summary>
        /// 1 Thickで何秒を表すか取得
        /// </summary>
        public static float GetThickSeconds(SequenceEditorModel.TimeMode timeMode) {
            switch (timeMode) {
                case SequenceEditorModel.TimeMode.Seconds:
                    return 0.5f;
                case SequenceEditorModel.TimeMode.Frames30:
                    return 0.5f;
                case SequenceEditorModel.TimeMode.Frames60:
                    return 0.5f;
            }

            return 1.0f;
        }

        /// <summary>
        /// MemoryCycleの取得
        /// </summary>
        public static int[] GetMemoryCycles(SequenceEditorModel.TimeMode timeMode) {
            switch (timeMode) {
                case SequenceEditorModel.TimeMode.Seconds:
                    return new[] { 20, 10, 5, 2 };
                case SequenceEditorModel.TimeMode.Frames30:
                    return new[] { 15, 5, 3 };
                case SequenceEditorModel.TimeMode.Frames60:
                    return new[] { 30, 15, 10, 5, 2 };
            }

            return new[] { 20, 10, 5, 2 };
        }

        /// <summary>
        /// ThickCycleの取得
        /// </summary>
        public static int GetTickCycle(SequenceEditorModel.TimeMode timeMode) {
            switch (timeMode) {
                case SequenceEditorModel.TimeMode.Seconds:
                    return 20;
                case SequenceEditorModel.TimeMode.Frames30:
                    return 15;
                case SequenceEditorModel.TimeMode.Frames60:
                    return 30;
            }

            return 20;
        }

        /// <summary>
        /// シーケンスクリップ内に含まれる未使用なSubAssetをクリーンする
        /// </summary>
        public static void CleanUnusedSubAssets(SequenceClip clip) {
            if (clip == null) {
                return;
            }

            bool ContainsTrack(SequenceClip ownerClip, SequenceTrack targetTrack) {
                return ownerClip.tracks.Contains(targetTrack);
            }

            bool ContainsEvent(SequenceClip ownerClip, SequenceEvent targetEvent) {
                foreach (var track in ownerClip.tracks) {
                    if (track.sequenceEvents.Contains(targetEvent)) {
                        return true;
                    }
                }

                return false;
            }

            var path = AssetDatabase.GetAssetPath(clip);
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            var sequenceTracks = allAssets.OfType<SequenceTrack>();
            var sequenceEvents = allAssets.OfType<SequenceEvent>();
            var dirty = false;

            foreach (var sequenceTrack in sequenceTracks) {
                if (ContainsTrack(clip, sequenceTrack)) {
                    continue;
                }

                // 削除
                Object.DestroyImmediate(sequenceTrack, true);
                dirty = true;
            }

            foreach (var sequenceEvent in sequenceEvents) {
                if (ContainsEvent(clip, sequenceEvent)) {
                    continue;
                }

                // 削除
                Object.DestroyImmediate(sequenceEvent, true);
                dirty = true;
            }

            if (dirty) {
                EditorUtility.SetDirty(clip);
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
        }
    }
}