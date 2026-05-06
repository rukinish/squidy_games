using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UDPReceiver : MonoBehaviour
{
    private static readonly bool VERBOSE_UDP_LOGS = false;
    public int port = 5065;

    // Global gaze state
    public static float GazeX { get; private set; } = 0.5f;
    public static float GazeY { get; private set; } = 0.5f;
    public static bool IsReceiving { get; private set; } = false;

    // Validation / Logging Metrics
    public static bool FaceFound { get; private set; } = false;
    public static float PythonInfTime { get; private set; } = 0f;
    public static double RemoteTimestamp { get; private set; } = 0.0;

    // Packet Tracking Metadata
    public static int LastSequenceID { get; private set; } = -1;
    public static int DroppedPackets { get; private set; } = 0;
    public static int InvalidChecksums { get; private set; } = 0;
    public static int PacketsReceived { get; private set; } = 0;

    // Derived Performance Metrics
    public static double NetworkTransitLatencyMs { get; private set; } = 0.0;
    public static float DroppedFrameRate => (PacketsReceived + DroppedPackets) > 0 ? (float)DroppedPackets / (PacketsReceived + DroppedPackets) : 0f;

    private Thread receiveThread;
    private UdpClient client;
    private bool running = true;
    private readonly object shutdownLock = new object();

    public static UDPReceiver instance;
    private bool hasLoggedConnection = false;

    private static void UdpLog(string message)
    {
        if (VERBOSE_UDP_LOGS) Debug.Log(message);
    }

    private static void UdpWarn(string message)
    {
        if (VERBOSE_UDP_LOGS) Debug.LogWarning(message);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        if (instance == this)
        {
            UdpLog("[1.2 UNITY CONNECTION] UDPReceiver starting. Binding to port " + port + "...");
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();
            UdpLog("[1.2 UNITY CONNECTION] Receive thread started. Waiting for gaze stream from Python server.");
        }
    }

    void OnDestroy()
    {
        ShutdownReceiver();
    }

    void OnApplicationQuit()
    {
        ShutdownReceiver();
    }

    private void ShutdownReceiver()
    {
        lock (shutdownLock)
        {
            if (!running) return;
            running = false;
            try
            {
                client?.Close(); // unblock Receive()
            }
            catch
            {
            }

            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(200);
            }
        }
    }

    private void ReceiveData()
    {
        client = new UdpClient(port);
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
        UdpLog("[1.2 UNITY CONNECTION] UDP socket bound to port " + port + ". Listening for gaze packets.");

        while (running)
        {
            try
            {
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);

                if (data.Length < 135)
                {
                    UdpWarn($"[UDP SHORT PACKET] len={data.Length}B | raw_json={text}");
                }

                GazePacket packet = JsonUtility.FromJson<GazePacket>(text);

                if (PacketsReceived % 60 == 0)
                {
                    UdpLog($"[UDP RAW PARSE] seq={packet.seq} | packet.x={packet.x:F6} | packet.y={packet.y:F6} | found={packet.found} | chk={packet.chk} | raw_json_len={data.Length}B");
                }

                // Check checksum
                int expectedChk = (packet.seq ^ (int)(packet.x * 100) ^ (int)(packet.y * 100)) & 0xFF;
                if (packet.chk != expectedChk)
                {
                    InvalidChecksums++;
                    UdpWarn($"[UDP] Invalid checksum! Expected {expectedChk}, got {packet.chk}");
                    continue;
                }

                // Calculate Transit Latency
                double currentTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
                NetworkTransitLatencyMs = (currentTs - packet.ts) * 1000.0;

                PacketsReceived++;

                // Check for packet drop
                if (LastSequenceID != -1 && packet.seq > LastSequenceID + 1)
                {
                    int dropCount = packet.seq - LastSequenceID - 1;
                    DroppedPackets += dropCount;
                }
                LastSequenceID = packet.seq;

                // Update metrics
                FaceFound = packet.found;
                RemoteTimestamp = packet.ts;
                PythonInfTime = packet.proc;

                if (FaceFound)
                {
                    GazeX = packet.x;         // raw — flipping handled in GazeMapper after calibration
                    GazeY = 1.0f - packet.y;  // Invert vertical — model outputs bottom-to-top

                    if (PacketsReceived % 60 == 0)
                        UdpLog($"[UDP ASSIGN]    GazeX={GazeX:F6}  GazeY={GazeY:F6}  (packet.x={packet.x:F6}  packet.y={packet.y:F6})");
                }

                if (!hasLoggedConnection)
                {
                    Debug.Log($"[UDP] Connected! Receiving Gaze Data from Python. (First Packet Size: {data.Length} bytes)");
                    UdpLog("[1.2 UNITY CONNECTION] Gaze stream connected. First packet received from Python server.");
                    UdpLog($"[1.3 UDP TRANSMISSION] Packet received | seq={packet.seq} | x={packet.x:F3} | y={packet.y:F3} | face_found={packet.found} | size={data.Length} bytes");
                    hasLoggedConnection = true;
                }

                IsReceiving = true;
            }
            catch (SocketException)
            {
                if (running) UdpWarn("UDP socket exception while receiving gaze packets.");
            }
            catch (ObjectDisposedException)
            {
                if (running) UdpWarn("UDP socket disposed while receiving gaze packets.");
            }
            catch (Exception e)
            {
                if (running) UdpWarn("UDP Error: " + e.Message);
            }
        }
    }

    [System.Serializable]
    public class GazePacket
    {
        public int seq;
        public float x;
        public float y;
        public bool found;
        public double ts;
        public float proc;
        public int chk;
    }

    public static void SendXAICommand(string activityType, string sessionId, string savePath = null, string bgPath = null)
    {
        Thread sendThread = new Thread(() => {
            try
            {
                using (UdpClient client = new UdpClient())
                {
                    string timestamp    = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                    string escapedSave  = string.IsNullOrEmpty(savePath) ? "" : savePath.Replace("\\", "\\\\");
                    string escapedBg    = string.IsNullOrEmpty(bgPath)   ? "" : bgPath.Replace("\\", "\\\\");

                    string json;
                    if (string.IsNullOrEmpty(savePath))
                    {
                        json = $"{{\"command\":\"ACTIVITY_END_XAI\", \"activity_type\":\"{activityType}\", \"session_id\":\"{sessionId}\", \"timestamp\":\"{timestamp}\"}}";
                    }
                    else if (string.IsNullOrEmpty(bgPath))
                    {
                        json = $"{{\"command\":\"ACTIVITY_END_XAI\", \"activity_type\":\"{activityType}\", \"session_id\":\"{sessionId}\", \"timestamp\":\"{timestamp}\", \"save_path\":\"{escapedSave}\"}}";
                    }
                    else
                    {
                        json = $"{{\"command\":\"ACTIVITY_END_XAI\", \"activity_type\":\"{activityType}\", \"session_id\":\"{sessionId}\", \"timestamp\":\"{timestamp}\", \"save_path\":\"{escapedSave}\", \"bg_path\":\"{escapedBg}\"}}";
                    }

                    byte[] data = Encoding.UTF8.GetBytes(json);

                    client.Send(data, data.Length, "127.0.0.1", 5066);
                }
            }
            catch (Exception e)
            {
                UdpWarn("[UDP Send Error - XAI] " + e.Message);
            }
        });

        sendThread.IsBackground = true;
        sendThread.Start();
    }
}
