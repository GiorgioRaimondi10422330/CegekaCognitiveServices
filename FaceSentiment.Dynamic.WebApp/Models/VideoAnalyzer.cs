using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Timers;

namespace FaceSentiment.Dynamic.WebApp.Models
{
    /// <summary>
    /// Class to use the all Subclasses to manage the acquisition of Image,
    /// analyse the sentiment of detected face and save the result on DB
    /// </summary>
    public class VideoAnalyzer
    {
        //Definition Variable
        private CameraValues camera;
        private Connections connections;
        private DetectionSetting detectionSettings;

        //Internal Variable Managers
        //Acquisition of Image from a Camera
        private IStreamManager Video;
        //Detect and track face from an Image
        private FaceDetectionManager Detector;
        //Detect Face, Analyse sentiment of detected face, try to Track people, save result on db 
        private FaceSentimentManager Sentiment;

        //Filepath of the LogError file
        private string LogsFilePath = "";
        
        //Timers
        //Timer to check if stream is correctly working
        private Timer streamTimer;
        //Timer to check if connection worked
        private Timer connectionTimer;
        //Timer for try to restart the connection
        private Timer restartTimer;
        //Timer to Analyse a frame
        private Timer detectionTimer;

        //Event Handler
        private EventHandler onClosing;
        private EventHandler<Bitmap> distributeFrameHandler;
        private EventHandler<CameraValues> statusChangeHandler;
        private EventHandler<int[]> faceDetectedHandler;
        private EventHandler stopView;

        //Error Auxiliary variables

        //List of Error before restarting (Not largely used)
        private List<ErrorLocation> Error= new List<ErrorLocation>();
        //Actual Index
        private int CurrentErrorIndex = 0;
        //Number of Error found until now
        private int CountOfTotalError = 0;
        //Number of Time Restarting the Video
        private int RestartCount = 0;
        //Max number of restart allowed
        private int MaxRestart;

        //Detection Variables
        //Number of faces detected
        private int NumOfFace;
        //Number of request sent to Azure
        private int NumOfRequest;

        //Frame
        private Bitmap newFrame;

        //Variables to avoid multi Thread access to variables
        private bool Closing = false;
        private bool IsConnected = false;
        private bool UsingImage = false;
        private object lockFrameObject=new object();
        public object lockViewClosing = new object();
        private object lockChangeStatus= new object();
        private bool Stopping;


        //Constructor-----------------------------------------------------------------

        /// <summary>
        /// Constructor which set all connections, detections settings and camera settings
        /// </summary>
        /// <param name="cam">Camera Settings</param>
        /// <param name="con">Connections Settings</param>
        /// <param name="det">Detection Settings</param>
        public VideoAnalyzer(CameraValues cam,Connections con, DetectionSetting det)
        {
            //Setting Camera/Connection
            camera = cam.Clone();
            connections = con.Clone();
            detectionSettings = det.Clone();

            //Manager Instantiation
            if (camera.MjpegStreamer)
            {
                Video = new VideoStreamManager();
            }
            else
            {
                Video = new SnapshotStreamManager();
            }
            Detector = new FaceDetectionManager();
            Sentiment = new FaceSentimentManager();

            //Set Connections
            Sentiment.SetConnections(connections, camera);
            Detector.SetDetectionsSetting(detectionSettings);
            Video.SetConnections(camera);
            MaxRestart = connections.TimesOfRestart;

            //Set Event
            Video.FrameHandler += GetFrame;
            Video.ErrorStreamHandler += ErrorHandler;

            //Set Timer
            streamTimer = new Timer();
            connectionTimer = new Timer();
            restartTimer = new Timer();
            detectionTimer = new Timer();

            //streamTimer.Interval = new TimeSpan(0, 0, 0, camera.SecondTimeOut);
            //connectionTimer.Interval = new TimeSpan(0, 0, connections.SecondTimeout * 2);
            //restartTimer.Interval = new TimeSpan(0, 0, connections.SecondTimeout);
            //detectionTimer.Interval = new TimeSpan(0, 0, detectionSettings.SecondForDetection);


            streamTimer.Interval =  camera.SecondTimeOut*1000;
            connectionTimer.Interval = connections.SecondTimeout * 2*1000;
            restartTimer.Interval = connections.SecondTimeout*1000;
            detectionTimer.Interval = detectionSettings.SecondForDetection*1000;


            streamTimer.Elapsed += StreamNotWorking;
            restartTimer.Elapsed += TryRestart;
            connectionTimer.Elapsed += ConnectionNotWorking;
            detectionTimer.Elapsed += DetectFace;

            string root = System.Web.HttpContext.Current.Server.MapPath("~/Settings");
            LogsFilePath =string.Format("{0}\\{1}",root,"Logs\\LogsError.txt");

        }

