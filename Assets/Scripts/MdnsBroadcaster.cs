using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Threading;

public class MdnsBroadcaster : MonoBehaviour
{
    private UdpClient udpClient;
    private IPEndPoint multicastEP = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);
    private Thread broadcastThread;
    private volatile bool running = false;

    void Start()
    {
        udpClient = new UdpClient();
        // We don't need to join the multicast group since we're only broadcasting
        running = true;
        broadcastThread = new Thread(BroadcastLoop);
        broadcastThread.Start();
    }

    // Broadcast loop running in a separate thread
    void BroadcastLoop()
    {
        while (running)
        {
            string message = $"MYFILEAPP|{GetLocalIPAddress()}|8080";
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, multicastEP);
            Thread.Sleep(2000); // Broadcast every 2 seconds
        }
    }

    // Method to get the local IP address (IPv4)
    string GetLocalIPAddress()
    {
        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        }
        return "127.0.0.1"; // Return localhost if no IP is found
    }

    // Clean up the thread and udpClient when the application quits
    private void OnApplicationQuit()
    {
        running = false; // Stop the thread gracefully
        udpClient.Close();
        
        // Safely abort the broadcast thread
        if (broadcastThread != null && broadcastThread.IsAlive)
        {
            broadcastThread.Join(); // Wait for the thread to finish
        }
    }
}