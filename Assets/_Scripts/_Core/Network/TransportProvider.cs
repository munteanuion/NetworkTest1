using Netcode.Transports;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;


namespace Assets._Scripts._Core.Network
{
    public class TransportProvider
    {
        private readonly NetworkManager _networkManager;

        public SteamNetworkingSocketsTransport Steam { get; }
        public UnityTransport Direct { get; }


        public TransportProvider(NetworkManager networkManager)
        {
            _networkManager = networkManager;
            Steam = networkManager.GetComponent<SteamNetworkingSocketsTransport>();
            Direct = networkManager.GetComponent<UnityTransport>();
        }


        public void Select(TransportKind kind)
        {
            var useSteam = kind == TransportKind.Steam;

            Steam.enabled = useSteam;
            Direct.enabled = !useSteam;

            NetworkTransport transport = useSteam ? Steam : Direct;
            _networkManager.NetworkConfig.NetworkTransport = transport;
            Debug.Log($"[Transport] {kind}");
        }


        public void SetSteamPeer(ulong steamId) => Steam.ConnectToSteamID = steamId;
    }
}
