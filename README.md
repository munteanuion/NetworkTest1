# Coop Physics Prototype

2–4 player co-op prototype: Steam lobby, networked scene load, owner-authoritative
players and host-authoritative physics items.

Unity **6000.6.9f1** · Netcode for GameObjects **2.14.0** · Steamworks.NET **20.0.0** ·
SteamNetworkingSockets transport (community package) · VContainer **1.19.0** ·
Input System **1.20.1** · AppID **480 (Spacewar)**.

## Run

**Editor:** open `Assets/_Scenes/MainMenu.unity`, press Play, **Host**.
Steam must be running; `steam_appid.txt` (`480`) is in the project root.

**Build (two PCs, two Steam accounts):**
1. Build Windows x64.
2. Next to the exe: `steam_appid.txt` (480). Unity copies `steam_api64.dll` into
   `Game_Data/Plugins/x86_64/`.
3. Host on PC A (**Host**). On PC B join by:
   - lobby ID (**Copy lobby ID** → paste → **Join**),
   - Steam friends list → right click host → **Join Game** (Rich Presence),
   - Steam overlay invite, when the overlay is available.

Joining with the game already running uses `GameRichPresenceJoinRequested_t`.

### Command line

| Argument | Description |
|---|---|
| `+connect_lobby <id>` | Auto-join a Steam lobby (sent by Steam on invite/Join Game). |
| `-host` | Create a lobby and host immediately. |

### Controls

| Input | Action |
|---|---|
| `WASD` / `Shift` / `Space` | Move / sprint / jump |
| Mouse | Look |
| `F` | Spawn item |
| `E` | Grab / drop |
| `Q` | Throw |
| `X` | Delete held item |
| `Esc` | Leave session, return to menu |

Lobby: Host, Join, Copy lobby ID, Invite friends, Start game (host), Leave.

## Architecture

```
_Scripts/_Core/Configs     AppConfig, CharacterConfig, ItemsConfig, NetworkPrefabsConfig
_Scripts/_Core/Network     TransportProvider, NetworkDiResolver, NetworkBehaviourBase
_Scripts/_Core/Services    SteamSessionService, LoadingService
_Scripts/_DI_Container     GameScope (root LifetimeScope), GameBootstrapper
_Scripts/Gameplay          PlayerController, PlayerSpawner, PlayerItems, NetworkItem, SpawnPointGroup
_Scripts/UI                UISessionController, GameplayHud, LoadingView
Resources/--DIContainer--  root scope prefab (spawns --NetworkManager--, registers services)
```

- `GameScope` (root, `DontDestroyOnLoad`) registers configs, `NetworkManager`,
  `TransportProvider`, `SteamSessionService` and the entry points.
- `TransportProvider` is the only place that selects the transport
  (`NetworkConfig.NetworkTransport`, `ConnectToSteamID`); gameplay code never touches it.
- Scenes: `MainMenu` (menu + lobby), `Gameplay` (arena).

**Extension:** another transport → add it to `--NetworkManager--` and to
`TransportProvider.Select`; another item type → prefab with `NetworkObject`,
`NetworkTransform` (Owner), `Rigidbody`, `NetworkItem`, registered in
`NetworkPrefabsConfig` and in `DefaultNetworkPrefabs`.

## Network design

Listen server over Steam P2P (SDR).

| Entity | Authority | Sync |
|---|---|---|
| Player | owner | `NetworkTransform` Owner, remote players interpolated |
| Free item | host | host `Rigidbody`, `NetworkTransform` Owner (owner = host) |
| Held item | holder | kinematic, holder writes the transform |
| Thrown item | host | host applies velocity and simulates |

- **Approval:** host checks player limit (`Session is full.`) and duplicate SteamID
  (`You are already in this session.`); the client shows `DisconnectReason`.
- **Lobby:** Steam lobby members are the lobby player list (name + SteamID).
- **Scene load:** the host calls `NetworkManager.SceneManager.LoadScene(Gameplay, Single)`
  (NGO network scene management, `EnableSceneManagement = true`); NGO replicates the
  load to every client. `LoadingService` shows an overlay per peer and hides it on
  `OnLoadEventCompleted`. Scene synchronisation for late joiners is ignored.
- **Late join:** NGO sync loads `Gameplay` on the joining client; `PlayerSpawner` spawns
  its player on `OnSynchronizeComplete`; items and holders arrive as replicated state.
- **Client leaves with an item:** the server clears the holder and drops the item
  (`OnNetworkDespawn`); items are `DontDestroyWithOwner = true` so NGO does not delete
  them on disconnect.
- **Host leaves:** clients read the disconnect reason, leave the lobby and load
  `MainMenu` with the local Unity `SceneManager` (no host to drive a network load).
- **Items:** `F` → `SpawnItemServerRpc` (host owns the item, survives the creator leaving);
  `E` → `GrabServerRpc` validates distance, free item, no current holder, then transfers
  ownership; `Q` → `ThrowServerRpc`; `X` → `DeleteServerRpc`. `NetworkVariable<ulong>
  Holder` replicates who holds what. Two simultaneous grabs are serialised by the host.

## Known issues

- Steam is the only wired transport; no IP session service.
- Approval does not validate the build version.
- Held items are kinematic on every peer, so they do not push other objects until thrown.
- No client-side prediction (remote players/items are interpolated, one RTT behind).
- Steam overlay invites depend on the environment (overlay enabled, build, not admin);
  joining by lobby ID or "Join Game" always works.
- No automated tests.

## Time and video

Approximate time spent: ~23H
Demo video: In Root Folder Project
