using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopShell;

public class TCPServer
{
    private readonly TcpListener? tcplistener;
    private readonly int portNum;
    private readonly CancellationTokenSource cts = new();
    private readonly CancellationToken token;

    public TCPServer()
    {
        string hostName = Dns.GetHostName().Trim().ToLower();
        token = cts.Token;

        bool foundHostPort = false;

        foreach (KeyValuePair<string, string> hostPair in GlobalVar.HostList)
        {
            string tempHostName = hostPair.Key.Trim().ToLower();
            if (tempHostName.Equals(hostName))
            {
                GlobalVar.Log($"^^^ Starting server on port: {hostPair.Value}");
                if (!int.TryParse(hostPair.Value, out portNum))
                {
                    GlobalVar.Log($"### Error trying to parse port number as int: {hostPair.Value}");
                    return;
                }

                foundHostPort = true;
                break;
            }
        }

        if (!foundHostPort || portNum <= 0)
        {
            GlobalVar.Log($"### TCPServer not started: host '{hostName}' missing/invalid port in hostlist.txt");
            return;
        }

        try
        {
            tcplistener = new TcpListener(IPAddress.Any, portNum);
            var listenThread = new Thread(ListenForClients);
            listenThread.IsBackground = true;
            listenThread.Start();
        }
        catch (Exception e)
        {
            GlobalVar.ToolTip("Server Error", $"TCPServer::TCPServer() {e.Message}");
        }
    }

    private void ListenForClients()
    {
        if (tcplistener == null)
        {
            return;
        }

        try
        {
            tcplistener.Start();

            while (!token.IsCancellationRequested)
            {
                TcpClient? acceptedClient = null;
                try
                {
                    acceptedClient = tcplistener.AcceptTcpClient();
                    var clientThread = new Thread(HandleClientComm);
                    clientThread.IsBackground = true;
                    clientThread.Start(acceptedClient);
                }
                catch (SocketException) when (token.IsCancellationRequested)
                {
                    // Listener was stopped during shutdown.
                    return;
                }
                catch (ObjectDisposedException) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception e)
                {
                    GlobalVar.Log($"### TCPServer::ListenForClients() - {e.Message}");
                    try
                    {
                        acceptedClient?.Close();
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }
        catch (Exception e)
        {
            GlobalVar.Log($"### TCPServer::ListenForClients() fatal - {e.Message}");
        }
    }

    private static string TrimPassPhrase(string inString)
    {
        if (string.IsNullOrWhiteSpace(inString))
        {
            return "";
        }

        // Require the passphrase at the start of the message (prevents embedding it later in the payload).
        if (inString.StartsWith(GlobalVar.PassPhrase, StringComparison.Ordinal))
        {
            return inString[GlobalVar.PassPhrase.Length..];
        }

        return $"spike{inString}";
    }

    private static string ReadStream(TcpClient tcpClient, NetworkStream clientStream)
    {
        int bytesRead;
        byte[] message = new byte[GlobalVar.TcpBufferSize];
        string receivedString = "";

        do
        {
            GlobalVar.Log($"@@@ ReadStream()");
            try
            {
                if (clientStream.CanRead && clientStream.DataAvailable)
                {
                    bytesRead = clientStream.Read(message, 0, GlobalVar.TcpBufferSize);
                    if (bytesRead <= 0)
                    {
                        return "";
                    }
                    ASCIIEncoding encoder = new();
                    receivedString = TrimPassPhrase(encoder.GetString(message, 0, bytesRead).Trim());
                    GlobalVar.Log($"$$$ TCPServer::ReadStream() - {receivedString}");
                    return receivedString;
                }
            }
            catch (Exception e)
            {
                GlobalVar.Log($"### TCPServer::ReadStream() - {e.Message}");
                return "";
            }
            Thread.Sleep(GlobalVar.TcpReadDelayMs);
        } while (receivedString.Length <= 1 && tcpClient.Connected);

        return receivedString;
    }

    private void HandleClientComm(object client)
    {
        SoundPlayer myPlayer = new();
        string soundLocation = $"{GlobalVar.GetSoundFolderLocation()}\\remote.wav";
        var tcpClient = (TcpClient)client;

        using NetworkStream clientStream = tcpClient.GetStream();
        bool isCommunicationOver = false;

        do
        {
            string receivedString = ReadStream(tcpClient, clientStream);
            GlobalVar.Log($"@@@ HandleClientComm()");
            if (receivedString.Equals(""))
            {
                Thread.Sleep(GlobalVar.WebBrowserLaunchDelayMs);
                continue;
            }
            else
            {
                switch (receivedString)
                {
                    case "ack":
                        try
                        {
                            if (GlobalVar.IsWindows)
                            {
                                using (myPlayer = new SoundPlayer(soundLocation))
                                {
                                    myPlayer.PlaySync();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (e.GetType() == typeof(FileNotFoundException))
                            {
                                GlobalVar.Log($"### Error playing sound at location: {myPlayer.SoundLocation}");
                            }
                            else
                            {
                                GlobalVar.Log($"### Error playing sound: {e.Message}");
                            }
                        }
                        GlobalVar.Log("$$$ Ack received, closing connection");
                        isCommunicationOver = true;
                        break;
                    case "ringdoorbell":
                        ///TODO: Add ring doorbell functionality here to pull up video stream or snapshot
                        GlobalVar.ToolTip("Ring", "");
                        GlobalVar.SendRemoteCommand(tcpClient, "ack\r\n");
                        break;
                    case string a when a.Contains("spike"):
                        int idx = receivedString.IndexOf("spike", StringComparison.OrdinalIgnoreCase) + "spike".Length;
                        GlobalVar.Log($"$$$ Got fake command: {receivedString[idx..]}");
                        GlobalVar.SendRemoteCommand(tcpClient, "lol\r\n");
                        break;
                    default:
                        //GlobalVar.SendRemoteCommand(tcpClient, "ack\r\n");
                        GlobalVar.ShellInstance?.ProcessCommand(receivedString);
                        isCommunicationOver = true;
                        break;
                }
            }
        } while (!isCommunicationOver && tcpClient.Connected);

        if (tcpClient != null)
        {
            GlobalVar.Log("@@@ TCPServer::HandleClientComm - Closing tcpClient");
            tcpClient.Close();
        }
    }

    public void CloseServer()
    {
        cts.Cancel();
        tcplistener?.Stop();
    }

    ~TCPServer()
    {
        cts.Cancel();
        tcplistener?.Stop();
    }
}