        //Properties-------------------------------------------------------------
        
        public Guid CameraId { get => camera.Id; } 
        public EventHandler<Bitmap> GetFrameEvent { get => distributeFrameHandler; set => distributeFrameHandler = value; }
        public EventHandler<CameraValues> StatusChangeHandler { get => statusChangeHandler; set => statusChangeHandler = value; }
        public EventHandler StopView { get => stopView; set => stopView = value; }
        public EventHandler<int[]> FaceDetectedHandler { get => faceDetectedHandler; set => faceDetectedHandler = value; }
        public CameraValues Camera { get => camera;  }

        //Public Methods------------------------------------------------------------------


        /// <summary>
        /// Method to try to start the analysis
        /// </summary>
        public void Start()
        {
            if (camera.Status.Contains("Stopped"))
            {
                try
                {
                    Task.Factory.StartNew(() =>
                    {
                        Closing = false;
                        Stopping = false;
                        ChangeStatus("Connecting");
                        connectionTimer.Start();
                        Video.StartStream();
                    });
                }
                catch (Exception ex)
                {
                    StopAll();
                    Error.Add(ErrorLocation.VideoStart);
                    ErrorHandler(Video, ex.Message);
                }
            }
        }

        /// <summary>
        /// Method to try to stop the analysis
        /// </summary>
        public void Stop()
        {
            if (camera.Status == "Working")
            {
                try
                {
                    StopAll();
                }
                catch (Exception ex)
                {
                    Error.Add(ErrorLocation.VideoStop);
                    ErrorHandler(Video, ex.Message);
                }
            }
        }

        /// <summary>
        /// Method to Update the Connection Settings
        /// </summary>
        public void UpdateConnections(Connections conn)
        {
            connections = conn.Clone();
            connectionTimer.Interval = connections.SecondTimeout * 2*1000;
            restartTimer.Interval =  connections.SecondTimeout * 1000;
            Sentiment.SetConnections(connections, camera);
            MaxRestart = connections.TimesOfRestart;
        }

        /// <summary>
        /// Method to Update the Camera Settings
        /// </summary>
        public void UpdateCamera(CameraValues cam)
        {
            camera = cam.Clone();
            streamTimer.Interval = camera.SecondTimeOut*1000;
            Sentiment.SetConnections(connections, camera);
            Video.SetConnections(camera);
        }

        /// <summary>
        /// Method to Update the Detection Settings
        /// </summary>
        public void UpdateDetection(DetectionSetting det)
        {
            detectionSettings = det.Clone();
            Detector.SetDetectionsSetting(detectionSettings);
            detectionTimer.Interval = detectionSettings.SecondForDetection*1000;
        }


        //Public Event --------------------------------------------------------------

        /// <summary>
        /// Method Event Called on Closing Event of higher Logic
        /// </summary>
        public void OnClosing(object sender, EventArgs e)
        {
            Closing = true;
            StopAll();

            if (stopView != null)
                stopView.Invoke(this, null);
        }

        public Size ViewSize()
        {
            Size size;

            lock (lockFrameObject)
            {
                var b = new Bitmap(newFrame);
                size = b.Size;
            }
            return size;
        }
        public Bitmap ViewFrame() {
            Bitmap bit;
            lock (lockFrameObject)
            {
                bit = new Bitmap(newFrame);
            }
            return bit;
        }
        //Private Event--------------------------------------------------------

