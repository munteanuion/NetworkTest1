using Assets._Scripts._Core;
using Assets._Scripts._Core.Configs;
using Assets._Scripts._Core.Network;
using Assets._Scripts._Core.Services;
using Assets._Scripts._DI_Container;
using Assets._Scripts.UI;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameScope : LifetimeScope
{
    [SerializeField] private AppConfig _appConfig;
    [SerializeField] private NetworkManager _networkManagerPrefab;
    [SerializeField] private LoadingView _loadingView;
    private IObjectResolver _objectResolver;


    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterEntryPoint<GameBootstrapper>();
        builder.RegisterEntryPoint<LoadingService>();

        NetworkManager networkManagerInstance = SpawnNetworkManager();

        builder.RegisterInstance(_appConfig);
        builder.RegisterInstance(networkManagerInstance);
        builder.RegisterInstance(_loadingView);

        builder.Register<TransportProvider>(Lifetime.Singleton);
        builder.Register<SteamSessionService>(Lifetime.Singleton);

        builder.RegisterBuildCallback(resolver =>
        {
            _objectResolver = resolver;
            NetworkDiResolver.Initialize(_objectResolver);
        });
    }


    private NetworkManager SpawnNetworkManager()
    {
        var networkManagerInstance = Instantiate(_networkManagerPrefab);
        DontDestroyOnLoad(networkManagerInstance);

        if (networkManagerInstance.gameObject.activeSelf) 
            Debug.LogError("Prefab Object Need to be disabled");

        return networkManagerInstance;
    }
}
