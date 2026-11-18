using System;


namespace Assets._Scripts._Core
{
    public enum TransportKind
    {
        Steam,
        DirectIp
    }

    public enum LobbyVisibility
    {
        FriendsOnly,
        Private,
        Public
    }

    public enum SessionPhase
    {
        Lobby = 0,
        Loading = 1,
        InGame = 2
    }

    public enum SessionState
    {
        Idle,
        HostStarting,
        Connecting,
        Lobby,
        Loading,
        InGame,
        ReturningToMenu
    }

    public readonly struct SessionResult
    {
        public readonly bool Success;
        public readonly string Error;

        private SessionResult(bool success, string error)
        {
            Success = success;
            Error = error;
        }

        public static SessionResult Ok() => new SessionResult(true, null);
        public static SessionResult Fail(string error) => new SessionResult(false, error);
    }

    public readonly struct HostOptions
    {
        public readonly string Name;
        public readonly int MaxPlayers;
        public readonly LobbyVisibility Visibility;

        public HostOptions(string name, int maxPlayers, LobbyVisibility visibility)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Coop Session" : name.Trim();
            MaxPlayers = Math.Clamp(maxPlayers, 2, 4);
            Visibility = visibility;
        }
    }
}