        /// <summary>
        /// Method called to manage the Error Event and save them in LogsError File
        /// </summary>
        /// <param name="sender">Class who called the error</param>
        /// <param name="e">Error Message</param>
        private void ErrorHandler(object sender, string e)
        {
            CountOfTotalError++;

            if (CurrentErrorIndex > Error.Count - 1 && CountOfTotalError < 10)
            {
                Error.Add(ErrorLocation.None);
                CurrentErrorIndex++;
                File.AppendAllLines(LogsFilePath, new string[] { $"{sender} / {ErrorLocation.None} /{DateTime.Now} : {e}" });
                TryRestart(this, null);
            }
            else if(CountOfTotalError<10)
            {
                switch (Error[CurrentErrorIndex])
                {
                    case ErrorLocation.Restart:
                        File.AppendAllLines(LogsFilePath, new string[] { $"{sender} / {Error[CurrentErrorIndex]} /{DateTime.Now} : {e}" });
                        StopAll();
                        ChangeStatus("Stopped Unconnectable");
                        break;

                    case ErrorLocation.VideoStop:
                        File.AppendAllLines(LogsFilePath, new string[] { $"{sender} / {Error[CurrentErrorIndex]} /{DateTime.Now} : {e}" });
                        StopAll();
                        ChangeStatus("Stopped Error");
                        break;

                    default:
                        File.AppendAllLines(LogsFilePath, new string[] { $"{sender} / {Error[CurrentErrorIndex]} /{DateTime.Now} : {e}" });
                        TryRestart(sender, null);
                        break;
                }
                CurrentErrorIndex++;
            }
            else
            {
                StopAll();
                CountOfTotalError = 0;
                ChangeStatus("Stopped Error");
            }
        }
        
        /// <summary>
        /// Event reciving the Frame the VideoManager
        /// </summary>
        /// <param name="e">Frame</param>
        private void GetFrame(object sender, Bitmap e)
        {
            lock (lockFrameObject)
            {
                lock (lockViewClosing)
                {
                    if (distributeFrameHandler != null)
                    {
                        distributeFrameHandler.Invoke(this, (Bitmap)e.Clone());
                    }
                    if (Stopping)
                        return;
                }
            }
            if (IsConnected)
            {
                streamTimer.Stop();
            }
            else
            {
                connectionTimer.Stop();
                IsConnected = true;
                RestartCount = 0;
                ChangeStatus("Working");
                Error.Clear();
                CurrentErrorIndex = 0;
                detectionTimer.Start();
            }
            lock (lockFrameObject)
            {
                if (!UsingImage)
                {
                    newFrame = e.Clone() as Bitmap;
                    //newFrame.Save(MemoryFrame, ImageFormat.Jpeg);
                }
            }
            if (IsConnected)
            {
                streamTimer.Start();
            }
        }

