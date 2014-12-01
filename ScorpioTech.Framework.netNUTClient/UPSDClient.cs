using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace ScorpioTech.Framework.netNUTClient
{
    /// <summary>
    /// A simple client for communicating with a UPSD daemon server
    /// </summary>
    public class UPSDClient
    {
        private string upsdServer;
        private string upsdHostname;
        private UInt16 upsdPort;
        private bool connected = false;
        private TcpClient client;
        private NetworkStream clientStream;
        private MemoryStream clientRecvBuffer;

        /// <summary>
        /// Create a new client connected to the specified address
        /// </summary>
        /// <param name="upsdServer">The address of the UPSD server in the format &lt;hostname&gt;[:&lt;port&gt;]</param>
        public UPSDClient(string upsdServer)
        {
            this.upsdServer = upsdServer;
            string[] parts = upsdServer.Split(new char[] {':'}, StringSplitOptions.RemoveEmptyEntries);
            this.upsdHostname = parts[0];
            this.upsdPort = 3493;
            if( parts.Count() > 1)
            {
                if (UInt16.TryParse(parts[1], out this.upsdPort) == false)
                {
                    throw new ArgumentOutOfRangeException("upsdServer", "Error: Invalid hostname.");
                }
            }
        }
        /// <summary>
        /// Create a new client connected to the specified address and port
        /// </summary>
        /// <param name="upsdHostname">IP address or host name</param>
        /// <param name="upsdPort">(Optional) TCP Port UPSD is listening on (default is 3493)</param>
        public UPSDClient(string upsdHostname, UInt16 upsdPort)
        {
            this.upsdServer = upsdHostname + ":" + upsdPort.ToString();
            this.upsdHostname = upsdHostname;
            this.upsdPort = upsdPort;
        }
        /// <summary>
        /// Destructor to ensure disconnection from UPSD server
        /// </summary>
        ~UPSDClient()
        {
            this.Disconnect();
        }
        /// <summary>
        /// Connect to the UPSD daemon
        /// </summary>
        public void Connect()
        {
            if (connected) return;

            this.client = new TcpClient(this.upsdHostname, this.upsdPort);
            this.connected = true;
            this.clientStream = this.client.GetStream();
            this.clientRecvBuffer = new MemoryStream(128);
        }
        /// <summary>
        /// Disconnect from the UPSD daemon
        /// </summary>
        public void Disconnect()
        {
            if (!connected) return;

            this.clientStream.Close();
            this.client.Close();
        }
        /// <summary>
        /// Get a list of the UPS's configured on the server
        /// </summary>
        /// <returns>List of <see cref="UPS"/> on the server</returns>
        public List<UPS> ListUPS()
        {
            if( connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            List<UPS> list = new List<UPS>();
            byte[] cmdListUPS =ASCIIEncoding.ASCII.GetBytes("LIST UPS\n");
            clientStream.Write(cmdListUPS, 0, cmdListUPS.Length);
            String reply = "";
            while( clientStream.CanRead)
            {
                if (client.Client.Poll(100, SelectMode.SelectRead) == true)
                {
                    if(clientStream.DataAvailable == false)
                    {
                        break;
                    }

                    byte[] buffer = new byte[1024];
                    int bytesRead = this.clientStream.Read(buffer, 0, buffer.Length);
                    string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    reply+= data;
                }
                if (reply.StartsWith("ERR ") && reply.EndsWith("\n"))
                {
                    HandleUPSDError(reply);
                }
                if (reply.StartsWith("BEGIN LIST UPS\n") && reply.EndsWith("END LIST UPS\n"))
                {
                    string[] parts = reply.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int idx = 1; idx < parts.Length - 1; idx++ )
                    {
                        string[] upsParts = parts[idx].Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                        if (upsParts[0] != "UPS") continue;
                        if (upsParts.Length != 3) continue;

                        UPS temp = new UPS(upsParts[1]);
                        temp.Description = upsParts[2].Replace("\"", "");
                        list.Add(temp);
                    }
                    break;
                }
            }

            return list;
        }
        /// <summary>
        /// Get a list of the variables and their values for the specified UPS
        /// </summary>
        /// <param name="upsName">Name of the UPS</param>
        /// <returns>Dictionary of the variable names(keys) and their current values</returns>
        public Dictionary<string, string> ListUPSVar(string upsName)
        {
            if (connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            Dictionary<string, string> list = new Dictionary<string, string>();
            byte[] cmdListUPSVars = ASCIIEncoding.ASCII.GetBytes("LIST VAR " + upsName + "\n");
            clientStream.Write(cmdListUPSVars, 0, cmdListUPSVars.Length);
            String reply = "";
            while (clientStream.CanRead)
            {
                if (client.Client.Poll(100, SelectMode.SelectRead) == true)
                {
                    if (clientStream.DataAvailable == false)
                    {
                        break;
                    }

                    byte[] buffer = new byte[1024];
                    int bytesRead = this.clientStream.Read(buffer, 0, buffer.Length);
                    string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    reply += data;
                }
                if (reply.StartsWith("ERR ") && reply.EndsWith("\n"))
                {
                    HandleUPSDError(reply);
                }
                if (reply.StartsWith("BEGIN LIST VAR " + upsName + "\n") && reply.EndsWith("END LIST VAR " + upsName + "\n"))
                {
                    string[] parts = reply.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int idx = 1; idx < parts.Length - 1; idx++)
                    {
                        string[] varParts = parts[idx].Split(new char[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                        if (varParts[0] != "VAR") continue;
                        if (varParts.Length != 4) continue;
                        if (varParts[1] != upsName) continue;

                        varParts[3] = varParts[3].Replace("\"", "");
                        list.Add(varParts[2], varParts[3]);
                    }
                    break;
                }
            }

            return list;
        }
        /// <summary>
        /// Get a list of the network clients connected to the specified UPS
        /// </summary>
        /// <param name="upsName">Name of the UPS</param>
        /// <returns>A list of IP Addresses for all currently connected clients</returns>
        public List<string> ListUPSClient(string upsName)
        {
            if (connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            List<string> list = new List<string>();
            byte[] cmdListClients = ASCIIEncoding.ASCII.GetBytes("LIST CLIENT " + upsName + "\n");
            clientStream.Write(cmdListClients, 0, cmdListClients.Length);
            String reply = "";
            while (clientStream.CanRead)
            {
                if (client.Client.Poll(100, SelectMode.SelectRead) == true)
                {
                    if (clientStream.DataAvailable == false)
                    {
                        break;
                    }

                    byte[] buffer = new byte[1024];
                    int bytesRead = this.clientStream.Read(buffer, 0, buffer.Length);
                    string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    reply += data;
                }
                if (reply.StartsWith("ERR ") && reply.EndsWith("\n"))
                {
                    HandleUPSDError(reply);
                }
                if (reply.StartsWith("BEGIN LIST CLIENT " + upsName + "\n") && reply.EndsWith("END LIST CLIENT " + upsName + "\n"))
                {
                    string[] parts = reply.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int idx = 1; idx < parts.Length - 1; idx++)
                    {
                        string[] upsParts = parts[idx].Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                        if (upsParts[0] != "CLIENT") continue;
                        if (upsParts.Length != 3) continue;
                        if (upsParts[1] != upsName) continue;

                        list.Add(upsParts[2]);
                    }
                    break;
                }
            }

            return list;
        }
        /// <summary>
        /// Get the value of a specific variable on the specified UPS
        /// </summary>
        /// <param name="upsName">Name of the UPS</param>
        /// <param name="varName">Name of the variable (for example 'ups.status')</param>
        /// <returns>The value of the requested variable as a string</returns>
        public string GetUPSVar(string upsName, string varName)
        {
            if (connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            byte[] cmdGetUPSVar = ASCIIEncoding.ASCII.GetBytes("GET VAR " + upsName + " " + varName + "\n");
            clientStream.Write(cmdGetUPSVar, 0, cmdGetUPSVar.Length);
            String reply = "";
            while (clientStream.CanRead)
            {
                if (client.Client.Poll(100, SelectMode.SelectRead) == true)
                {
                    if (clientStream.DataAvailable == false)
                    {
                        break;
                    }

                    byte[] buffer = new byte[1024];
                    int bytesRead = this.clientStream.Read(buffer, 0, buffer.Length);
                    string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    reply += data;
                }
                if(reply.StartsWith("ERR ") && reply.EndsWith("\n"))
                {
                    HandleUPSDError(reply);
                }
                if (reply.StartsWith("VAR " + upsName + " ") && reply.EndsWith("\n"))
                {
                        string[] varParts = reply.Split(new char[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                        if (varParts[0] != "VAR") continue;
                        if (varParts.Length != 4) continue;
                        if (varParts[1] != upsName) continue;
                        if (varParts[2] != varName) continue;

                        return varParts[3].Replace("\"", "");
                    
                }
            }

            return string.Empty;
        }
        /// <summary>
        /// Handles all error replies from UPSD daemon and generates exceptions
        /// </summary>
        /// <param name="reply">Error reply received from server</param>
        private void HandleUPSDError(string reply)
        {
            string[] errParts = reply.Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
            if (errParts[0] != "ERR") throw new ArgumentException("Not a valid UPSD Error Message!", "reply");
            if (errParts.Length < 2) throw new ArgumentException("Not a valid UPSD Error Message!", "reply");

            UPSException.ErrorCode code;
            if( Enum.TryParse<UPSException.ErrorCode>(errParts[1].Replace('-', '_'), out code) == false)
            {
                throw new UPSException(UPSException.ErrorCode.UNKNOWN_ERROR, errParts[1] + " is not a recognized UPSD Error Code");
            }
            if( errParts.Length == 3)
            {
                throw new UPSException(code, errParts[2]);
            }

            throw new UPSException(code, string.Empty);
        }
    }
}
