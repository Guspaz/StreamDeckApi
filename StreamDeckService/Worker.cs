namespace StreamDeckService
{
    using System.Reflection;
    using OpenMacroBoard.SDK;
    using StreamDeckSharp;

    public class Worker(ILogger<Worker> logger) : BackgroundService
    {
        private readonly List<IMacroBoard> decks = [];
        private byte brightness = 15;

        private void Log(string log)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(log);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log($"Worker running at: {DateTimeOffset.Now}");

            foreach (var deck in StreamDeck.EnumerateDevices())
            {
                var openDeck = deck.Open();
                openDeck.KeyStateChanged += (sender, e) => (sender as IMacroBoard)?.SetBrightness(brightness);
                decks.Add(openDeck);
            }

            try
            {
                new HttpServer(8081, HandleRequest, stoppingToken).Run();
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Message}", ex.Message);
                Environment.Exit(1);
            }
        }

        private bool HandleRequest(string path)
        {
            if (path.StartsWith("/setBrightness/"))
            {
                string brightstr = path[15..];

                Console.WriteLine($"Setting brightness to: {brightstr}");

                if (byte.TryParse(brightstr, out brightness))
                {
                    decks.ForEach(d => d.SetBrightness(brightness));
                    return true;
                }

                Console.WriteLine($"Invalid brightness: {brightstr}");
            }

            return false;
        }
    }
}