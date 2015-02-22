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
        private bool loggedin = true;
        private TcpClient client;
        private NetworkStream clientStream;
        private MemoryStream clientRecvBuffer;

        /// <summary>
        /// Are we connected to the UPSD server
        /// </summary>
        public bool Connected { get { return this.connected; } }
        /// <summary>
        /// Are we logged in to the UPSD server
        /// </summary>
        public bool LoggedIn { get { return this.loggedin; } }

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
            this.clientStream = this.client.GetStream();
            this.clientRecvBuffer = new MemoryStream(128);
            this.connected = true;
        }
        /// <summary>
        /// Disconnect from the UPSD daemon
        /// </summary>
        public void Disconnect()
        {
            if (!connected) return;

            if (loggedin)
            {
                this.Logout();
            }

            this.clientStream.Close();
            this.client.Close();
            this.connected = false;
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
        /// Get a list of the read/write variables available on the specified UPS
        /// </summary>
        /// <param name="upsName">Name of the UPS</param>
        /// <returns>A List of <see cref="UPS.VariableDescription"/> entries describing the available variables</returns>
        public List<UPS.VariableDescription> ListUPSReadWrite(string upsName)
        {
            if (connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            List<UPS.VariableDescription> list = new List<UPS.VariableDescription>();
            byte[] cmdListUPSReadWrite = ASCIIEncoding.ASCII.GetBytes("LIST RW " + upsName + "\n");
            clientStream.Write(cmdListUPSReadWrite, 0, cmdListUPSReadWrite.Length);
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
                if (reply.StartsWith("BEGIN LIST RW " + upsName + "\n") && reply.EndsWith("END LIST RW " + upsName + "\n"))
                {
                    string[] parts = reply.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int idx = 1; idx < parts.Length - 1; idx++)
                    {
                        string[] varParts = parts[idx].Split(new char[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                        if (varParts[0] != "RW") continue;
                        if (varParts.Length != 4) continue;
                        if (varParts[1] != upsName) continue;

                        varParts[3] = varParts[3].Replace("\"", "");
                        string desc = this.GetUPSVarDescription(upsName, varParts[2]);
                        string type = this.GetUPSVarType(upsName, varParts[2]);
                        type = type.Replace("RW ", "");
                        list.Add(new UPS.VariableDescription(varParts[2], desc, type, varParts[3]));
                    }
                    break;
                }
            }

            return list;
        }
        /// <summary>
        /// Get a list of the instant commands available on the specified UPS
        /// </summary>
        /// <param name="upsName">Name of the UPS</param>
        /// <returns>Dictionary of the instant command names(keys) and their descriptions</returns>
        public Dictionary<string, string> ListUPSCommands(string upsName)
        {
            if (connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            Dictionary<string, string> list = new Dictionary<string, string>();
            byte[] cmdListUPSCommands = ASCIIEncoding.ASCII.GetBytes("LIST CMD " + upsName + "\n");
            clientStream.Write(cmdListUPSCommands, 0, cmdListUPSCommands.Length);
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
                if (reply.StartsWith("BEGIN LIST CMD " + upsName + "\n") && reply.EndsWith("END LIST CMD " + upsName + "\n"))
                {
                    string[] parts = reply.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int idx = 1; idx < parts.Length - 1; idx++)
                    {
                        string[] cmdParts = parts[idx].Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                        if (cmdParts[0] != "CMD") continue;
                        if (cmdParts.Length != 3) continue;
                        if (cmdParts[1] != upsName) continue;
                        string desc = this.GetUPSCmdDescription(upsName, cmdParts[2]);
                        list.Add(cmdParts[2], desc);
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

                        return varParts[3].Replace("\"", "").Trim();
                    
                }
            }

            return string.Empty;
        }
        /// <summary>
        /// Gets the data type of a specific variable on the specified UPS
        /// </summary>
        /// <param name="upsName">Name of the UPS</param>
        /// <param name="varName">Name of the variable (for example 'ups.status')</param>
        /// <returns></returns>
        public string GetUPSVarType(string upsName, string varName)
        {
            if (connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            byte[] cmdGetUPSVar = ASCIIEncoding.ASCII.GetBytes("GET TYPE " + upsName + " " + varName + "\n");
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
                if (reply.StartsWith("ERR ") && reply.EndsWith("\n"))
                {
                    HandleUPSDError(reply);
                }
                if (reply.StartsWith("TYPE " + upsName + " ") && reply.EndsWith("\n"))
                {
                    string[] varParts = reply.Split(new char[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                    if (varParts[0] != "TYPE") continue;
                    if (varParts.Length != 4) continue;
                    if (varParts[1] != upsName) continue;
                    if (varParts[2] != varName) continue;

                    return varParts[3].Trim();
                }
            }

            return string.Empty;
        }
        /// <summary>
        /// Gets the description of the specified UPS
        /// </summary>
        /// <param name="upsName">Name of the UPS</param>
        /// <returns>The description of the UPS as in ups.conf on the server</returns>
        public string GetUPSDescription(string upsName)
        {
            if (connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            byte[] cmdGetUPSDesc = ASCIIEncoding.ASCII.GetBytes("GET UPSDESC " + upsName + "\n");
            clientStream.Write(cmdGetUPSDesc, 0, cmdGetUPSDesc.Length);
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
                if (reply.StartsWith("UPSDESC " + upsName + " ") && reply.EndsWith("\n"))
                {
                    string[] varParts = reply.Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                    if (varParts[0] != "UPSDESC") continue;
                    if (varParts.Length != 3) continue;
                    if (varParts[1] != upsName) continue;

                    return varParts[2].Replace("\"", "").Trim();
                }
            }

            return string.Empty;
        }
        /// <summary>
        /// Gets the description of a specific variable on the specified UPS
        /// </summary>
        /// <param name="upsName">Name of the UPS</param>
        /// <param name="varName">Name of the variable (for example 'ups.status')</param>
        /// <returns></returns>
        public string GetUPSVarDescription(string upsName, string varName)
        {
            if (connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            byte[] cmdGetUPSVar = ASCIIEncoding.ASCII.GetBytes("GET DESC " + upsName + " " + varName + "\n");
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
                if (reply.StartsWith("ERR ") && reply.EndsWith("\n"))
                {
                    HandleUPSDError(reply);
                }
                if (reply.StartsWith("DESC " + upsName + " ") && reply.EndsWith("\n"))
                {
                    string[] varParts = reply.Split(new char[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                    if (varParts[0] != "DESC") continue;
                    if (varParts.Length != 4) continue;
                    if (varParts[1] != upsName) continue;
                    if (varParts[2] != varName) continue;

                    return varParts[3].Replace("\"", "").Trim();
                }
            }

            return string.Empty;
        }
        /// <summary>
        /// Gets the description of a specific instant command on the specified UPS
        /// </summary>
        /// <param name="upsName">Name of the UPS</param>
        /// <param name="cmdName">Name of the instant command (for example 'load.off')</param>
        /// <returns></returns>
        public string GetUPSCmdDescription(string upsName, string cmdName)
        {
            if (connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            byte[] cmdGetUPSVar = ASCIIEncoding.ASCII.GetBytes("GET CMDDESC " + upsName + " " + cmdName + "\n");
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
                if (reply.StartsWith("ERR ") && reply.EndsWith("\n"))
                {
                    HandleUPSDError(reply);
                }
                if (reply.StartsWith("CMDDESC " + upsName + " ") && reply.EndsWith("\n"))
                {
                    string[] cmdParts = reply.Split(new char[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                    if (cmdParts[0] != "CMDDESC") continue;
                    if (cmdParts.Length != 4) continue;
                    if (cmdParts[1] != upsName) continue;
                    if (cmdParts[2] != cmdName) continue;

                    return cmdParts[3].Replace("\"", "").Trim();
                }
            }

            return string.Empty;
        }
        /// <summary>
        /// Sets the Username for the connection that will be used for any command that require it
        /// </summary>
        /// <param name="authUser">Username to use</param>
        /// <returns>True if username is accepted, False if an error occurs</returns>
        public bool SetUsername(string authUser)
        {
            if (connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            byte[] cmdSetUsername = ASCIIEncoding.ASCII.GetBytes("USERNAME " + authUser + "\n");
            clientStream.Write(cmdSetUsername, 0, cmdSetUsername.Length);
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
                if (reply == "OK\n")
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Sets the Password for the connection that will be used for any command that require it
        /// </summary>
        /// <param name="authPass">Password to use</param>
        /// <returns>True if password is accepted, False if an error occurs</returns>
        public bool SetPassword(string authPass)
        {
            if (connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            byte[] cmdSetPassword = ASCIIEncoding.ASCII.GetBytes("PASSWORD " + authPass + "\n");
            clientStream.Write(cmdSetPassword, 0, cmdSetPassword.Length);
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
                if (reply == "OK\n")
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Set the value of a writable variable on the specified UPS
        /// </summary>
        /// <param name="upsName">Name of the UPS</param>
        /// <param name="varName">Name of the variable (for example 'ups.id')</param>
        /// <param name="newValue">Value to set to</param>
        /// <returns>True if the variable change is accepted, False if an error occurs</returns>
        public bool SetUPSVariable(string upsName, string varName, string newValue)
        {
            if (connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            byte[] cmdSetVariable = ASCIIEncoding.ASCII.GetBytes("SET VAR " + upsName + " " + varName + " \"" + newValue + "\"\n");
            clientStream.Write(cmdSetVariable, 0, cmdSetVariable.Length);
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
                if (reply == "OK\n")
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Notify the upsd server that this machine is drawing power from the specified UPS.
        /// </summary>
        /// <param name="upsName">Name of the UPS</param>
        /// <returns>True if successful, or False if an error occured</returns>
        public bool Login(string upsName)
        {
            if (connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            byte[] cmdLoginUPS = ASCIIEncoding.ASCII.GetBytes("LOGIN " + upsName + "\n");
            clientStream.Write(cmdLoginUPS, 0, cmdLoginUPS.Length);
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
                if (reply == "OK\n")
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Notify the upsd server that we are disconnecting
        /// </summary>
        /// <returns>True if successful, or False if an error occured</returns>
        public bool Logout()
        {
            if (connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            byte[] cmdLogoutUPS = ASCIIEncoding.ASCII.GetBytes("LOGOUT\n");
            clientStream.Write(cmdLogoutUPS, 0, cmdLogoutUPS.Length);
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
                if ((reply == "OK Goodbye\n") || (reply == "Goodbye...\n"))
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Perform and instant command on the specified UPS
        /// </summary>
        /// <param name="upsName">Name of the UPS</param>
        /// <param name="command">Instant Command to execute</param>
        /// <returns>True if the instant command is accepted, False if an error occurs</returns>
        public bool InstantCommand(string upsName, string command)
        {
            return InstantCommand(upsName, command, null);
        }
        /// <summary>
        /// Perform and instant command on the specified UPS
        /// </summary>
        /// <param name="upsName">Name of the UPS</param>
        /// <param name="command">Instant Command to execute</param>
        /// <param name="addParam">(Optional)Additional parameter to the instant command</param>
        /// <returns>True if the instant command is accepted, False if an error occurs</returns>
        public bool InstantCommand(string upsName, string command, string addParam)
        {
            if (connected == false)
            {
                throw new Exception("You have to connect by calling Connect() first!");
            }

            string cmd = "INSTCMD " + upsName + " " + command;
            if(String.IsNullOrEmpty(addParam) == false)
            {
                cmd += " \"" + addParam + "\"";
            }
            cmd += "\n";

            byte[] cmdInstantCommand = ASCIIEncoding.ASCII.GetBytes(cmd);
            clientStream.Write(cmdInstantCommand, 0, cmdInstantCommand.Length);
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
                if (reply == "OK\n")
                {
                    return true;
                }
            }

            return false;
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
            if (Enum.TryParse<UPSException.ErrorCode>(errParts[1].Replace('-', '_'), out code) == false)
            {
                throw new UPSException(UPSException.ErrorCode.UNKNOWN_ERROR, errParts[1] + " is not a recognized UPSD Error Code");
            }
            if (errParts.Length == 3)
            {
                throw new UPSException(code, errParts[2]);
            }

            throw new UPSException(code, string.Empty);
        }
    }
}
