// Basic Libraries
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
// Unity Libraries
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
// Connecting to server libraries
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class DisplayByteStream : MonoBehaviour {
    // State Variables
    private bool keep_alive = true;
    private bool connection_sucess = false;

    // Connection Variables
    private byte[] received_bytes;
    private int received_bytes_ptr = 0;
    private bool byte_arr_free = false;

    // Testing Variables
    UdpClient testudpclient = new UdpClient();
    IPEndPoint testep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000); // endpoint where server is listening
    Thread _thread;
    bool _thread_running;
    private int byte_counter = 0;
    private bool is_ready_test = false;
    private string test_string = "null";
    private Mutex test_mutex = new Mutex();

    // Unity Game Objects
    public VideoPlayer vp;
    public Renderer sphere_texture;
    public Text byte_stream_info;

    // Frame Variables
    private const int image_size = 5000000;                                     // Number of bytes for an image
    private const int num_frames = 2;                                           // Number of frames to swap out
    private enum TexFlags : int { free = 0, ready = 1, busy = 2 };              // The possible states for a texture
    private byte[][] images = { new byte[image_size], new byte[image_size] };   // The frames used to store images as bytes
    private int frame_ind = 0;                                                  // The index of the current frame we're working on
    private TexFlags[] image_state_arr = { TexFlags.free, TexFlags.free };      // The state of the frames - used for deciding on whether to overwrite them or not
    private Mutex frame_ind_mutex = new Mutex();                                // Used to prevent race conditions for occurring on frame_ind

    // Use this for initialization
    void Start () {
        vp = GameObject.Find("Sphere").GetComponent<VideoPlayer>();
        byte_stream_info = GameObject.Find("Byte Stream Info").GetComponent<Text>();
        sphere_texture = GameObject.Find("Sphere").GetComponent<Renderer>();

        // Testing
        try
        {
            testudpclient.Connect(testep);
            connection_sucess = true;
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
        if (connection_sucess)
        {
            // Waiting for response from server
            var datagram = Encoding.ASCII.GetBytes("Are we connected yet?");
            testudpclient.Send(datagram, datagram.Length);
            var data = Encoding.ASCII.GetString(testudpclient.Receive(ref testep));
            while (data != "Yes we are connected.")
            {
                testudpclient.Send(datagram, datagram.Length);
                data = Encoding.ASCII.GetString(testudpclient.Receive(ref testep));
            }
            // Requesting byte stream
            datagram = Encoding.ASCII.GetBytes("Received response!");
            testudpclient.Send(datagram, datagram.Length);
            data = Encoding.ASCII.GetString(testudpclient.Receive(ref testep));
            while (data.Length <= 0)
            {
                Debug.Log(data);
                testudpclient.Send(datagram, datagram.Length);
                data = Encoding.ASCII.GetString(testudpclient.Receive(ref testep));
            }
            Debug.Log("HEY");

            // Testing - Starting test thread
            _thread = new Thread(ReceiveAndDisplayBytes);
            _thread.Start();
        }
    }

    // Update is called once per frame
    void Update () {
        int current_ind = (frame_ind + 1) % num_frames;

        // Updating the texture
        if (image_state_arr[current_ind] == TexFlags.ready)
        {
            image_state_arr[current_ind] = TexFlags.busy;
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(images[frame_ind]);
            sphere_texture.material.mainTexture = tex;

            frame_ind_mutex.WaitOne();
            frame_ind = (frame_ind + 1) % num_frames;
            frame_ind_mutex.ReleaseMutex();

            image_state_arr[current_ind] = TexFlags.free;
        }

        test_mutex.WaitOne();
        byte_stream_info.text = test_string;
        test_mutex.ReleaseMutex();
    }

    // Testing - Displaying bytes on game text
    void ReceiveAndDisplayBytes()
    {
        while (true)
        {
            test_mutex.WaitOne();
            byte b = GetStreamByte();
            if (received_bytes != null)
                test_string = byte_counter.ToString() + ": " + Encoding.ASCII.GetString(received_bytes);
            test_mutex.ReleaseMutex();
        }
    }

    // Function for connecting to a UDP stream server
    // Should run on another thread
    void ConnectToStreamService()
    {
        UdpClient udpclient = new UdpClient();
        IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000); // endpoint where server is listening
        udpclient.Connect(ep);

        // Waiting for response from server
        var datagram = Encoding.ASCII.GetBytes("Are we connected yet?");
        udpclient.Send(datagram, datagram.Length);
        while ( Encoding.ASCII.GetString(udpclient.Receive(ref ep)) != "Yes we are connected." )
        {
            udpclient.Send(datagram, datagram.Length);
        }

        // Receiving packets of data to process
        datagram = Encoding.ASCII.GetBytes("Received response!");
        while (keep_alive)
        {
            // Checking if we can overwrite the byte buffer and then overwriting it
            if (byte_arr_free)
            {
                udpclient.Send(datagram, datagram.Length);
                received_bytes = udpclient.Receive(ref ep);
                byte_arr_free = false;
            }
        }
        return;
    }

    //// Function for populating images with bytes
    //void Build_images_JPEG()
    //{
    //    _thread_running = true;
    //    while (_thread_running)
    //    {
    //        byte b1 = GetStreamByte();
    //        byte b2 = GetStreamByte();

    //        // Start working on making a new image
    //        if (b1 == 0xff && b2 == 0xd8 && image_state_arr[frame_ind] == TexFlags.free)
    //        {
    //            // Initialising image
    //            int byte_count = 0;
    //            images[frame_ind] = new byte[image_size];
    //            images[frame_ind][byte_count++] = 0xff;
    //            images[frame_ind][byte_count++] = 0xd8;

    //            // Building image
    //            b1 = GetStreamBytes();
    //            int end_flag = 0;
    //            while(byte_count < image_size)
    //            {
    //                images[frame_ind][byte_count++] = b1;
    //                if (b1 == 0xff && end_flag == 0)
    //                    end_flag++;
    //                else if (b1 == 0xd9 && end_flag == 1)
    //                    break;
    //                else
    //                    end_flag = 0;
    //            }

    //            // Completing image
    //            if (byte_count == image_size-1)
    //            {
    //                images[frame_ind][image_size-2] = 0xff;
    //                images[frame_ind][image_size-1] = 0xd9;
    //            }
    //            image_state_arr[frame_ind] = TexFlags.ready;

    //            frame_ind_mutex.WaitOne();
    //            frame_ind = (frame_ind + 1) % num_frames;
    //            frame_ind_mutex.ReleaseMutex();
    //        }
    //    }
    //}
    
    // Function that returns the byte of the stream
    byte GetStreamByte()
    {
        //// Return byte at index
        //if (received_bytes_ptr < received_bytes.Length)
        //{
        //    return received_bytes[received_bytes_ptr++];
        //}
        //// Wait for byte array to be filled up
        //else
        //{
        //    byte_arr_free = true;
        //    while (byte_arr_free);
        //    received_bytes_ptr = 0;
        //    return received_bytes[received_bytes_ptr++];
        //}

        // Testing
        byte_counter++;
        // Wait for byte array to be filled up
        if (received_bytes == null || received_bytes_ptr < received_bytes.Length)
        {
            received_bytes = testudpclient.Receive(ref testep);
            received_bytes_ptr = 0;
            return received_bytes[received_bytes_ptr++];
        }
        else
            return received_bytes[received_bytes_ptr++];
    }

    void OnDisable()
    {
        // If the thread is still running, we should shut it down,
        // otherwise it can prevent the game from exiting correctly.
        if (_thread_running)
        {
            // This forces the while loop in the ThreadedWork function to abort.
            _thread_running = false;

            // This waits until the thread exits,
            // ensuring any cleanup we do after this is safe. 
            _thread.Join();
        }

        // Thread is guaranteed no longer running. Do other cleanup tasks.
    }
}
