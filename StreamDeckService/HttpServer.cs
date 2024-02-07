using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class HttpServer
{
    private TcpListener listener;
    private Func<string, bool> handleRequest;
    private CancellationToken cancellationToken;

    public HttpServer(int port, Func<string, bool> handleRequest, CancellationToken cancellationToken)
    {
        this.listener = new TcpListener(IPAddress.Any, port);
        this.handleRequest = handleRequest;
        this.cancellationToken = cancellationToken;
    }

    public void Run()
    {
        listener.Start();
        Console.WriteLine("Server is listening...");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (listener.Pending())
                {
                    using (TcpClient client = listener.AcceptTcpClient())
                    using (NetworkStream stream = client.GetStream())
                    using (StreamReader reader = new StreamReader(stream))
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        string requestLine = reader.ReadLine();
                        string responseStatus = ProcessRequest(requestLine);
                        SendResponse(writer, responseStatus);
                    }
                }
                else
                {
                    // To avoid busy waiting, sleep a bit
                    Thread.Sleep(25); // Sleep for 25 milliseconds
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

        string path = tokens[1];
        bool handleResult = handleRequest(path);
        return handleResult ? "HTTP/1.0 200 OK\r\n" : "HTTP/1.0 400 Bad Request\r\n";
    }

    private void SendResponse(StreamWriter writer, string responseStatus)
    {
        writer.WriteLine(responseStatus);
        writer.WriteLine("Connection: close\r\n");
        writer.Flush();
    }
}
