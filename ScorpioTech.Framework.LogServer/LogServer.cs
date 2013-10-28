using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace ScorpioTech.Framework.LogServer
{
    /// <summary>
    /// Simple TCP/IP Log Server that can be connected to by any telnet type application to watch the log file
    /// </summary>
    public class LogServer
    {
        /// <summary>
        /// Delegate for the onClientConnect Event
        /// </summary>
        /// <param name="client">Address of Client that connected</param>
        public delegate void ClientConnectedEvent(string client);
        /// <summary>
        /// Delegate for the onClientDisconnect Event
        /// </summary>
        /// <param name="client">Address of Client that disconnected</param>
        public delegate void ClientDisconnectedEvent(string client);
        /// <summary>
        /// Delegate for the onServerException Event
        /// </summary>
        /// <param name="ex">Exception that occurred and was not handled</param>
        public delegate void LogServerExceptionEvent(Exception ex);

        /// <summary>
        /// Occurs when a new client connects to the server
        /// </summary>
        public event ClientConnectedEvent onClientConnect;
        /// <summary>
        /// Occurs when a client disconnects from the server
        /// </summary>
        public event ClientDisconnectedEvent onClientDisconnect;
        /// <summary>
        /// Occurs when a exception occurs in the log server and is not handled internally
        /// </summary>
        public event LogServerExceptionEvent onServerException;

        private TcpListener tcp_server;
        private Thread server_thread;
        private Mutex logMutex;
        private volatile bool running = false;
        private List<LogServer_Client> clients;
        private List<LogServer_Client> discon_clients;

        private string greeting;

        private Queue<string> logLines;
        private Queue<string> unsentLines;

        private int maxLogSize;

        /// <summary>
        /// Create a standard log server on port 446
        /// </summary>
        /// <param name="greetingMessage">Greeting to send to clients when they connect</param>
        /// <param name="logSize">Number of lines to keep in memory and return initially</param>
        public LogServer(string greetingMessage, int logSize)
            : this(446, greetingMessage, logSize)
        { }

        /// <summary>
        /// Create a standard log server on a custom port
        /// </summary>
        /// <param name="port">The port to listen on</param>
        /// <param name="greetingMessage">Greeting to send to clients when they connect</param>
        /// <param name="logSize">Number of lines to keep in memory and return initially</param>
        public LogServer(int port, string greetingMessage, int logSize)
        {
            this.tcp_server = new TcpListener(IPAddress.Any, port);
            this.server_thread = new Thread(this._exec);
            this.clients = new List<LogServer_Client>();
            this.discon_clients = new List<LogServer_Client>();

            this.greeting = greetingMessage;

            this.logLines = new Queue<string>();
            this.unsentLines = new Queue<string>();
            this.maxLogSize = logSize;
            this.logMutex = new Mutex();
        }

        /// <summary>
        /// Start the Log Server
        /// </summary>
        public void Start()
        {
            this.server_thread.Start();
        }

        /// <summary>
        /// Stop the Log Server
        /// </summary>
        public void Stop()
        {
            this.running = false;
        }

        /// <summary>
        /// Join the Log Server and wait for it to shutdown
        /// </summary>
        public void Join()
        {
            this.server_thread.Join();
        }

        /// <summary>
        /// Is the Log Server Active
        /// </summary>
        public bool Active
        {
            get { return this.running; }
        }

        /// <summary>
        /// Log a new line to the clients
        /// </summary>
        /// <param name="line">Any text to be sent to the Log Client</param>
        public void Log(string line)
        {
            if (logMutex.WaitOne() == true)
            {
                try
                {
                    this.logLines.Enqueue(line);
                    if (this.logLines.Count > maxLogSize)
                    {
                        this.logLines.Dequeue();
                    }
                    this.unsentLines.Enqueue(line);
                }
                finally
                {
                    logMutex.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// Returns a copy of the Log History
        /// </summary>
        public string[] LogHistory
        {
            get
            {
                if (logMutex.WaitOne() == true)
                {
                    try
                    {
                        return this.logLines.ToArray();
                    }
                    finally
                    {
                        logMutex.ReleaseMutex();
                    }
                }

                return new string[0];
            }
        }

        private void _exec()
        {
            this.running = true;
            try
            {
                this.tcp_server.Start();

                while (this.running == true)
                {
                    while (this.tcp_server.Pending())
                    {
                        TcpClient client = this.tcp_server.AcceptTcpClient();

                        LogServer_Client cl = new LogServer_Client(client);
                        cl.onThreadEnd += new LogServer_Client.ThreadEnd(cl_onThreadEnd);
                        this.clients.Add(cl);
                        cl.Send(greeting);
                        if (logMutex.WaitOne() == true)
                        {
                            try
                            {
                                foreach (string line in this.logLines)
                                {
                                    cl.Send(line);
                                }
                            }
                            finally
                            {
                                logMutex.ReleaseMutex();
                            }
                        }
                        cl.Start();
                        if (this.onClientConnect != null)
                        {
                            this.onClientConnect(cl.Address);
                        }
                    }

                    if((unsentLines.Count > 0) &&  (logMutex.WaitOne() == true))
                    {
                        try
                        {
                            foreach(string line in unsentLines)
                            {
                                sendLine(line);
                            }

                            unsentLines.Clear();
                        }
                        finally
                        {
                            logMutex.ReleaseMutex();
                        }
                    }

                    foreach (LogServer_Client cl in discon_clients)
                    {
                        clients.Remove(cl);
                    }
                    discon_clients.Clear();

                    Thread.Sleep(10);
                }

                foreach (LogServer_Client cl in this.clients)
                {
                    cl.Stop();
                    cl.Join();
                }

                this.tcp_server.Stop();
            }
            catch (Exception ex)
            {
                if (this.onServerException != null)
                {
                    this.onServerException(ex);
                }
            }

            this.running = false;
        }

        private void sendLine(string line)
        {
            foreach (LogServer_Client cl in this.clients)
            {
                cl.Send(line);
            }
        }

        private void cl_onThreadEnd(LogServer_Client sender)
        {
            discon_clients.Add(sender);
            if (this.onClientDisconnect != null)
            {
                this.onClientDisconnect(sender.Address);
            }
        }
    }
}
