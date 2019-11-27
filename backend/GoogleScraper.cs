using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Net.Security;
using System.IO;

namespace GoogleScraper
{
    #region "StateObjects"
    // State object for receiving data from web server.
    public class StateObject
    {
        // Client socket.
        public Socket workSocket = null;

        // Size of receive buffer.
        public const int nBufferSize = 256;

        // Receive buffer.
        public byte[] buffer = new byte[nBufferSize];

        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }


    // State object for receiving data from web server in ssl mode.
    public class StateObjectSSL
    {
        // Client ssl stream.
        public SslStream workStream = null;

        // Size of receive buffer.
        public const int nBufferSize = 1024;

        // Receive buffer.
        public byte[] buffer = new byte[nBufferSize];

        public StringBuilder sb = new StringBuilder();

    }

    #endregion

    /// <summary>
    /// Http Socket is used to handle all request response to server with raw data 
    /// </summary>
    public class HttpSocket
    {

        #region "Private Members"

        // The port number for the remote device.
        private int nPort = 80;

        // ManualResetEvent instances signal completion.
        private ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // The response from the remote device.
        private String response = String.Empty;

        // ReadData and buffer holds the data read from the server.
        // They is used by the ReadCallback method.
        StringBuilder strReadData = new StringBuilder();

        byte[] buffer = new byte[2048];

        // Hold uri object for requested url 
        private Uri uHost { get; set; }

        // True if request is secured i.e Https
        private bool IsHttps { get; set; }

        // True if request goes via proxy
        private bool IsProxy { get; set; }

        // IpAddress object to hold ipaddress
        private IPAddress oIpAddress = null;

        //True if proxy used is IPv6
        private bool IsIpv6 { get; set; }

        //Request string text
        private string strRequest = String.Empty;

        // try variable used when request is in redirect mode  
        private int nTry = 0;

        #endregion

        #region "Public Members"

        // Public member used to set proxy after creating object of this class
        public string Proxy = String.Empty;

        #endregion

        #region "Public Method"

