using Assets._Scripts._Core.Network;
using Unity.Netcode;
using UnityEngine;

namespace Assets._Scripts.Gameplay
{
    public sealed class NetworkItem : NetworkBehaviourBase
    {
        public const ulong NoHolder = ulong.MaxValue;

        public readonly NetworkVariable<ulong> Holder = new(
            NoHolder,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        [SerializeField] private Rigidbody _rigidbody;

        public Rigidbody Body => _rigidbody;
        public bool IsHeld => Holder.Value != NoHolder;

        protected override void OnSpawned()
        {
            Holder.OnValueChanged += OnHolderChanged;
            RefreshPhysics();
        }

        public override void OnNetworkDespawn()
        {
            Holder.OnValueChanged -= OnHolderChanged;
            base.OnNetworkDespawn();
        }

        public override void OnGainedOwnership() => RefreshPhysics();
        public override void OnLostOwnership() => RefreshPhysics();

        private void OnHolderChanged(ulong previous, ulong current) => RefreshPhysics();

        private void RefreshPhysics()
        {
            if (_rigidbody == null) return;

            var simulate = IsOwner && !IsHeld;

            _rigidbody.isKinematic = !simulate;
            _rigidbody.interpolation = simulate
                ? RigidbodyInterpolation.Interpolate
                : RigidbodyInterpolation.None;
        }

        public void Throw(Vector3 velocity)
        {
            if (_rigidbody == null) return;

            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }
}
