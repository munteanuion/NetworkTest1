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

        public void SetHeld(bool held)
        {
            if (_rigidbody == null) return;

            _rigidbody.isKinematic = held;
            _rigidbody.interpolation = held ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
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