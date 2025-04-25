using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;

public class FileReceiver : MonoBehaviour
{
    public Text statusText; // Text component to display status
    public Text ipAddressText; // Text component to display receiver's IP
    private const int CHUNK_SIZE = 1024 * 1024; // 1MB chunk size
    private TcpListener tcpListener; // Listener for incoming TCP connections
    private Thread listenerThread; // Thread for handling the listener without blocking the main thread

    void Start()
    {
        // Check if the UI Text elements are assigned in the Inspector
        if (statusText == null)
        {
            Debug.LogError("statusText is not assigned in the Inspector!");
        }
        if (ipAddressText == null)
        {
            Debug.LogError("ipAddressText is not assigned in the Inspector!");
        }

        // Display the receiver's IP address
        string localIP = GetLocalIPAddress();
        if (ipAddressText != null) // Ensure ipAddressText is assigned before using
        {
            ipAddressText.text = $"Receiver IP: {localIP}";
        }
    }

    // Get local IP address of the receiver device (this works on Android as well)
    string GetLocalIPAddress()
    {
        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString(); // Return the first IPv4 address
        }
        return "127.0.0.1"; // Default to localhost if no valid IP is found
    }

    public void StartReceivingFile()
    {
        if (statusText != null)
        {
            statusText.text = "Waiting for incoming connection..."; // Update UI before starting listening
        }

        // Start listening for incoming connections on TCP port 8080 in a separate thread
        listenerThread = new Thread(ListenForConnections);
        listenerThread.Start();
    }

    // Listener method to accept connections
    void ListenForConnections()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Any, 8081); // Listening on port 8080
            tcpListener.Start();

            // Update status on the main thread using UnityMainThreadDispatcher
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (statusText != null) // Ensure it's not null before updating
                {
                    statusText.text = "Waiting for incoming connection...";
                }
            });

            // Accept the client connection asynchronously
            TcpClient client = tcpListener.AcceptTcpClient();
            OnClientConnect(client); // Handle the client connection
        }
        catch (SocketException se)
        {
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (statusText != null) statusText.text = $"Connection Error: {se.Message}";
            });
            Debug.LogError(se.Message);
        }
        catch (Exception ex)
        {
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (statusText != null) statusText.text = $"Error: {ex.Message}";
            });
            Debug.LogError(ex.Message);
        }
    }

    // Callback method to handle the client connection
    void OnClientConnect(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();

            // The first 8 bytes of the stream can be used to send the total file size (you can implement this on the sender side)
            byte[] sizeBuffer = new byte[8];
            stream.Read(sizeBuffer, 0, 8); // Read the first 8 bytes to get the file size
            long totalFileSize = BitConverter.ToInt64(sizeBuffer, 0);  // Convert the first 8 bytes to file size

            // Save the incoming file
            string savePath = Path.Combine(Application.persistentDataPath, "received_file");
            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                byte[] buffer = new byte[CHUNK_SIZE];
                int bytesRead;
                long totalReceived = 0;

                // Receiving the file in chunks
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fileStream.Write(buffer, 0, bytesRead);
                    totalReceived += bytesRead;

                    // Calculate progress
                    float progress = (float)totalReceived / totalFileSize;
                    progress = Mathf.Clamp01(progress); // Ensure progress is between 0 and 1

                    // Update status text and progress slider on the main thread
                    UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        if (statusText != null) statusText.text = $"Receiving file... {progress * 100:F2}%";
                    });
                }
            }

            // File received successfully, update the UI on the main thread
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (statusText != null) statusText.text = "File successfully received!";
            });

            stream.Close();
            client.Close();
        }
        catch (SocketException se)
        {
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (statusText != null) statusText.text = $"Connection error: {se.Message}";
            });
            Debug.LogError(se.Message);
        }
        catch (IOException ioEx)
        {
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (statusText != null) statusText.text = $"IO error: {ioEx.Message}";
            });
            Debug.LogError(ioEx.Message);
        }
        catch (Exception ex)
        {
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (statusText != null) statusText.text = $"Error: {ex.Message}";
            });
            Debug.LogError(ex.Message);
        }
    }

    // Clean up the listener and thread on application quit
    private void OnApplicationQuit()
    {
        if (tcpListener != null)
        {
            tcpListener.Stop(); // Stop the listener when the application quits
        }

        if (listenerThread != null && listenerThread.IsAlive)
        {
            listenerThread.Abort(); // Abort the listener thread if it is still running
        }
    }
}
