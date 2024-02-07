namespace StreamDeckService
{
    using OpenMacroBoard.SDK;
    using StreamDeckSharp;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        List<IMacroBoard> decks = new();

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
                decks.Add(deck.Open());
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

        private bool HandleRequest(string path)
        {
            if (path.StartsWith("/setBrightness/"))
            {
                string brightness = path.Substring(15);

                Console.WriteLine($"Setting brightness to: {brightness}");

                if (byte.TryParse(brightness, out byte value))
                {
                    decks.ForEach(d => d.SetBrightness(Convert.ToByte(brightness)));
                    return true;
                }
                else
                {
                    Console.WriteLine($"Invalid brightness: {brightness}");
                    return false;
                }
            }

            return false;
        }
    }
}


//using OpenMacroBoard.SDK;
//using PeanutButter.SimpleHTTPServer;
//using PeanutButter.Utils;
//using StreamDeckSharp;

//namespace StreamDeckService
//{
//    public class Worker : BackgroundService
//    {
//        private readonly ILogger<Worker> _logger;
//        List<IMacroBoard> decks = new();

//        public Worker(ILogger<Worker> logger)
//        {
//            _logger = logger;
//        }

//        private void Log(string log)
//        {
//            if (_logger.IsEnabled(LogLevel.Information))
//            {
//                _logger.LogInformation(log);
//            }
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            Log($"Worker running at: {DateTimeOffset.Now}");

//            StreamDeck.EnumerateDevices().ForEach(d => decks.Add(d.Open()));

//            try
//            {
//                while (!stoppingToken.IsCancellationRequested)
//                {
//                    var http = new PeanutButter.SimpleHTTPServer.HttpServer(8081, true, null);
//                    http.AddHandler(handler);
//                    Log("Listening on port 8081");

//                    while (http.IsListening && !stoppingToken.IsCancellationRequested)
//                    {
//                        await Task.Delay(1000, stoppingToken);
//                    }
//                }
//            }
//            catch (TaskCanceledException) { }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "{Message}", ex.Message);
//                Environment.Exit(1);
//            }
//        }

//        HttpServerPipelineResult handler(HttpProcessor processor, Stream stream)
//        {
//            Console.WriteLine("Request: " + processor.FullPath);

//            if (processor.FullPath.StartsWith("/setBrightness/"))
//            {
//                string brightness = processor.FullPath.Substring(15);

//                Console.WriteLine($"Setting brightness to: {brightness}");
//                if (byte.TryParse(brightness, out byte value))
//                {
//                    decks.ForEach(d => d.SetBrightness(Convert.ToByte(brightness)));
//                    processor.WriteOKStatusHeader();
//                    processor.WriteDocument("200");
//                }
//                else
//                {
//                    Console.WriteLine($"Invalid brightness: {brightness}");
//                    processor.WriteFailure(System.Net.HttpStatusCode.BadRequest, "INVALID BRIGHTNESS", "400");
//                }
//            }
//            else
//            {
//                processor.WriteFailure(System.Net.HttpStatusCode.NotFound, "NOT FOUND", "404");
//            }

//            return HttpServerPipelineResult.Handled;
//        }

//    }
//}
