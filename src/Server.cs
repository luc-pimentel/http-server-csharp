using System.Net;
using System.Net.Sockets;
using System.IO;

public class Server
{
    private static string? _directory;
    public static void Main(string[] args)
    {
        Console.WriteLine("Starting server...");
        Console.WriteLine($"Received command: {string.Join(" ", args)}");
        _directory = args.Length > 1 && args[0] == "--directory" ? args[1] : "";
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Server started on port 4221");
        
        while (true)
        {
            Socket clientSocket = server.AcceptSocket();
            _ = Task.Run(() => HandleClientAsync(clientSocket));
        }
    }
    static async Task HandleClientAsync(Socket clientSocket)
    {
        try
        {
        using (clientSocket)
        using (NetworkStream stream = new NetworkStream(clientSocket))
        {
            // Read the HTTP request
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string request = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine("request: " + request);

            // Parse the path from the request
            string[] requestLines = request.Split('\n');
            string[] requestParts = requestLines[0].Split(' ');
            string method = requestParts[0];
            string path = requestParts[1];
            Console.WriteLine("method: " + method + " path: " + path);

            string userAgent = "";
            int contentLength = 0;
            foreach (string line in requestLines)
            {
                if (line.StartsWith("User-Agent: ", StringComparison.OrdinalIgnoreCase))
                {
                    userAgent = line.Substring("User-Agent: ".Length).Trim();
                }
                else if (line.StartsWith("Content-Length: ", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(line.Substring("Content-Length: ".Length).Trim(), out contentLength);
                }
            }


            // Prepare response based on path
            string response;
            string requestBody = "";
            if (contentLength > 0)
            {
                byte[] bodyBuffer = new byte[contentLength];
                await stream.ReadAsync(bodyBuffer, 0, contentLength);
                requestBody = System.Text.Encoding.ASCII.GetString(bodyBuffer);
            }

            // Handle POST request to /files/
            if (method == "POST" && path.StartsWith("/files/"))
            {
                string filename = path.Substring("/files/".Length);
                string fullPath = Path.Combine(_directory ?? "", filename);
                Console.WriteLine("fullPath: " + fullPath);
                
                
                await File.WriteAllTextAsync(fullPath, requestBody ?? "");
                
                response = "HTTP/1.1 201 Created\r\n\r\n";
            }
            else if (path.StartsWith("/files/") && _directory != null)
            {
                    string filename = path.Substring("/files/".Length);
                    string fullPath = Path.Combine(_directory, filename);

                if (File.Exists(fullPath))
                {
                    byte[] fileContent = await File.ReadAllBytesAsync(fullPath);
                    response = "HTTP/1.1 200 OK\r\n" +
                              "Content-Type: application/octet-stream\r\n" +
                              $"Content-Length: {fileContent.Length}\r\n" +
                              "\r\n";
                    
                    // Send headers
                    byte[] headerBytes = System.Text.Encoding.ASCII.GetBytes(response);
                    await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
                    
                    // Send file content
                    await stream.WriteAsync(fileContent, 0, fileContent.Length);
                    return;
                }
                else
                {
                    response = "HTTP/1.1 404 Not Found\r\n\r\n";
                }
            }
            else if (path == "/")
            {
                response = "HTTP/1.1 200 OK\r\n\r\n";
            }
            else if (path.StartsWith("/echo/"))
            {
                string content = path.Substring("/echo/".Length);
                response = "HTTP/1.1 200 OK\r\n" +
                          "Content-Type: text/plain\r\n" +
                          $"Content-Length: {content.Length}\r\n" +
                          "\r\n" +
                          content;
            }
            else if (path == "/user-agent")
            {
                response = "HTTP/1.1 200 OK\r\n" +
                          "Content-Type: text/plain\r\n" +
                          $"Content-Length: {userAgent.Length}\r\n" +
                          "\r\n" +
                          userAgent;
            }
            else
            {
                response = "HTTP/1.1 404 Not Found\r\n\r\n";
            }

            Console.WriteLine("response: " + response);
            byte[] responseBytes = System.Text.Encoding.ASCII.GetBytes(response);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error handling client: {ex.Message}");
    }
}
}
