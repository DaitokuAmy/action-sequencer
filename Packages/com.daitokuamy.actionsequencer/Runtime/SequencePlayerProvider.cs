using System;
using UnityEngine;

namespace ActionSequencer {
    /// <summary>
    /// SequencePlayerを提供する汎用Provider
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class SequencePlayerProvider : MonoBehaviour, ISequencePlayerProvider {
        private IReadOnlySequencePlayer _sequencePlayer;

        /// <inheritdoc />
        public IReadOnlySequencePlayer SequencePlayer => _sequencePlayer;

        /// <summary>
        /// 制御対象のSequencePlayerを設定
        /// </summary>
        /// <param name="sequencePlayer">制御対象のSequencePlayer</param>
        public void SetPlayer(IReadOnlySequencePlayer sequencePlayer) {
            _sequencePlayer = sequencePlayer;
        }

        /// <summary>
        /// GameObject上へSequencePlayerProviderを追加してSequencePlayerを設定
        /// </summary>
        /// <param name="gameObject">設定先のGameObject</param>
        /// <param name="sequencePlayer">制御対象のSequencePlayer</param>
        /// <returns>設定に利用したProvider</returns>
        public static SequencePlayerProvider AddTo(GameObject gameObject, IReadOnlySequencePlayer sequencePlayer) {
            if (gameObject == null) {
                throw new ArgumentNullException(nameof(gameObject));
            }
            if (sequencePlayer == null) {
                throw new ArgumentNullException(nameof(sequencePlayer));
            }

            if (!gameObject.TryGetComponent(out SequencePlayerProvider provider)) {
                provider = gameObject.AddComponent<SequencePlayerProvider>();
            }

            provider.SetPlayer(sequencePlayer);
            return provider;
        }

        /// <summary>
        /// Component配下のGameObject上へSequencePlayerProviderを追加してSequencePlayerを設定
        /// </summary>
        /// <param name="component">設定先を持つComponent</param>
        /// <param name="sequencePlayer">制御対象のSequencePlayer</param>
        /// <returns>設定に利用したProvider</returns>
        public static SequencePlayerProvider AddTo(Component component, IReadOnlySequencePlayer sequencePlayer) {
            if (component == null) {
                throw new ArgumentNullException(nameof(component));
            }

            return AddTo(component.gameObject, sequencePlayer);
        }
    }
}
