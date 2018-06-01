using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Timers;

namespace FaceSentiment.Dynamic.WebApp.Models
{
    /// <summary>
    /// Stream Manager for stream a video as Sequence of Snapshot
    /// </summary>
    class SnapshotStreamManager : IStreamManager
    {
        //Parameters setting to connect to Webcam
        private string connectionString = "";
        private string user = "";
        private string password = "";
        private bool useLocalCamera = false; //Never Used
        private CameraValues camera;

        // Timer for send a request for a snapshot
        private Timer catchImageDispatcher;

        //Event Handler to send the Frame to UpperLevels
        private EventHandler<Bitmap> frameHandler;
        //Event Handler to send the Error to UpperLevels
        private EventHandler<string> errorStreamHandler;

        //Variable to control multiple thread to access to the same variable
        private object lockFrameRequest = new object();
        private bool requiringFrame = false;
        private bool stopping = false;
        
        /// <summary>
        /// DefaultConstructor
        /// </summary>
        public SnapshotStreamManager()
        {
            catchImageDispatcher = new Timer();
            catchImageDispatcher.Elapsed += catch_frame;
            catchImageDispatcher.Interval = 50;
        }

        //Properties

        /// <summary>
        /// Event Handler for Frame
        /// </summary>
        public EventHandler<Bitmap> FrameHandler { get => frameHandler; set => frameHandler = value; }
        
        /// <summary>
        /// Event Handler for Errors
        /// </summary>
        public EventHandler<string> ErrorStreamHandler { get => errorStreamHandler; set => errorStreamHandler = value; }
        
        //Events

        /// <summary>
        /// Event required by the time to send the request for a frame
        /// </summary>
        private void catch_frame(object sender, EventArgs e)
        {
            lock (lockFrameRequest)
            {
                if (requiringFrame)
                    return;
                requiringFrame = true;
            }
            Task.Factory.StartNew(() =>
            {
                try
                {
                    if (FrameHandler != null)
                    {
                        WebRequest request = HttpWebRequest.Create(connectionString);
                        request.Credentials = new NetworkCredential(user, password);

                        WebResponse response = request.GetResponse();
                        Stream ms = response.GetResponseStream();
                        
                        if(FrameHandler!=null)
                            FrameHandler.Invoke(sender, new Bitmap(ms));
                        lock (lockFrameRequest)
                        {
                            requiringFrame = false;
                        }
                    }
                    else
                    {
                        throw new Exception("No event add to FrameHandler");
                    }
                }
                catch (Exception ex)
                {
                    ErrorStreamHandler.Invoke(this, $"Error while Streaming\n {ex.Message}");
                }
            });

        }
        

        //Methods


        /// <summary>
        /// Try to Start the Camera stream
        /// </summary>
        public void StartStream()
        {
            catchImageDispatcher.Start();
        }

        /// <summary>
        /// Try to Stop the camera string
        /// </summary>
        public void StopStream()
        {
            lock (lockFrameRequest)
            {
                catchImageDispatcher.Stop();
            }
        }

        /// <summary>
        /// Set all Connections for a given Camera Setup
        /// </summary>
        /// <param name="cam">Camera Set Up</param>
        public void SetConnections(CameraValues cam)
        {
            camera = cam.Clone();
            connectionString = camera.ConnectionString;
            user = camera.User;
            password = camera.Password;
            catchImageDispatcher.Interval = (int) Math.Round(1000.0/(double)camera.Fps);
        }

        /// <summary>
        /// Name of the Snapshot video streamer
        /// </summary>
        /// <returns>Name of the class with the used camera</returns>
        public override string ToString()
        {
            return $"SnapshotStreamManager/{camera}/. ";
        }
    }
}
