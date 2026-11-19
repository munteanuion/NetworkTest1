using System;
using System.Collections.Generic;
using Assets._Scripts._Core.Configs;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Assets._Scripts.Gameplay
{
    public sealed class PlayerSpawner : IStartable, IDisposable
    {
        private readonly NetworkManager _networkManager;
        private readonly NetworkPrefabsConfig _prefabs;
        private readonly string _gameScene;

        private readonly List<ulong> _spawned = new();
        private bool _hooked;

        public PlayerSpawner(NetworkManager networkManager, NetworkPrefabsConfig prefabs, AppConfig config)
        {
            _networkManager = networkManager;
            _prefabs = prefabs;
            _gameScene = config.GameScene;
        }

        public void Start()
        {
            _networkManager.OnServerStarted += Hook;
            _networkManager.OnServerStopped += OnStopped;
            _networkManager.OnClientStopped += OnStopped;
        }

        public void Dispose()
        {
            _networkManager.OnServerStarted -= Hook;
            _networkManager.OnServerStopped -= OnStopped;
            _networkManager.OnClientStopped -= OnStopped;
            Unhook();
        }

        private void Hook()
        {
            if (_hooked || _networkManager.SceneManager == null) return;

            _hooked = true;
            _networkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
            _networkManager.SceneManager.OnSynchronizeComplete += OnSynchronizeComplete;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void OnStopped(bool wasClean)
        {
            Unhook();
            _spawned.Clear();
        }

        private void Unhook()
        {
            if (!_hooked) return;

            _hooked = false;

            if (_networkManager.SceneManager != null)
            {
                _networkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
                _networkManager.SceneManager.OnSynchronizeComplete -= OnSynchronizeComplete;
            }

            _networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode,
            List<ulong> completed, List<ulong> timedOut)
        {
            if (!_networkManager.IsServer || sceneName != _gameScene) return;

            foreach (var clientId in _networkManager.ConnectedClientsIds)
                SpawnFor(clientId);
        }

        private void OnSynchronizeComplete(ulong clientId)
        {
            if (!_networkManager.IsServer) return;
            if (SceneManager.GetActiveScene().name != _gameScene) return;

            SpawnFor(clientId);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            _spawned.Remove(clientId);
        }

        private void SpawnFor(ulong clientId)
        {
            if (_spawned.Contains(clientId)) return;

            if (_prefabs.Player == null)
            {
                Debug.LogError("[Spawner] Player prefab is not assigned.");
                return;
            }

            var group = UnityEngine.Object.FindAnyObjectByType<SpawnPointGroup>();
            var pose = group != null
                ? group.Next()
                : new Pose(new Vector3(0f, 1.2f, 0f), Quaternion.identity);

            var instance = UnityEngine.Object.Instantiate(_prefabs.Player, pose.position, pose.rotation);
            var networkObject = instance.GetComponent<NetworkObject>();

            networkObject.SpawnWithOwnership(clientId, true);
            _spawned.Add(clientId);
        }
    }
}