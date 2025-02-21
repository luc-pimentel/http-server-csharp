# HTTP Server in C#

A lightweight HTTP/1.1 server implementation built in C# that demonstrates core networking concepts and HTTP protocol handling. This project was created as a learning exercise to understand web servers from the ground up and practice some C# concepts.

## Features

- HTTP/1.1 protocol support
- Concurrent client handling using async/await
- File operations (GET/POST)
- Response compression (gzip)
- Custom routing system
- Basic error handling

## Technical Stack

- C# (.NET 9.0)
- TCP/IP Networking (System.Net.Sockets)
- Async I/O operations
- GZip compression

## Getting Started

1. Prerequisites:
   - .NET 9.0 SDK
   - Any modern IDE (Visual Studio, VS Code, Rider)

2. Clone the repository:

3. Build and run:
   ```sh
   dotnet build
   dotnet run
   ```

   The server will start on port 4221 by default.

## Usage Examples

1. Basic GET request:
   ```sh
   curl http://localhost:4221/
   ```

2. Echo endpoint:
   ```sh
   curl http://localhost:4221/echo/hello-world
   ```

3. File operations:
   ```sh
   # Upload a file
   curl -X POST -d "content" http://localhost:4221/files/example.txt

   # Download a file
   curl http://localhost:4221/files/example.txt
   ```

> Inspired by and built following the "Build Your Own HTTP server" challenge from [CodeCrafters](https://codecrafters.io/) (not affiliated).