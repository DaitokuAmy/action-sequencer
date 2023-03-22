using System;
using System.Collections.Generic;
using System.Linq;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// ProjectSettings拡張用クラス
    /// </summary>
    public class ActionSequencerSettingsProvider : SettingsProvider {
        private const string SettingsPath = "Project/Action Sequencer";

        private ActionSequencerSettings _settings;
        private SerializedObject _serializedObject;

        /// <summary>
        /// Providerの作成
        /// </summary>
        [SettingsProvider]
        private static SettingsProvider CreateProvider() {
            return new ActionSequencerSettingsProvider(SettingsPath, SettingsScope.Project);
        }

        /// <summary>
        /// アクティブ化
        /// </summary>
        public override void OnActivate(string searchContext, VisualElement rootElement) {
            _settings = ActionSequencerSettings.instance;
            _settings.hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable;
            _serializedObject = new SerializedObject(_settings);
        }

        /// <summary>
        /// GUI描画
        /// </summary>
        public override void OnGUI(string searchContext) {
            _serializedObject.Update();

            using (var changeCheckScope = new EditorGUI.ChangeCheckScope()) {
                // SequenceEventのカスタム設定
                DrawSequenceEventSettings();

                if (changeCheckScope.changed) {
                    ActionSequencerSettings.instance.Save();
                }
            }

            _serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// SequenceEvent設定の描画
        /// </summary>
        private void DrawSequenceEventSettings() {
            var eventTypes = TypeCache.GetTypesDerivedFrom<SignalSequenceEvent>()
                .Concat(TypeCache.GetTypesDerivedFrom<RangeSequenceEvent>())
                .Where(x => !x.IsAbstract && !x.IsGenericType)
                .OrderBy(x => x.FullName);
            var settingsProp = _serializedObject.FindProperty("_sequenceEventTypeSettings");

            // 設定の変更
            void ChangeSetting(Type type, string typeLabel, Color typeColor) {
                var fullName = type.FullName;

                // 該当のFullNameを持った設定を探す
                var foundIndex = -1;
                for (var i = 0; i < settingsProp.arraySize; i++) {
                    if (settingsProp.GetArrayElementAtIndex(i).FindPropertyRelative("fullName").stringValue != fullName) {
                        continue;
                    }

                    foundIndex = i;
                    break;
                }

                // 無ければ追加
                if (foundIndex < 0) {
                    settingsProp.arraySize++;
                    foundIndex = settingsProp.arraySize - 1;
                }

                // 設定の変更
                var elementProp = settingsProp.GetArrayElementAtIndex(foundIndex);
                elementProp.FindPropertyRelative("fullName").stringValue = fullName;
                elementProp.FindPropertyRelative("label").stringValue = typeLabel;
                elementProp.FindPropertyRelative("color").colorValue = typeColor;
            }

            // EventType毎の描画
            EditorGUILayout.LabelField("Sequence Event Type Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            foreach (var type in eventTypes) {
                var fullName = type.FullName;
                
                // 既存設定の取り出し
                _settings.SequenceEventTypeSettings.TryGetValue(fullName ?? "", out var sequenceEventSetting);

                // 設定部分描画
                using (var changeCheckScope = new EditorGUI.ChangeCheckScope()) {
                    EditorGUILayout.LabelField(type.Name, EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    var newLabel = EditorGUILayout.TextField("Label", sequenceEventSetting?.label ?? type.Name);
                    var newColor = EditorGUILayout.ColorField("Color", sequenceEventSetting?.color ?? Color.clear);
                    
                    if (changeCheckScope.changed) {
                        // 設定を更新
                        ChangeSetting(type, newLabel, newColor);
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        private ActionSequencerSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords) {
        }
    }
}