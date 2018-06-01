using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceSentiment.Dynamic.WebApp.Models
{
    /// <summary>
    /// Stream Manager for stream a video into Mjpeg format (using Aforge)
    /// </summary>
    public class VideoStreamManager : IStreamManager
    {
        //Private Fields------------------------------------------------
        
        //List of useable LocalwebcamController
        private FilterInfoCollection videoDevicesList;
        //Streamer for LocalWebCam
        private IVideoSource computerStream;
        //Streamer for Webcam Ip
        private MJPEGStream webCamIpStream;
        //Object to control multiThread access to the same variable
        private volatile object _locker = new object();

        //Camera Settings
        private string connectionString = "";
        private string user = "";
        private string password = "";
        private bool useLocalCamera = false;
        private CameraValues camera;
        
        //Event Handler
        //Dispatching a new frame
        private EventHandler<Bitmap> frameHandler;
        //Dispatching the error message
        private EventHandler<string> errorStreamHandler;

        //Properties------------------------------------------------------------
        public EventHandler<Bitmap> FrameHandler { get => frameHandler; set => frameHandler = value; }
        public EventHandler<string> ErrorStreamHandler { get => errorStreamHandler; set => errorStreamHandler = value; }

        //Constructor-------------------------------------------------------------------

        /// <summary>
        /// Default constructor
        /// </summary>
        public VideoStreamManager()
        {
            videoDevicesList = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        }

        //Public Methods----------------------------------------------------------------------

        /// <summary>
        /// Set Up of camera settings
        /// </summary>
        /// <param name="cam">Camera Settings</param>
        public void SetConnections(CameraValues cam)
        {
            camera = cam.Clone();
            connectionString = cam.ConnectionString;
            user = cam.User;
            password = cam.Password;
            useLocalCamera = cam.LocalWebcam;
        }

        /// <summary>
        /// Trying to start a stream Video
        /// </summary>
        public void StartStream()
        {
            if (!useLocalCamera)
            {
                if (connectionString == "")
                {
                    throw new Exception("Error while starting Streaming\n No connection string set");
                }
                try
                {
                    webCamIpStream = new MJPEGStream(connectionString);
                    webCamIpStream.Login = user;
                    webCamIpStream.Password = password;
                    webCamIpStream.NewFrame += new NewFrameEventHandler(video_NewFrame);
                    webCamIpStream.Start();

                }
                catch (Exception ex)
                {
                    this.StopStream();
                    throw new Exception("Error while starting Streaming IP", ex);
                }
            }
            else
            {
                if (videoDevicesList.Count == 0)
                {
                    throw new Exception("Error while starting Streaming\n No device found");
                }
                try
                {
                    computerStream = new VideoCaptureDevice(videoDevicesList[0].MonikerString);
                    computerStream.NewFrame += new NewFrameEventHandler(video_NewFrame);
                    computerStream.Start();
                }
                catch (Exception ex)
                {
                    this.StopStream();
                    throw new Exception("Error while starting Streamin on DeviceList", ex);
                }
            }
        }
        
        /// <summary>
        /// Try to Stop a stream Video
        /// </summary>
        public void StopStream()
        {
            if (useLocalCamera)
            {
                if (computerStream != null)
                {
                    computerStream.SignalToStop();
                    computerStream.NewFrame -= video_NewFrame;
                }
            }
            else
            {
                if (webCamIpStream != null)
                {
                    webCamIpStream.SignalToStop();
                    webCamIpStream.NewFrame -= video_NewFrame;
                }
            }
        }
        
        /// <summary>
        /// Name of the VideoStreamr
        /// </summary>
        /// <returns>Name of the class with the used camera</returns>
        public override string ToString()
        {
            return $"VideoStreamManager/{camera}/. ";
        }

        //Private Event---------------------------------------------------------------
        /// <summary>
        /// Event Called when the webcamIpStream or computerStream catched a new Frame
        /// and Invoke its own NewFrame Event
        /// </summary
        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                if (FrameHandler != null)
                {
                    Bitmap bi = (Bitmap)eventArgs.Frame.Clone();
                    FrameHandler.Invoke(sender, bi);
                }
                else
                {
                    throw new Exception("No event Listener");
                }

            }
            catch (Exception ex)
            {
                if (errorStreamHandler != null)
                {
                    errorStreamHandler.Invoke(this, "Error while showing Frame\n" + ex.Message);
                }
                else
                {
                    this.StopStream();
                    throw new Exception("Error while showing Frame\n", ex);
                }
            }
        }
    }
}
