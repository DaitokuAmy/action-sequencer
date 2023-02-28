using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// ActionSequencer用の設定ファイル
    /// </summary>
    [FilePath("ProjectSettings/ActionSequencerSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class ActionSequencerSettings : ScriptableSingleton<ActionSequencerSettings> {
        /// <summary>
        /// SequenceEvent型用の設定
        /// </summary>
        [Serializable]
        public class SequenceEventTypeSetting {
            public string fullName;
            public string label;
            public Color color = Color.white;
        }

        [SerializeField]
        private SequenceEventTypeSetting[] _sequenceEventTypeSettings = Array.Empty<SequenceEventTypeSetting>();

        // SequenceEvent名 > SequenceEventSetting変換
        private Dictionary<string, SequenceEventTypeSetting> _sequenceEventTypeSettingDict;
        public IReadOnlyDictionary<string, SequenceEventTypeSetting> SequenceEventTypeSettings {
            get {
                if (_sequenceEventTypeSettingDict == null) {
                    _sequenceEventTypeSettingDict = new Dictionary<string, SequenceEventTypeSetting>();
                    RefreshSettingDict();
                }

                return _sequenceEventTypeSettingDict;
            }
        }

        /// <summary>
        /// 保存処理
        /// </summary>
        public void Save() {
            Save(true);
        }

        /// <summary>
        /// 設定ファイル検索用辞書の更新
        /// </summary>
        private void RefreshSettingDict() {
            _sequenceEventTypeSettingDict.Clear();

            foreach (var setting in _sequenceEventTypeSettings) {
                _sequenceEventTypeSettingDict[setting.fullName] = setting;
            }
        }

        /// <summary>
        /// アクティブ時処理
        /// </summary>
        private void OnEnable() {
            RefreshSettingDict();
        }

        /// <summary>
        /// 値変更時通知
        /// </summary>
        private void OnValidate() {
            if (_sequenceEventTypeSettingDict == null) {
                _sequenceEventTypeSettingDict = new Dictionary<string, SequenceEventTypeSetting>();
            }

            RefreshSettingDict();
        }
    }
}