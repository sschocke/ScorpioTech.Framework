#Log Server Library

##Overview
This simple library creates a logging target to send anything you want to log to.
It then creates a listening TCP port(default 446, but can be passed in as a parameter).
Simply use your favourite telnet client to connect to the port, and you can watch a live log of what your 
application is doing. Great for debugging things like Windows Services.

##Simple Example
```csharp
using ScorpioTech.Framework.LogServer;
using System;

namespace LoggingServerExample
{
    class Program
    {
        static void Main(string[] args)
        {
            LogServer server = new LogServer("Log Server Example", 100);
            server.onClientConnect += server_onClientConnect;
            server.onClientDisconnect += server_onClientDisconnect;

            server.Start();
            server.Log("Simple Test Log");
            server.Log("Retains a history of logged lines for later reading");
            Console.WriteLine("Connect to 127.0.0.1:446 using any telnet client to see log server in action");
            Console.WriteLine("Press the enter key to exit...");
            Console.ReadLine();

            server.Stop();
            server.Join();
        }

        static void server_onClientDisconnect(string client)
        {
            Console.WriteLine("Log Server Client Disconnected : " + client);
        }

        static void server_onClientConnect(string client)
        {
            Console.WriteLine("Log Server Client Connected : " + client);
        }
    }
}
```

Usage is simple. Create a **LoggingServer** instance, call **.Start()** to start the server, and use **.Log(...)**
to write a line of text out to the log. When exiting, call **.Stop()** to stop the server, and call **.Join()**
to wait for all clients to disconnect before terminating the program. _If you don't do the **.Join()** the
software will still close, but it may happen that there is still a connection open from a client. If you
were to start the software again immediately, the Logging Server may fail to start up due to this_