        /// <summary>
        /// Public method used to get html data via url host
        /// </summary>
        /// <param name="uhost">request host</param>
        /// <returns>html string</returns>
        public string GetHtml(Uri uhost)
        {
            // Set host Uri object
            uHost = uhost;

            // Set scheme Http/Https
            SetScheme(uhost);

            //If proxy null
            if (String.IsNullOrEmpty(Proxy))
                //Set address if proxy is null
                SetAddress(uhost);
            else
                // Set proxy if any
                SetProxy(uhost, Proxy);

            try
            {
                // Connect to a remote device.
                Socket oSocket = ConnectSocket(oIpAddress, nPort);

                //If request is in https and proxy mode
                if (IsHttps && IsProxy)
                {
                    //Connect to proxy
                    ConnectProxy(oSocket);

                    //Retrieve Data via ssl stream
                    RetreiveSSLData(oSocket);

                }
                else if (IsHttps && !IsProxy)  // if request is in https mode and proxy is null
                {
                    //Retrieve Data via ssl strea
                    RetreiveSSLData(oSocket);

                }
                else // if reuest if not in https mode neither in proxy mode
                {
                    RetreiveData(oSocket);
                }

                // Get redirect url if retreive response contains Http 301/302
                string strRequestUrl = GetRedirectUrl(response);

                // Release the socket.
                oSocket.Shutdown(SocketShutdown.Both);

                //Close the socket
                oSocket.Close();

                //Reset data so that request can preocces in redirect mode if any
                ResetData();

                // Checks if request url contains data than process Gethtml method again we need to connect to 
                // socket again because socket and ip can be change during redirect
                if (!String.IsNullOrEmpty(strRequestUrl) && nTry == 1)
                {
                    GetHtml(new Uri(strRequestUrl));
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return response;
        }


        #endregion

        #region "Private Methods"

        /// <summary>
        /// Used to connect socket with proxy when request is in https mode
        /// </summary>
        /// <param name="oSocK">Socket object</param>
        /// <returns>Returns true or false</returns>
        private bool ConnectProxy(Socket oSocK)
        {
            string strConnectMsg = "CONNECT  " + uHost.Host + ":443 HTTP/1.1\r\n\r\n";

            //Convert text message to bytes
            byte[] bymsg = System.Text.Encoding.ASCII.GetBytes(strConnectMsg);

            // Send message to server via proxy
            oSocK.Send(bymsg);

            string strProxyConnectionResponseHeader = String.Empty;

            // Do while response stream contains end characters i.e \r\n\r\n
            while (!strProxyConnectionResponseHeader.Contains("\r\n\r\n"))
            {
                //read the header byte by byte, until \r\n\r\n
                byte[] buffer = new byte[1];

                // Recieve bytes from server
                oSocK.Receive(buffer, 0, 1, 0);

                //Decode bytes to string
                strProxyConnectionResponseHeader += Encoding.ASCII.GetString(buffer);
            }

            // Check if string doesnt contains 200 connection extablished than returns false
            if (!strProxyConnectionResponseHeader.Contains("200 Connection established"))
                return false;
            else
                return true;

        }

        /// <summary>
        /// Used to set wheather a request if https or http according to that it sets port
        /// </summary>
        /// <param name="uri">Requested uri</param>
        private void SetScheme(Uri uri)
        {
            // Set host uri to Host object
            uHost = uri;

            // Checks weather reguest is http or https sets port accordingly
            if (uri.Scheme == "http")
                nPort = 80;

            else if (uri.Scheme == "https")
            {
                nPort = 443;
                // Set Ishttps true 
                IsHttps = true;
            }
        }

        /// <summary>
        /// Used to resolve dns and set the ipaddress to ipaddress object and set the request url if request id from not proxy mode
        /// </summary>
        /// <param name="uri">Requested url</param>
        public void SetAddress(Uri uri)
        {
            // Resolve DNS and gets the list of IPs
            IPHostEntry ipHostInfo = Dns.Resolve(uri.Host);

            // Set the first IP to Ipaddress object
            oIpAddress = ipHostInfo.AddressList[0];

            // Set Request string in non proxy mode and its important to make connection close 
            strRequest = "GET " + uri.PathAndQuery + " HTTP/1.1\r\nHost: " + uri.Host + "\r\n" +
                              "User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64; rv:41.0) Gecko/20100101 Firefox/41.0\r\n" +
                              "Connection: close\r\n" +
                              "\r\n";
        }

        /// <summary>
        /// Used to set the proxy IPv6 or IPv4 and Ipaddress of proxy in OIpAddress object and set the request Url
        /// </summary>
        /// <param name="uri">Request Uri</param>
        /// <param name="strproxy">Proxy</param>
        private void SetProxy(Uri uri, string strproxy)
        {
            // Set isProxy true
            IsProxy = true;

            string strProxy = String.Empty;

            string strPort = String.Empty;

            // checks the last index of : as IPv6 proxies contain more than 1 :
            int index = strproxy.LastIndexOf(":");

            // Check weather proxy is Ipv6
            if (strproxy.Split(':').Length > 2)
                IsIpv6 = true;

            // Seperate proxy and port from proxy string
            if (index > 0)
            {
                strProxy = strproxy.Substring(0, index);
                strPort = strproxy.Substring(index + 1);
            }
            // Asign the extracted text
            oIpAddress = IPAddress.Parse(strProxy);
            nPort = Convert.ToInt16(strPort);

            // Create requested header according to proxy
            strRequest = "GET " + uri.AbsoluteUri + " HTTP/1.1\r\nAccept: */*\r\nHost: " + uri.Host + "\r\nProxy-Connection: Close\r\n" +
                          "User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64; rv:41.0) Gecko/20100101 Firefox/41.0\r\n" +
                          "Connection: close\r\n" +
                          "\r\n";

        }

        /// <summary>
        /// Used to retrieve data in Https or SSL mode
        /// </summary>
        /// <param name="objSock">Socket object</param>
        private void RetreiveSSLData(Socket objSock)
        {
            // Netoerk stream
            NetworkStream nStream = new NetworkStream(objSock, true);

            //// Wrap in SSL stream.SSl stream used to add ssl headres to request
            SslStream ssStream = new SslStream(nStream);

            //Authenitcate host with stream
            ssStream.AuthenticateAsClient(uHost.Host);

            // Convert reuest tring to bytes.
            byte[] messsage = Encoding.UTF8.GetBytes(strRequest);


            // Create the state object in ssl mode
            StateObjectSSL state = new StateObjectSSL();

            state.workStream = ssStream;
            state.buffer = messsage;

            // Send data to server
            ssStream.BeginWrite(messsage, 0, messsage.Length, new AsyncCallback(WriteCallback), state);

            // Wait for asyn send to complete
            receiveDone.WaitOne();
        }

        /// <summary>
        /// Connect Tcp/Ip socket with Ipaddress and port and returns socket object
        /// </summary>
        /// <param name="oIpAddress">Ipadress object</param>
        /// <param name="nPort">port numner</param>
        /// <returns>Socket object</returns>
        private Socket ConnectSocket(IPAddress oIpAddress, int nPort)
        {
            // Create a TCP/IP socket.
            Socket client = new Socket(oIpAddress.AddressFamily,
                  SocketType.Stream, ProtocolType.Tcp);


            // Connect to the remote endpoint.
            client.BeginConnect(oIpAddress, nPort,
                new AsyncCallback(ConnectCallback), client);
            connectDone.WaitOne();

            return client;

        }

        /// <summary>
        /// Used to get data in non ssl and non proxy mode
        /// </summary>
        /// <param name="objSocket">Socket object</param>
        private void RetreiveData(Socket objSocket)
        {
            // Send data to the remote device.
            Send(objSocket, strRequest);

            //Wait till send async is complete
            sendDone.WaitOne();

            // Receive the response from the remote device.
            Receive(objSocket);

            // Wait till recieve async is complete
            receiveDone.WaitOne();
        }

        /// <summary>
        /// Method used to reset data for auto redirect url
        /// </summary>
        private void ResetData()
        {
            //Reset connection done
            connectDone.Reset();

            //Reset Snd done
            sendDone.Reset();

            //Reste receive done
            receiveDone.Reset();

            //Clear read data
            strReadData.Clear();

            //Clear buffer
            buffer = new byte[2048];

            nTry++;
        }

        /// <summary>
        /// Used to parse the request header and check 301 and 302 Htps protocl 
        /// if present than find Location header parse it and get the redirect url 
        /// </summary>
        /// <param name="strresonse">Response html text</param>
        /// <returns>Return redirect url if any</returns>
        private string GetRedirectUrl(string strresonse)
        {
            int i = 0;

            bool IsRedirectUrl = false;

            string strLocation = "";

            //Parse Headers line by line and checks 301 and 302
            using (StringReader reader = new StringReader(strresonse))
            {
                string line = string.Empty;
                do
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        if ((line.StartsWith("HTTP/1.1 302") || line.StartsWith("HTTP/1.1 301")) && i == 0)
                        {
                            IsRedirectUrl = true;
                        }
                        else if (i == 0)
                        {
                            //break if first line doesnt contain 301 and 302 headers
                            break;
                        }

                        // Parse the location header and get redirect url
                        if (IsRedirectUrl == true)
                        {
                            if ((line.StartsWith("Location:")))
                            {
                                strLocation = line.Substring(line.IndexOf(":") + 1).Trim();
                                break;
                            }
                        }
                        i++;
                    }

                } while (line != null);
            }

            return strLocation;

        }