        /// <summary>
        /// Start the detection of faces, and facesentiment analysis
        /// </summary>
        private void DetectFace(object sender, EventArgs e)
        {
            Task.Factory.StartNew(async () => {
                try
                {

                    using (MemoryStream ms = new MemoryStream())
                    {
                        lock (lockFrameObject)
                        {
                            if (UsingImage)
                                return;
                            UsingImage = true;
                            try
                            {
                                newFrame.Save(ms, ImageFormat.Jpeg);
                            }
                            catch (Exception ex)
                            {
                                newFrame = new Bitmap(newFrame);
                                newFrame.Save(ms, ImageFormat.Jpeg);
                            }
                        }
                        //ImageForm im = new ImageForm(new Bitmap(ms));
                        //im.ShowDialog();
                        bool findFace = Detector.CheckAndTrackFaces(new Bitmap(ms));
                        if (!findFace)
                        {
                            lock (lockFrameObject)
                            {
                                UsingImage = false;
                            }
                            if (Detector.ErrorMessage != null && Detector.ErrorMessage.Contains("Error"))
                            {
                                Error.Add(ErrorLocation.FaceDetection);
                                ErrorHandler(Detector, Detector.ErrorMessage);
                            }
                            return;
                        }
                        NumOfRequest++;
                        var findSentiment = Sentiment.DetectFaceSentiment(ms);
                        if (!(await findSentiment))
                        {

                            if (Sentiment.ErrorMessage != null && Sentiment.ErrorMessage.Contains("Error"))
                            {
                                if (Sentiment.ErrorMessage.Contains("Similar"))
                                {
                                    Error.Add(ErrorLocation.FaceComparison);
                                }
                                else
                                {
                                    Error.Add(ErrorLocation.SentimentAnalysis);
                                }
                                ErrorHandler(Sentiment, Sentiment.ErrorMessage);
                            }
                            FaceDetected();
                            lock (lockFrameObject)
                            {
                                UsingImage = false;
                            }
                            return;
                        }
                        NumOfFace += Sentiment.NumOfFaceDetected;
                        NumOfRequest += Sentiment.NumOfFaceDetected - 1;
                        FaceDetected();
                        var isSaved = await Sentiment.AddToDbAsync();
                        if (!isSaved)
                        {
                            lock (lockFrameObject)
                            {
                                UsingImage = false;
                            }
                            Error.Add(ErrorLocation.SavingOnDB);
                            ErrorHandler(Sentiment, Sentiment.ErrorMessage);
                        }
                        lock (lockFrameObject)
                        {
                            UsingImage = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (lockFrameObject)
                    {
                        UsingImage = false;
                    }
                    ErrorHandler(Sentiment, ex.Message);
                }
            });
        }

        /// <summary>
        /// Called when the Connection Timeout Ends
        /// </summary>
        private void ConnectionNotWorking(object sender, EventArgs e)
        {
            StopAll();
            restartTimer.Start();
        }

        /// <summary>
        /// Try to restart the Video and the analysis when Connection is not working
        /// or when a restart is require
        /// </summary>
        private void TryRestart(object sender, EventArgs e)
        {
            StopAll();
            RestartCount++;
            if (RestartCount < MaxRestart)
            {
                this.Start();
            }
            else
            {
                Error.Add(ErrorLocation.Restart);
                ErrorHandler(this, "Unable to Restart");
            }
        }

        /// <summary>
        /// Called when the strem Timeout Ends
        /// </summary>
        private void StreamNotWorking(object sender, EventArgs e)
        {
            StopAll();
            Error.Add(ErrorLocation.VideoOnStream);
            ErrorHandler(this, "Stream not Working");
        }


        //Private Methods----------------------------------------------------------------

        /// <summary>
        /// Called when the camera status change, and it Invoke the Changestatus EventHandler
        /// </summary>
        /// <param name="status"></param>
        private void ChangeStatus(string status)
        {
            lock (lockChangeStatus)
            {
                camera.Status = status;
                if (StatusChangeHandler != null)
                {
                    StatusChangeHandler.Invoke(this, camera);
                }
            }
        }

        /// <summary>
        /// Stop all Timers and the Video, Changing status to stopped
        /// </summary>
        private void StopAll()
        {
            Task.Factory.StartNew(() =>
            {
                Video.StopStream();
                connectionTimer.Stop();
                restartTimer.Stop();
                streamTimer.Stop();
                detectionTimer.Stop();
                if (!Closing)
                {
                    ChangeStatus("Stopped");
                }
                IsConnected = false;
                lock (lockViewClosing)
                {
                    if (stopView != null)
                        stopView.Invoke(this, null);
                    Stopping = true;
                }
            });
        }

        /// <summary>
        /// Invoke the FaceDetected Event Handler
        /// </summary>
        private void FaceDetected()
        {
            camera.FaceDetectedOnRequest = new int[] { NumOfFace, NumOfRequest };
            if (faceDetectedHandler != null)
            {
                faceDetectedHandler.Invoke(this, new int[] { NumOfFace, NumOfRequest });
            }
        }

        /// <summary>
        /// Name of the VideoAnalizer
        /// </summary>
        /// <returns>String containg the Class Name and Used camera name</returns>
        public override string ToString()
        {
            return $"VideoAnalyser/{camera}";
        }

    }

    public enum ErrorLocation: byte {None, SentimentAnalysis, FaceComparison, FaceDetection, VideoStart, VideoStop, VideoOnStream, SavingOnDB, Restart };
}
