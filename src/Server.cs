using System.Net;
using System.Net.Sockets;
using System.IO;
using System.IO.Compression;
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
            // Read the initial request headers
            byte[] buffer = new byte[4096];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string request = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Raw request:\n" + request);

            // Find the end of headers (marked by double CRLF)
            int headerEnd = request.IndexOf("\r\n\r\n");
            if (headerEnd == -1)
            {
                Console.WriteLine("Error: No header termination found");
                return;
            }

            // Parse only the headers portion
            string headers = request.Substring(0, headerEnd);
            string[] requestLines = headers.Split('\n');
            string[] requestParts = requestLines[0].Split(' ');
            string method = requestParts[0];
            string path = requestParts[1];
            Console.WriteLine($"method: {method} path: {path}");

            // Parse headers for User-Agent and Content-Length
            string userAgent = "";
            int contentLength = 0;
            string acceptEncoding = "";
            foreach (string line in requestLines)
            {
                if (line.StartsWith("User-Agent: ", StringComparison.OrdinalIgnoreCase))
                {
                    userAgent = line.Substring("User-Agent: ".Length).Trim();
                    Console.WriteLine($"userAgent: {userAgent}");
                }
                else if (line.StartsWith("Content-Length: ", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(line.Substring("Content-Length: ".Length).Trim(), out contentLength);
                    Console.WriteLine($"contentLength: {contentLength}");
                }
                else if (line.StartsWith("Accept-Encoding: ", StringComparison.OrdinalIgnoreCase))
                {
                    acceptEncoding = line.Substring("Accept-Encoding: ".Length).Trim();
                    Console.WriteLine($"acceptEncoding: {acceptEncoding}");
                }
            }

            // Read the request body if present
            string requestBody = "";
            if (contentLength > 0)
            {
                // The body starts after the double CRLF
                int bodyStartInBuffer = headerEnd + 4;
                int bodyBytesInBuffer = bytesRead - bodyStartInBuffer;
                
                // Create a buffer for the complete body
                byte[] bodyBuffer = new byte[contentLength];
                
                // Copy any body bytes we already read
                if (bodyBytesInBuffer > 0)
                {
                    Array.Copy(buffer, bodyStartInBuffer, bodyBuffer, 0, bodyBytesInBuffer);
                }
                
                // Read any remaining body bytes
                int remainingBytes = contentLength - bodyBytesInBuffer;
                int totalBytesRead = bodyBytesInBuffer;
                
                while (remainingBytes > 0)
                {
                    int n = await stream.ReadAsync(bodyBuffer, totalBytesRead, remainingBytes);
                    if (n == 0) break; // Connection closed
                    totalBytesRead += n;
                    remainingBytes -= n;
                }
                
                requestBody = System.Text.Encoding.ASCII.GetString(bodyBuffer);
                Console.WriteLine($"Request body: '{requestBody}'");
            }

            // Prepare response based on path
            string response;

            // Handle POST request to /files/
            if (method == "POST" && path.StartsWith("/files/"))
            {
                string filename = path.Substring("/files/".Length);
                string fullPath = Path.Combine(_directory ?? "", filename);
                Console.WriteLine($"Writing to file: {fullPath}");
                Console.WriteLine($"Content to write: '{requestBody}'");
                
                await File.WriteAllTextAsync(fullPath, requestBody);
                
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
                byte[] contentBytes = System.Text.Encoding.ASCII.GetBytes(content);
                
                // Check if client accepts gzip encoding
                if (acceptEncoding.Contains("gzip"))
                {
                    using var memoryStream = new MemoryStream();
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                    {
                        gzipStream.Write(contentBytes, 0, contentBytes.Length);
                    }
                    
                    byte[] compressedContent = memoryStream.ToArray();
                    
                    response = "HTTP/1.1 200 OK\r\n" +
                              "Content-Type: text/plain\r\n" +
                              "Content-Encoding: gzip\r\n" +
                              $"Content-Length: {compressedContent.Length}\r\n" +
                              "\r\n";
                              
                    byte[] headerBytes = System.Text.Encoding.ASCII.GetBytes(response);
                    await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
                    await stream.WriteAsync(compressedContent, 0, compressedContent.Length);
                    return;
                }
                else
                {
                    response = "HTTP/1.1 200 OK\r\n" +
                              "Content-Type: text/plain\r\n" +
                              $"Content-Length: {content.Length}\r\n" +
                              "\r\n" +
                              content;
                }
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
