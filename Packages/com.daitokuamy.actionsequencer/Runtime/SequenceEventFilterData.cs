using UnityEngine;

namespace ActionSequencer {
    /// <summary>
    /// SequenceEventをフィルタリングするためのデータ
    /// </summary>
    [CreateAssetMenu(fileName = "sequence_event_filter.asset", menuName = "Action Sequencer/Sequence Event Filter")]
    public sealed class SequenceEventFilterData : ScriptableObject {
        public string[] namespaceFilters;
        public string[] pathFilters;
        public string[] ignoreNamespaces;
        public string[] ignorePaths;
    }
}