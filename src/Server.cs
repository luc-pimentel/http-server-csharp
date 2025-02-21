using System.Net;
using System.Net.Sockets;
using System.IO;
// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");
// ... existing code ...

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

while (true)
{
    TcpClient client = server.AcceptTcpClient();
    // Handle each client in a separate task
    _ = HandleClientAsync(client);
}

// Add this new method
static async Task HandleClientAsync(TcpClient client)
{
    try
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        {
            // Read the HTTP request
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string request = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine("request: " + request);

            // Parse the path from the request
            string[] requestLines = request.Split('\n');
            string[] requestParts = requestLines[0].Split(' ');
            string path = requestParts[1];

            string userAgent = "";
            foreach (string line in requestLines)
            {
                if (line.StartsWith("User-Agent: ", StringComparison.OrdinalIgnoreCase))
                {
                    userAgent = line.Substring("User-Agent: ".Length).Trim();
                    break;
                }
            }

            // Prepare response based on path
            string directory = args.Length > 1 && args[0] == "--directory" ? args[1] : "";

            // Prepare response based on path
            string response;
            if (path.StartsWith("/files/"))
            {
                string filename = path.Substring("/files/".Length);
                string fullPath = Path.Combine(directory, filename);

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

            byte[] responseBytes = System.Text.Encoding.ASCII.GetBytes(response);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error handling client: {ex.Message}");
    }
}