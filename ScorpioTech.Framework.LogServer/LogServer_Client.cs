using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;

namespace ScorpioTech.Framework.LogServer
{
    internal class LogServer_Client
    {
        internal delegate void ThreadEnd(LogServer_Client sender);

        private Thread client_thread;
        private TcpClient client;
        private NetworkStream stream;
        private string address;
        private volatile bool terminate = false;

        private Mutex logMutex;
        private Queue<string> unsentLines;

        internal event ThreadEnd onThreadEnd;

        internal LogServer_Client(TcpClient client)
        {
            this.client = client;
            this.client_thread = new Thread(this._exec);
            this.stream = client.GetStream();
            this.address = client.Client.RemoteEndPoint.ToString();
            this.logMutex = new Mutex();
            this.unsentLines = new Queue<string>();
        }

        internal void Start()
        {
            this.client_thread.Start();
        }
        internal void Stop()
        {
            this.terminate = true;
        }
        internal void Join()
        {
            this.client_thread.Join(10000);
        }

        private void _exec()
        {
            while (this.terminate == false)
            {
                try
                {
                    if (this.client.Client.Poll(100, SelectMode.SelectRead) == true)
                    {
                        if (this.stream.DataAvailable == false)
                        {
                            this.terminate = true;
                            break;
                        }
                        else
                        {
                            byte[] buffer = new byte[1024 * 32];

                            this.stream.Read(buffer, 0, buffer.Length);
                        }
                    }

                    if ((unsentLines.Count > 0) && (logMutex.WaitOne() == true))
                    {
                        string[] lines = this.unsentLines.ToArray();
                        unsentLines.Clear();
                        logMutex.ReleaseMutex();

                        foreach (string line in lines)
                        {
                            byte[] lineData = Encoding.ASCII.GetBytes(line + "\r\n");
                            this.stream.Write(lineData, 0, lineData.Length);
                        }
                    }

                    Thread.Sleep(50);
                }
                catch
                {

                }
            }

            if (this.onThreadEnd != null)
            {
                this.onThreadEnd(this);
            }

            this.stream.Close(10);
            this.client.Close();
        }

        /// <summary>
        /// The remote client address
        /// </summary>
        public string Address
        {
            get { return this.address; }
        }

        /// <summary>
        /// Is this connection still active
        /// </summary>
        public bool Active
        {
            get { return !this.terminate; }
        }

        /// <summary>
        /// Queue up a line to send to the client
        /// </summary>
        /// <param name="line"></param>
        internal void Send(string line)
        {
            if (logMutex.WaitOne() == true)
            {
                try
                {
                    this.unsentLines.Enqueue(line);
                }
                finally
                {
                    logMutex.ReleaseMutex();
                }
            }
        }
    }
}