        /// <summary>
        /// Used for Connect callbasck
        /// </summary>
        /// <param name="ar"></param>
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Recieve method to recieve data from remote server
        /// </summary>
        /// <param name="client">Socket object</param>
        private void Receive(Socket client)
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject();

                state.workSocket = client;

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.nBufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Recieve call back
        /// </summary>
        /// <param name="ar">Async Result</param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, StateObject.nBufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Send method used to send message to remote server
        /// </summary>
        /// <param name="client">Socket object</param>
        /// <param name="data">string data to be send</param>
        private void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        /// <summary>
        /// Send callback
        /// </summary>
        /// <param name="ar">IAsyncResult callbacl</param>
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                //Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Write call bacl
        /// </summary>
        /// <param name="ar"></param>
        private void WriteCallback(IAsyncResult ar)
        {
            StateObjectSSL state = (StateObjectSSL)ar.AsyncState;
            SslStream stream = state.workStream;
            try
            {
                //Console.WriteLine("Writing data to the server.");
                stream.EndWrite(ar);


                // Asynchronously read a message from the server.
                stream.BeginRead(buffer, 0, buffer.Length,
                    new AsyncCallback(ReadCallback),
                    state);

                // Signal that all bytes have been sent.

            }
            catch (Exception writeException)
            {
                //e = writeException;
                //complete = true;
                return;
            }
        }

        /// <summary>
        /// Read call back
        /// </summary>
        /// <param name="ar"></param>
        private void ReadCallback(IAsyncResult ar)
        {
            // Read the  message sent by the server.
            // The end of the message is signaled using the
            // "<EOF>" marker.
            StateObjectSSL state = (StateObjectSSL)ar.AsyncState;

            SslStream stream = state.workStream;

            //SslStream stream = (SslStream)ar.AsyncState;
            int byteCount = -1;
            try
            {
                //Console.WriteLine("Reading data from the server.");
                byteCount = stream.EndRead(ar);
                // Use Decoder class to convert from bytes to UTF8
                // in case a character spans two buffers.
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, byteCount)];
                decoder.GetChars(buffer, 0, byteCount, chars, 0);
                strReadData.Append(chars);
                // Check for EOF or an empty message.
                if (strReadData.ToString().IndexOf("<EOF>") == -1 && byteCount != 0)
                {
                    // We are not finished reading.
                    // Asynchronously read more message data from  the server.
                    stream.BeginRead(buffer, 0, buffer.Length,
                        new AsyncCallback(ReadCallback),
                        state);
                }
                else
                {
                    response = strReadData.ToString();

                    receiveDone.Set();
                    // Console.WriteLine("Message from the server: {0}", readData.ToString());
                }
            }
            catch (Exception readException)
            {
                // e = readException;
                //complete = true;
                return;
            }
            //complete = true;
        }

        #endregion
    }
}