using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// PRIORITY: Run before default scripts to latch input before PlayerController.Update()
[DefaultExecutionOrder(-100)]
public class EyeDataReceiver : MonoBehaviour
{
    [Header("Settings")]
    public int port = 12345;
    
    [Header("Reference")]
    [Tooltip("Target Player Controller to control. Will auto-find if empty.")]
    public PlayerController targetController; 

    private UdpClient _udpClient;
    private Thread _receiveThread;
    private bool _isRunning;

    // --- Latch / Buffer Variables (Thread Shared) ---
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
        // Auto-detect PlayerController if not assigned
        if (targetController == null)
        {
            targetController = GetComponent<PlayerController>();
            if (targetController == null)
            {
                // Updated: Using FindFirstObjectByType instead of Obsolete FindObjectOfType
                targetController = FindFirstObjectByType<PlayerController>();
            }
            
            if (targetController != null)
            {
                Debug.Log("EyeDataReceiver: Auto-Connected to PlayerController.");
            }
            else
            {
                Debug.LogError("EyeDataReceiver: PlayerController NOT found in scene!");
            }
        }
        
        StartReceiver();
    }

    private void StartReceiver()
    {
        try
        {
            _udpClient = new UdpClient(port);
            _udpClient.Client.ReceiveBufferSize = 1024;

            _isRunning = true;
            _receiveThread = new Thread(ReceiveData);
            _receiveThread.IsBackground = true;
            _receiveThread.Priority = System.Threading.ThreadPriority.AboveNormal; 
            _receiveThread.Start();
            Debug.Log($"UDP Receiver started on port {port} (High Priority)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to start UDP Receiver: {e.Message}");
        }
    }

    private void ReceiveData()
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
        while (_isRunning)
        {
            try
            {
                byte[] data = _udpClient.Receive(ref endPoint);
                string json = Encoding.UTF8.GetString(data);
                EyeData parsed = JsonUtility.FromJson<EyeData>(json);

                _latestGazeX = parsed.gazeX;
                _latestGazeY = parsed.gazeY; // Added
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
            // Removed Text UI update to avoid dependency error
            
            if (targetController != null)
            {
                targetController.gazeX = _latestGazeX;
                targetController.gazeY = _latestGazeY; // Apply Y
                targetController.isLeftEyeClosed = _latchedLeftClosed;
                targetController.isRightEyeClosed = _latchedRightClosed;
                
                // Notify Watchdog
                targetController.NotifyInputReceived();

                _latchedLeftClosed = false;
                _latchedRightClosed = false;
            }
            _newDataAvailable = false; // Reset after processing
        }
    }

    private void OnGUI()
    {
        if (showDebugOnScreen)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.yellow;
            
            string debugMsg = $"UDP: {System.DateTime.Now:HH:mm:ss}\n" +
                              $"X: {_latestGazeX:F2} Y: {_latestGazeY:F2}\n" +
                              $"L: {_latchedLeftClosed} R: {_latchedRightClosed}";
                              
            GUI.Label(new Rect(20, 20, 400, 200), debugMsg, style);
        }
    }

    private void OnDestroy()
    {
        _isRunning = false;
        if (_udpClient != null) _udpClient.Close();
        // Removed obsolete Thread.Abort(). Thread terminates via Exception or Loop Exit.
    }
}
