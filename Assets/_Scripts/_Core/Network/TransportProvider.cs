using Netcode.Transports;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;


namespace Assets._Scripts._Core.Network
{
    public class TransportProvider
    {
        private readonly NetworkManager _networkManager;

        public TransportKind Kind { get; private set; }
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
            Kind = kind;
            var useSteam = kind == TransportKind.Steam;

            Steam.enabled = useSteam;
            Direct.enabled = !useSteam;

            NetworkTransport transport = useSteam ? Steam : Direct;
            _networkManager.NetworkConfig.NetworkTransport = transport;
            Debug.Log($"[Transport] {kind}");
        }


        public void SetSteamPeer(ulong steamId) => Steam.ConnectToSteamID = steamId;
        public void SetDirectHost(ushort port) => Direct.SetConnectionData("0.0.0.0", port);
        public void SetDirectPeer(string ip, ushort port) => Direct.SetConnectionData(ip, port);
    }
}
