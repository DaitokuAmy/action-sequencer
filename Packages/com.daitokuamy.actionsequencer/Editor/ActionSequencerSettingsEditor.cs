using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Windows;
using Color = UnityEngine.Color;

namespace ActionSequencer.Editor {
    /// <summary>
    /// ActionSequencerSettings用のエディタ拡張
    /// </summary>
    [CustomEditor(typeof(ActionSequencerSettings))]
    public class ActionSequencerSettingsEditor : UnityEditor.Editor {
        private SerializedProperty _sequenceEventTypeInfosProp;
        private ReorderableList _sequenceEventTypeInfoList;

        /// <summary>
        /// 設定ファイルの生成
        /// </summary>
        [MenuItem("Tools/Action Sequencer/Create Settings")]
        private static void CreateAsset() {
            // すでにAssets以下に存在していれば作らない
            var foundGuids = AssetDatabase.FindAssets($"t:{nameof(ActionSequencerSettings)}");
            if (foundGuids.Length > 0) {
                EditorUtility.DisplayDialog("エラー", $"既に設定ファイルが存在しています\n{AssetDatabase.GUIDToAssetPath(foundGuids[0])}", "OK");
                return;
            }

            var settings = CreateInstance<ActionSequencerSettings>();
            var directoryPath = "Assets/ActionSequencer";
            var path = $"{directoryPath}/ActionSequencerSettings.asset";
            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }

            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// インスペクタ拡張用のGUI描画
        /// </summary>
        public override void OnInspectorGUI() {
            serializedObject.Update();
            _sequenceEventTypeInfoList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// アクティブ時処理
        /// </summary>
        private void OnEnable() {
            _sequenceEventTypeInfosProp = serializedObject.FindProperty("_sequenceEventTypeInfos");
            _sequenceEventTypeInfoList = new ReorderableList(serializedObject, _sequenceEventTypeInfosProp);
            _sequenceEventTypeInfoList.displayAdd = false;
            _sequenceEventTypeInfoList.displayRemove = false;
            _sequenceEventTypeInfoList.drawHeaderCallback += rect => { EditorGUI.LabelField(rect, _sequenceEventTypeInfosProp.displayName, EditorStyles.miniLabel); };
            _sequenceEventTypeInfoList.drawElementCallback += (rect, index, active, focused) => {
                var prop = _sequenceEventTypeInfosProp.GetArrayElementAtIndex(index);
                var fullNameProp = prop.FindPropertyRelative("fullName");
                var labelProp = prop.FindPropertyRelative("label");
                var colorProp = prop.FindPropertyRelative("color");
                var r = rect;
                r.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.LabelField(r, fullNameProp.stringValue, EditorStyles.miniLabel);
                r.y += r.height;
                EditorGUI.PropertyField(r, labelProp, true);
                r.y += r.height;
                EditorGUI.PropertyField(r, colorProp, true);
            };
            _sequenceEventTypeInfoList.elementHeightCallback += index => {
                var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                return height * 3;
            };

            RefreshSettings();
        }

        /// <summary>
        /// 設定ファイルの中身をリフレッシュする
        /// </summary>
        private void RefreshSettings() {
            if (_sequenceEventTypeInfosProp == null) {
                return;
            }

            // SequenceEventTypeInfos内にある該当名の要素Indexを探す
            int FindIndex(string fullName) {
                for (var i = 0; i < _sequenceEventTypeInfosProp.arraySize; i++) {
                    var elementFullName = _sequenceEventTypeInfosProp.GetArrayElementAtIndex(i).FindPropertyRelative("fullName").stringValue;
                    if (elementFullName == fullName) {
                        return i;
                    }
                }

                return -1;
            }

            var eventTypes = TypeCache.GetTypesDerivedFrom<SignalSequenceEvent>()
                .Concat(TypeCache.GetTypesDerivedFrom<RangeSequenceEvent>())
                .Where(x => !x.IsAbstract && !x.IsGenericType)
                .OrderBy(x => x.FullName);

            // シリアライズデータの中身を実際に存在するクラス名と同期させる
            serializedObject.Update();

            var removeIndices = new List<int>();
            if (_sequenceEventTypeInfosProp.arraySize > 0) {
                removeIndices.AddRange(Enumerable.Range(0, _sequenceEventTypeInfosProp.arraySize));
            }

            foreach (var type in eventTypes) {
                var foundIndex = FindIndex(type.FullName);
                if (foundIndex >= 0) {
                    removeIndices.Remove(foundIndex);
                    continue;
                }

                _sequenceEventTypeInfosProp.arraySize++;
                var elementProp = _sequenceEventTypeInfosProp.GetArrayElementAtIndex(_sequenceEventTypeInfosProp.arraySize - 1);
                elementProp.FindPropertyRelative("fullName").stringValue = type.FullName;
                elementProp.FindPropertyRelative("label").stringValue = "";
                elementProp.FindPropertyRelative("color").colorValue = Color.clear;
            }

            // 存在しないクラス名の設定を削除
            for (var i = removeIndices.Count - 1; i >= 0; i--) {
                _sequenceEventTypeInfosProp.DeleteArrayElementAtIndex(removeIndices[i]);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}