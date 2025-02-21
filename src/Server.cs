using System.Net;
using System.Net.Sockets;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");


// ... existing code ...

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

while (true)
{
    TcpClient client = server.AcceptTcpClient();
    NetworkStream stream = client.GetStream();

    // Read the HTTP request
    byte[] buffer = new byte[1024];
    int bytesRead = stream.Read(buffer, 0, buffer.Length);
    string request = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
    Console.WriteLine("request: " + request);

    // Parse the path from the request
    string[] requestLines = request.Split('\n');
    string[] requestParts = requestLines[0].Split(' ');
    string path = requestParts[1];

    // Prepare response based on path
    string response;
    if (path == "/")
    {
        response = "HTTP/1.1 200 OK\r\n\r\n";
    }
    else
    {
        response = "HTTP/1.1 404 Not Found\r\n\r\n";
    }

    byte[] responseBytes = System.Text.Encoding.ASCII.GetBytes(response);
    stream.Write(responseBytes, 0, responseBytes.Length);

    client.Close();
}