namespace Assets._Scripts._Core
{
    /// <summary>
    /// Supported arguments:
    ///   -host
    ///   +connect_lobby 109775241234567890
    /// </summary>
    public sealed class CommandLineOptions
    {
        public bool AutoHost;
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
                    case "-host":
                        result.AutoHost = true;
                        break;
                    case "+connect_lobby":
                        if (ulong.TryParse(Next(), out var lobbyId))
                            result.ConnectLobbyId = lobbyId;
                        break;
                }
            }

            return result;
        }
    }
}
