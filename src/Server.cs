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

    // Send HTTP response
    string response = "HTTP/1.1 200 OK\r\n\r\n";
    byte[] responseBytes = System.Text.Encoding.ASCII.GetBytes(response);
    stream.Write(responseBytes, 0, responseBytes.Length);

    client.Close();
}