using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MdnsListener : MonoBehaviour
{
    public Text foundIPText;
    private UdpClient listener;

    void Start()
    {
        listener = new UdpClient(5353);  // Bind to port 5353 to listen for incoming messages
        listener.JoinMulticastGroup(IPAddress.Parse("224.0.0.251"));  // Join the mDNS multicast address
        StartCoroutine(ListenForServices());  // Start listening for services using Coroutine
    }

    // Coroutine to handle mDNS listening without blocking the main thread
    private IEnumerator ListenForServices()
    {
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, 5353);

        while (true)
        {
            // Receive mDNS message asynchronously
            byte[] data = listener.Receive(ref groupEP);
            string message = Encoding.UTF8.GetString(data);

            // Check for specific mDNS response, adjust if necessary
            if (message.StartsWith("MYFILEAPP"))
            {
                string[] parts = message.Split('|');
                if (parts.Length > 1)
                {
                    string foundIP = parts[1];
                    foundIPText.text = $"Ditemukan: {foundIP}";  // Update UI with found IP
                }
            }

            // Yield return to allow Unity to continue the main loop without freezing
            yield return null;
        }
    }

    // Clean up the listener when the application quits
    private void OnApplicationQuit()
    {
        listener.Close();  // Ensure we properly close the listener on quit
    }
}