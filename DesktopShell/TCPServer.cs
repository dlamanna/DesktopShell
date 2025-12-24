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
    private readonly Thread? listenThread;
    private readonly int portNum;
    private TcpClient tcpClient;
    private NetworkStream clientStream;
    private readonly CancellationTokenSource cts = new();
    private readonly CancellationToken token;

    public TCPServer()
    {
        string hostName = Dns.GetHostName().Trim().ToLower();
        token = cts.Token;

        foreach (KeyValuePair<string, string> hostPair in GlobalVar.hostList)
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
            }
        }

        try
        {
            tcplistener = new TcpListener(IPAddress.Any, portNum);
            listenThread = new Thread(new ThreadStart(ListenForClients));
            {
                listenThread.Start();
            }
        }
        catch (Exception e)
        {
            GlobalVar.ToolTip("Server Error", $"TCPServer::TCPServer() {e.Message}");
        }
    }

    private void ListenForClients()
    {
        if (tcplistener != null)
        {
            tcplistener.Start();
            Task task = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        tcpClient = await tcplistener.AcceptTcpClientAsync().ConfigureAwait(false);
                        Thread clientThread = new(new ParameterizedThreadStart(HandleClientComm));
                        clientThread.Start(tcpClient);
                        await Task.Delay(300);
                    }
                    catch (Exception e)
                    {
                        GlobalVar.Log($"### TCPServer::ListenForClients() - {e.Message}");
                    }
                }
            }, token);
        }
    }

    private static string TrimPassPhrase(string inString)
    {
        if (inString.Contains(GlobalVar.passPhrase))
        {
            int startIndex = inString.IndexOf(GlobalVar.passPhrase) + GlobalVar.passPhrase.Length;
            return inString[startIndex..];
        }
        else
        {
            return $"spike{inString}";
        }
    }

    private string ReadStream()
    {
        int bytesRead;
        byte[] message = new byte[4096];
        string receivedString = "";

        do
        {
            GlobalVar.Log($"@@@ ReadStream()");
            try
            {
                if (clientStream.CanRead && clientStream.DataAvailable)
                {
                    bytesRead = clientStream.Read(message, 0, 4096);
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
            Thread.Sleep(100);
        } while (receivedString.Length <= 1 && tcpClient.Connected);

        return receivedString;
    }

    private void HandleClientComm(object client)
    {
        SoundPlayer myPlayer = new();
        string soundLocation = $"{GlobalVar.GetSoundFolderLocation()}\\remote.wav";
        tcpClient = (TcpClient)client;
        clientStream = tcpClient.GetStream();
        bool isCommunicationOver = false;

        do
        {
            string receivedString = ReadStream();
            GlobalVar.Log($"@@@ HandleClientComm()");
            if (receivedString.Equals(""))
            {
                Thread.Sleep(500);
                continue;
            }
            else
            {
                switch (receivedString)
                {
                    case "ack":
                        try
                        {
                            if (GlobalVar.isWindows)
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
                        GlobalVar.shellInstance?.ProcessCommand(receivedString);
                        isCommunicationOver = true;
                        break;
                }
            }
        } while (!isCommunicationOver || !tcpClient.Connected);

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
