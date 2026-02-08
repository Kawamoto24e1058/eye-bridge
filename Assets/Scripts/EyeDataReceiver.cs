using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace EyeTrackingGame.Runtime
{
    // PRIORITY: Run before default scripts to latch input before PlayerController.Update()
    [DefaultExecutionOrder(-100)]
    public class EyeDataReceiver : MonoBehaviour
    {
        [Header("Settings")]
        public int port = 12345;
        public EyePlayerController playerController;
        public float autoStartSpeed = 3.0f;

        private UdpClient _udpClient;
        private Thread _receiveThread;
        private bool _isRunning;
        private bool _hasStartedGame = false;

        // --- Latch / Buffer Variables (Thread Shared) ---
        // Volatile to ensure visibility across threads
        private volatile float _latestGazeX = 0f;
        private volatile bool _latchedLeftClosed = false;
        private volatile bool _latchedRightClosed = false;
        private volatile bool _newDataAvailable = false;

        [System.Serializable]
        public class EyeData
        {
            public float gazeX;
            public bool isLeftClosed;
            public bool isRightClosed;
        }

        private void Start()
        {
            if (playerController == null)
            {
                playerController = GetComponent<EyePlayerController>();
            }
            StartReceiver();
        }

        private void StartReceiver()
        {
            _udpClient = new UdpClient(port);
            // Optimization: Set socket buffer if needed, but defaults are usually fine for small packets.
            _udpClient.Client.ReceiveBufferSize = 1024; // Small buffer to encourage dropping old if full? No, we want to read fast.

            _isRunning = true;
            _receiveThread = new Thread(ReceiveData);
            _receiveThread.IsBackground = true;
            _receiveThread.Priority = System.Threading.ThreadPriority.AboveNormal; // High priority thread
            _receiveThread.Start();
            Debug.Log($"UDP Receiver started on port {port} (High Priority)");
        }

        private void ReceiveData()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
            while (_isRunning)
            {
                try
                {
                    // Block until data available
                    byte[] data = _udpClient.Receive(ref endPoint);
                    
                    // Decode
                    string json = Encoding.UTF8.GetString(data);
                    EyeData parsed = JsonUtility.FromJson<EyeData>(json);

                    // --- Latching Logic ---
                    // 1. Gaze: Always take the LATEST value (overwrite)
                    _latestGazeX = parsed.gazeX;

                    // 2. Winks: LATCH 'true' values. 
                    // If multiple packets come per frame, and ANY of them is true, we want to know.
                    // We only reset these to false in Update() after consuming them.
                    if (parsed.isLeftClosed) _latchedLeftClosed = true;
                    if (parsed.isRightClosed) _latchedRightClosed = true;

                    _newDataAvailable = true;
                }
                catch (System.Exception e)
                {
                    if (_isRunning) Debug.LogError($"UDP Error: {e.Message}");
                }
            }
        }

        private void Update()
        {
            if (_newDataAvailable)
            {
                // Auto-Start
                if (!_hasStartedGame)
                {
                    _hasStartedGame = true;
                    SetForwardSpeed(autoStartSpeed);
                    Debug.Log("Game Auto-Started via Eye Tracking!");
                }

                if (playerController != null)
                {
                    // Apply Data
                    playerController.gazeX = _latestGazeX;
                    
                    // Apply Latched Inputs
                    playerController.isLeftEyeClosed = _latchedLeftClosed;
                    playerController.isRightEyeClosed = _latchedRightClosed;

                    // Message Handling: "Reset Latch"
                    // The trick: We applied "True" if it was ever True.
                    // But if we reset it to False now, we depend on the next packet to set it True again.
                    // If the user is holding the wink, the next packet will come and set it True.
                    // If the user released, the next packet will be False, and it stays False.
                    // This mechanism catches "Fast Winks" that happened entirely between Updates.
                    
                    _latchedLeftClosed = false;
                    _latchedRightClosed = false;
                }
                
                // Note: We don't set _newDataAvailable = false here because 
                // the thread keeps writing. But effectively we acted on the latest state.
            }
        }

        private void SetForwardSpeed(float speed)
        {
            if (playerController != null)
            {
                try
                {
                    var field = typeof(EyePlayerController).GetField("forwardSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null) field.SetValue(playerController, speed);
                } 
                catch (System.Exception e) { Debug.LogError("Failed to set speed: " + e.Message); }
            }
        }

        private void OnDestroy()
        {
            _isRunning = false;
            if (_udpClient != null) _udpClient.Close();
            if (_receiveThread != null && _receiveThread.IsAlive) _receiveThread.Abort();
        }
    }
}
