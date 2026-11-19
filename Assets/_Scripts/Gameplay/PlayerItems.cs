using Assets._Scripts._Core.Configs;
using Assets._Scripts._Core.Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Assets._Scripts.Gameplay
{
    public sealed class PlayerItems : NetworkBehaviourBase
    {
        private const float HoldHeight = 1.4f;
        private const float HoldForward = 0.9f;

        private ItemsConfig _items;
        private NetworkPrefabsConfig _prefabs;

        private NetworkItem _heldItemServer; 
        private NetworkItem _heldItemClient; 
        private float _nextSpawnTime;

        [Inject]
        private void Construct(ItemsConfig items, NetworkPrefabsConfig prefabs)
        {
            _items = items;
            _prefabs = prefabs;
        }

        private void Update()
        {
            if (!IsOwner || _items == null) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.eKey.wasPressedThisFrame) GrabOrDrop();
            if (keyboard.qKey.wasPressedThisFrame) Throw();
            if (keyboard.xKey.wasPressedThisFrame) Delete();
            if (keyboard.fKey.wasPressedThisFrame) SpawnItem();
        }

        private void LateUpdate()
        {
            if (!IsOwner) return;

            RefreshHeldItem();

            if (_heldItemClient == null) return;

            var holdPosition = transform.position
                               + transform.up * HoldHeight
                               + transform.forward * HoldForward;

            _heldItemClient.transform.SetPositionAndRotation(
                holdPosition,
                Quaternion.LookRotation(transform.forward, Vector3.up));
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && _heldItemServer != null)
                ReleaseOnServer(_heldItemServer, true);

            base.OnNetworkDespawn();
        }

        private void GrabOrDrop()
        {
            if (_heldItemClient != null)
            {
                DropServerRpc();
                return;
            }

            var item = FindNearestFreeItem();
            if (item != null)
                GrabServerRpc(item.NetworkObjectId);
        }

        private void Throw()
        {
            if (_heldItemClient != null)
                ThrowServerRpc(transform.forward);
        }

        private void Delete()
        {
            if (_heldItemClient != null)
                DeleteServerRpc();
        }

        private void SpawnItem()
        {
            SpawnItemServerRpc();
        }

        private NetworkItem FindNearestFreeItem()
        {
            NetworkItem best = null;
            var bestDistance = float.MaxValue;

            foreach (var networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (!networkObject.TryGetComponent<NetworkItem>(out var item)) continue;
                if (item.IsHeld) continue;

                var distance = Vector3.Distance(transform.position, item.transform.position);
                if (distance > _items.GrabRange) continue;
                if (distance >= bestDistance) continue;

                best = item;
                bestDistance = distance;
            }

            return best;
        }

        private void RefreshHeldItem()
        {
            if (_heldItemClient != null &&
                (!_heldItemClient.IsSpawned || _heldItemClient.Holder.Value != NetworkManager.LocalClientId))
                _heldItemClient = null;

            if (_heldItemClient != null) return;

            foreach (var networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (!networkObject.TryGetComponent<NetworkItem>(out var item)) continue;
                if (item.Holder.Value != NetworkManager.LocalClientId) continue;

                _heldItemClient = item;
                return;
            }
        }

        [ServerRpc]
        private void SpawnItemServerRpc(ServerRpcParams rpcParams = default)
        {
            if (_prefabs == null || _prefabs.PhysicsItem == null) return;
            if (Time.time < _nextSpawnTime) return;
            if (CountItems() >= _items.MaxItems) return;

            _nextSpawnTime = Time.time + _items.ItemSpawnCooldown;

            var position = transform.position + transform.forward * 1.5f + Vector3.up * 1.2f;
            var instance = Instantiate(_prefabs.PhysicsItem, position, Quaternion.identity);

            instance.GetComponent<NetworkObject>().Spawn();
        }

        [ServerRpc]
        private void GrabServerRpc(ulong itemId, ServerRpcParams rpcParams = default)
        {
            if (_heldItemServer != null) return;
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(itemId, out var networkObject)) return;
            if (!networkObject.TryGetComponent<NetworkItem>(out var item)) return;
            if (item.IsHeld) return;

            var senderId = rpcParams.Receive.SenderClientId;

            if (Vector3.Distance(transform.position, item.transform.position) >
                _items.GrabRange + _items.GrabTolerance)
                return;

            _heldItemServer = item;
            item.Holder.Value = senderId;
            item.SetHeld(true);
            networkObject.ChangeOwnership(senderId);
        }

        [ServerRpc]
        private void DropServerRpc(ServerRpcParams rpcParams = default)
        {
            if (_heldItemServer == null) return;

            var item = _heldItemServer;
            _heldItemServer = null;

            item.Holder.Value = NetworkItem.NoHolder;
            item.SetHeld(false);
            item.transform.position = transform.position + transform.forward * 1.2f + Vector3.up * 0.8f;
            item.Body.linearVelocity = Vector3.zero;
            item.NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
        }

        [ServerRpc]
        private void ThrowServerRpc(Vector3 direction, ServerRpcParams rpcParams = default)
        {
            if (_heldItemServer == null) return;

            var item = _heldItemServer;
            _heldItemServer = null;

            item.Holder.Value = NetworkItem.NoHolder;

            var forward = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
            item.transform.position = transform.position + transform.forward * 1.2f + Vector3.up * 1.2f;
            item.NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
            item.Throw(forward * _items.ThrowSpeed);
        }

        [ServerRpc]
        private void DeleteServerRpc(ServerRpcParams rpcParams = default)
        {
            if (_heldItemServer == null) return;

            var item = _heldItemServer;
            _heldItemServer = null;

            item.Holder.Value = NetworkItem.NoHolder;
            item.NetworkObject.Despawn(true);
        }

        private void ReleaseOnServer(NetworkItem item, bool destroy)
        {
            if (item == null || !item.IsSpawned) return;

            item.Holder.Value = NetworkItem.NoHolder;
            item.SetHeld(false);
            item.Body.linearVelocity = Vector3.zero;

            if (destroy)
                item.NetworkObject.Despawn(true);
            else
                item.NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
        }

        private int CountItems()
        {
            var count = 0;

            foreach (var networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
                if (networkObject.TryGetComponent<NetworkItem>(out _))
                    count++;

            return count;
        }
    }
}