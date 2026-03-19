namespace StreamDeckService
{
    using OpenMacroBoard.SDK;
    using StreamDeckSharp;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private List<IMacroBoard> decks = new();
        private byte brightness = 15;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        private void Log(string log)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(log);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log($"Worker running at: {DateTimeOffset.Now}");

            foreach (var deck in StreamDeck.EnumerateDevices())
            {
                var openDeck = deck.Open();
                decks.Add(openDeck);
                openDeck.KeyStateChanged += this.OpenDeck_KeyStateChanged;
            }

            try
            {
                new HttpServer(8081, HandleRequest, stoppingToken).Run();
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
                Environment.Exit(1);
            }
        }

        private void OpenDeck_KeyStateChanged(object? sender, KeyEventArgs e)
        {
            (sender as IMacroBoard)?.SetBrightness(brightness);
        }

        private bool HandleRequest(string path)
        {
            if (path.StartsWith("/setBrightness/"))
            {
                string brightstr = path.Substring(15);

                Console.WriteLine($"Setting brightness to: {brightstr}");

                if (byte.TryParse(brightstr, out brightness))
                {
                    decks.ForEach(d => d.SetBrightness(brightness));
                    return true;
                }
                else
                {
                    Console.WriteLine($"Invalid brightness: {brightstr}");
                    return false;
                }
            }

            return false;
        }
    }
}