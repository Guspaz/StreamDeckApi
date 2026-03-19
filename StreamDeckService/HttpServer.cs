using System.Net;
using System.Net.Sockets;

class HttpServer(int port, Func<string, bool> handleRequest, CancellationToken cancellationToken)
{
    private readonly TcpListener listener = new(IPAddress.Any, port);
    private readonly Func<string, bool> handleRequest = handleRequest;
    private readonly CancellationToken cancellationToken = cancellationToken;

    public void Run()
    {
        listener.Start();
        Console.WriteLine("Server is listening...");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using (TcpClient client = listener.AcceptTcpClient())
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    string responseStatus = ProcessRequest(reader.ReadLine() ?? "");
                    SendResponse(writer, responseStatus);
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // This exception can be thrown if the listener is closed during a cancellation.
            // It's an expected exception when stopping the server, so it can be safely ignored.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception occurred: {ex.Message}");
        }
        finally
        {
            listener.Stop();
            Console.WriteLine("Server has stopped.");
        }
    }

    private string ProcessRequest(string requestLine)
    {
        if (string.IsNullOrEmpty(requestLine))
        {
            return "HTTP/1.0 400 Bad Request\r\n";
        }

        string[] tokens = requestLine.Split(' ');
        if (tokens.Length < 3 || tokens[0] != "GET")
        {
            return "HTTP/1.0 400 Bad Request\r\n";
        }

        return handleRequest(tokens[1]) ? "HTTP/1.0 200 OK\r\n" : "HTTP/1.0 400 Bad Request\r\n";
    }

    private static void SendResponse(StreamWriter writer, string responseStatus)
    {
        writer.WriteLine(responseStatus);
        writer.WriteLine("Connection: close\r\n");
        writer.Flush();
    }
}
