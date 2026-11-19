using System;
using System.Globalization;

namespace Assets._Scripts._Core
{
    /// <summary>
    /// Examples:
    ///   -transport steam -host
    ///   -transport ip -join 127.0.0.1:7777
    ///   +connect_lobby 109775241234567890
    ///   -lag 150 -loss 5
    /// </summary>
    public class CommandLineOptions
    {
        public TransportKind? Transport;
        public bool AutoHost;
        public string JoinTarget;
        public string PlayerName;
        public string Ip;
        public ushort? Port;
        public int LagMs;
        public float LossPercent;
        public ulong? ConnectLobbyId;

        public static CommandLineOptions Parse(string[] args)
        {
            var result = new CommandLineOptions();
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i].ToLowerInvariant();
                string Next() => i + 1 < args.Length ? args[++i] : null;

                switch (arg)
                {
                    case "-transport":
                        var value = Next();
                        if (value == "steam") result.Transport = TransportKind.Steam;
                        else if (value == "ip" || value == "direct") result.Transport = TransportKind.DirectIp;
                        break;
                    case "-host":
                        result.AutoHost = true;
                        break;
                    case "-join":
                        result.JoinTarget = Next();
                        break;
                    case "-name":
                        result.PlayerName = Next();
                        break;
                    case "-ip":
                        result.Ip = Next();
                        break;
                    case "-port":
                        if (ushort.TryParse(Next(), out var port)) result.Port = port;
                        break;
                    case "-lag":
                        if (int.TryParse(Next(), out var lag)) result.LagMs = Math.Max(0, lag);
                        break;
                    case "-loss":
                        if (float.TryParse(Next(), NumberStyles.Float, CultureInfo.InvariantCulture, out var loss))
                            result.LossPercent = Math.Clamp(loss, 0f, 100f);
                        break;
                    case "+connect_lobby":
                        if (ulong.TryParse(Next(), out var lobbyId)) result.ConnectLobbyId = lobbyId;
                        break;
                }
            }

            return result;
        }
    }
}
