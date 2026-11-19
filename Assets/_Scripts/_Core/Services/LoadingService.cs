using Assets._Scripts._Core.Configs;
using Assets._Scripts.UI;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Assets._Scripts._Core.Network
{
    public sealed class LoadingService : IStartable, IDisposable
    {
        private readonly NetworkManager _networkManager;
        private readonly LoadingView _view;

        private int _ready;
        private int _total;
        private bool _hooked;
        private readonly string _gameScene;
        private bool _synchronizing;


        public LoadingService(NetworkManager networkManager, LoadingView view, AppConfig config)
        {
            _networkManager = networkManager;
            _view = view;
            _gameScene = config.GameScene;
        }


        public void Start()
        {
            _view.Hide();

            _networkManager.OnServerStarted += Hook;
            _networkManager.OnClientStarted += Hook;
            _networkManager.OnServerStopped += Hide;
            _networkManager.OnClientStopped += Hide;
        }

        public void Dispose()
        {
            _networkManager.OnServerStarted -= Hook;
            _networkManager.OnClientStarted -= Hook;
            _networkManager.OnServerStopped -= Hide;
            _networkManager.OnClientStopped -= Hide;
        }

        private void Hook()
        {
            if (_hooked || _networkManager.SceneManager == null) return;

            _hooked = true;
            _networkManager.SceneManager.OnLoad += OnLoad;
            _networkManager.SceneManager.OnLoadComplete += OnLoadComplete;
            _networkManager.SceneManager.OnLoadEventCompleted += OnCompleted;

            _networkManager.SceneManager.OnSynchronize += OnSync;
            _networkManager.SceneManager.OnSynchronizeComplete += OnSyncComplete;
        }

        private void OnLoad(ulong clientId, string sceneName, LoadSceneMode mode, AsyncOperation asyncOperation)
        {
            if (_synchronizing) return;
            if (sceneName != _gameScene) return;

            if (!_view.gameObject.activeSelf)
            {
                _ready = 0;
                _total = _networkManager.IsServer ? _networkManager.ConnectedClientsIds.Count : 0;
            }

            Show();
        }

        private void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode mode)
        {
            if (sceneName != _gameScene) return;

            _ready++;
            Show();
        }

        private void OnCompleted(string sceneName, LoadSceneMode mode,
            List<ulong> completed, List<ulong> timedOut)
        {
            if (sceneName != _gameScene) return;

            _view.Hide();
        }

        private void Hide(bool wasClean) => _view.Hide();

        private void Show()
        {
            _view.Show(_total > 0 ? $"Loading... {_ready}/{_total}" : "Loading...");
        }

        private void OnSync(ulong clientId)
        {
            if (!_networkManager.IsServer)
                _synchronizing = true;
        }

        private void OnSyncComplete(ulong clientId)
        {
            if (!_networkManager.IsServer)
                _synchronizing = false;
        }
    }
}