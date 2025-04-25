using System;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class FileSender : MonoBehaviour
{
    public InputField targetIPInput;
    public Text statusText;
    public Slider progressSlider;

    private const int CHUNK_SIZE = 1024 * 1024; // 1MB chunk size
    private string filePath = ""; // Variable to store the selected file path

    // Function to open file picker dialog using Native File Picker
    public void PickFile()
    {
        string[] mimeTypes = new string[] { "text/plain", "application/pdf", "image/jpeg", "image/png" };

        NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                statusText.text = "No file selected!";
                return;
            }

            filePath = path; // Save the selected file path
            statusText.text = $"Selected file: {Path.GetFileName(filePath)}";
        }, mimeTypes);
    }

    // Function to send the file
    public void SendFile()
    {
        if (string.IsNullOrEmpty(filePath))
        {
            statusText.text = "No file selected!";
            return;
        }

        string targetIP = targetIPInput.text.Trim(); // Remove extra spaces

        if (string.IsNullOrEmpty(targetIP))
        {
            statusText.text = "IP not valid!";
            return;
        }

        // Special case for localhost or 127.0.0.1
        if (targetIP == "localhost" || targetIP == "127.0.0.1")
        {
            targetIP = "127.0.0.1"; // Treat localhost as a valid IP
        }

        // Validate IP address format
        if (!System.Net.IPAddress.TryParse(targetIP, out _))
        {
            statusText.text = "Invalid IP address format!";
            return;
        }

        try
        {
            byte[] fileBytes = File.ReadAllBytes(filePath); // Read the entire file into memory
            long totalFileSize = fileBytes.Length; // Get the total size of the file
            int totalChunks = (int)((fileBytes.Length + CHUNK_SIZE - 1) / CHUNK_SIZE);
            int bytesSent = GetLastSentBytes(filePath);  // Get the last sent offset

            TcpClient client = new TcpClient(targetIP, 8081);
            NetworkStream stream = client.GetStream();

            for (int i = bytesSent / CHUNK_SIZE; i < totalChunks; i++)
            {
                int chunkSize = Mathf.Min(CHUNK_SIZE, fileBytes.Length - bytesSent);
                byte[] chunk = new byte[chunkSize];
                Array.Copy(fileBytes, bytesSent, chunk, 0, chunkSize); // Create a chunk from the file
                stream.Write(chunk, 0, chunk.Length); // Write the chunk to the stream

                bytesSent += chunkSize;
                SaveSentBytes(bytesSent, filePath);  // Save offset after each chunk is sent

                float progress = (float)bytesSent / totalFileSize;
                progress = Mathf.Clamp01(progress); // Ensure progress is between 0 and 1
                progressSlider.value = progress; // Update the slider progress
                statusText.text = $"Sending file... {progress * 100:F2}%"; // Update the status text
            }

            stream.Close();
            client.Close();

            statusText.text = "File successfully sent!"; // Display success message
        }
        catch (SocketException se)
        {
            statusText.text = $"Connection error: {se.Message}";
            Debug.LogError(se.Message);
        }
        catch (IOException ioEx)
        {
            statusText.text = $"IO error: {ioEx.Message}";
            Debug.LogError(ioEx.Message);
        }
        catch (Exception ex)
        {
            statusText.text = $"Error: {ex.Message}";
            Debug.LogError(ex.Message);
        }
    }

    private int GetLastSentBytes(string filePath)
    {
        string offsetFile = filePath + ".offset"; // Save the offset in a file
        if (File.Exists(offsetFile))
        {
            return int.Parse(File.ReadAllText(offsetFile)); // Return the saved offset
        }
        return 0; // Start from the beginning if no offset is found
    }

    private void SaveSentBytes(int bytesSent, string filePath)
    {
        string offsetFile = filePath + ".offset"; // Path to the offset file
        File.WriteAllText(offsetFile, bytesSent.ToString()); // Save the current byte offset
    }
}
