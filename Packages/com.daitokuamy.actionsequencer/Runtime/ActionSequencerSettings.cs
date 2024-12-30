using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ActionSequencer {
    /// <summary>
    /// ActionSequencer用の設定ファイル
    /// </summary>
    public class ActionSequencerSettings : ScriptableObject {
        /// <summary>
        /// SequenceEvent型用の情報
        /// </summary>
        [Serializable]
        public class SequenceEventTypeInfo {
            public string fullName = "";
            public string label = "";
            public Color color = Color.clear;
        }

        [SerializeField]
        private SequenceEventTypeInfo[] _sequenceEventTypeInfos = Array.Empty<SequenceEventTypeInfo>();

        // SequenceEvent名 > SequenceEventSetting変換
        private Dictionary<string, SequenceEventTypeInfo> _sequenceEventTypeSettingDict;
        
        /// <summary>
        /// SequenceEvent型に関する情報を取得
        /// </summary>
        public bool TryGetSequenceEventTypeInfo(string fullName, out SequenceEventTypeInfo info) {
            if (_sequenceEventTypeSettingDict == null) {
                _sequenceEventTypeSettingDict = new Dictionary<string, SequenceEventTypeInfo>();
                RefreshSettingDict();
            }

            return _sequenceEventTypeSettingDict.TryGetValue(fullName, out info);
        }

        /// <summary>
        /// 設定ファイル検索用辞書の更新
        /// </summary>
        private void RefreshSettingDict() {
            _sequenceEventTypeSettingDict.Clear();

            // 重複設定が混ざってしまっている不具合があったので、暫定的にここで除外
            _sequenceEventTypeInfos = _sequenceEventTypeInfos
                .GroupBy(x => x.fullName)
                .Select(x => x.First())
                .ToArray();

            foreach (var setting in _sequenceEventTypeInfos) {
                _sequenceEventTypeSettingDict[setting.fullName] = setting;
            }
        }

        /// <summary>
        /// 値変更時通知
        /// </summary>
        private void OnValidate() {
            _sequenceEventTypeSettingDict = null;
        }
    }
}