using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FaceSentiment.Dynamic.WebApp.Models
{
    /// <summary>
    /// Class containg all the attributes of a camera 
    /// (for settings and auxiliary utilities)
    /// </summary>
    public class CameraValues
    {
        //Setting Variables

        // Id of the camera
        [HiddenInput]
        public Guid Id { get; set; }
        // Type of Camera Used
        [Required]
        [StringLength(20)]
        public string Type { get; set; }
        // Location of the camera used

        [Required]
        [StringLength(40)]
        public string Location { get; set; }
        //Position of the camera (Entrance, Exit, Internal, None)

        [Required]
        [StringLength(40)]
        public string Position { get; set; }
        //Webcam Ip Connection string
        [DisplayName("Webcam Ip Url")]
        public string ConnectionString { get; set; }

        //Webcam Ip User for Login
        [DisplayName("Webcam Username")]
        public string User { get; set; }
        //Webcam Ip Password for Login
        [DisplayName("Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        //Use of a MjpegStreamer or a SnapShotStreamer
        [DisplayName("Use a VideoStreamer")]
        public bool MjpegStreamer { get; set; }

        //Use of Local Webcam (Computer webcam, USB webcam)
        [DisplayName("Use a Computer Webcam")]
        public bool LocalWebcam { get; set; }

        //Fps (only for SnapShotStreamer)
        [Required]
        [DisplayName("Webcam frequency")]
        [Range(1,60,ErrorMessage ="The frequency must be between 1 and 60")]
        public int Fps { get; set; }

        //Second to wait before consider the webcam as not working
        [Required]
        [DisplayName("Webcam Timeout")]
        public int SecondTimeOut { get; set; }

        //Auxiliary
        //Status of the webcam
        [HiddenInput]
        public string Status { get; set; }
        //Number of FaceDetected and Number of Request Sent
        [HiddenInput]
        public int[] FaceDetectedOnRequest { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public CameraValues()
        {

        }
        
        /// <summary>
        /// Constructor using a string of a Stringify Camera (from Db)
        /// </summary>
        /// <param name="row">String containg a stringify camera</param>
        public CameraValues(string row)
        {
            var value = row.Trim().Split(new string[] { "|||" }, StringSplitOptions.None);
            Id = new Guid(value[0]);
            Type = value[1];
            Location = value[2];
            Position = value[3];
            ConnectionString = value[4];
            User = value[5];
            Password = value[6];
            MjpegStreamer = Boolean.Parse(value[7]);
            LocalWebcam = Boolean.Parse(value[8]);
            Fps = Int32.Parse(value[9]);
            SecondTimeOut = Int32.Parse(value[10]);
            Status = "Stopped";
            FaceDetectedOnRequest = new int[] { 0, 0 };
        }

        /// <summary>
        /// Clone Method
        /// </summary>
        /// <returns>Clone of the original CameraValues</returns>
        public CameraValues Clone()
        {
            CameraValues cam = new CameraValues();
            cam.Id = this.Id;
            cam.Type = this.Type;
            cam.Location = this.Location;
            cam.Position = this.Position;
            cam.ConnectionString = this.ConnectionString;
            cam.User = this.User;
            cam.Password = this.Password;
            cam.MjpegStreamer = this.MjpegStreamer;
            cam.Fps = this.Fps;
            cam.LocalWebcam = this.LocalWebcam;
            cam.SecondTimeOut = this.SecondTimeOut;
            cam.Status = this.Status;
            cam.FaceDetectedOnRequest = FaceDetectedOnRequest;
            return cam;
        }

        /// <summary>
        /// Check if the current camera has the same Setting value of the passed camera
        /// </summary>
        /// <param name="cam">Camera to compare</param>
        /// <returns>True if the Cameras have the same setting values</returns>
        public bool IsEqual(CameraValues cam)
        {
            if (Id != cam.Id)
                return false;
            if (Type != cam.Type)
                return false;
            if (Location != cam.Location)
                return false;
            if (Position != cam.Position)
                return false;
            if (ConnectionString != cam.ConnectionString)
                return false;
            if (User != cam.User)
                return false;
            if (Password != cam.Password)
                return false;
            if (MjpegStreamer != cam.MjpegStreamer)
                return false;
            if (LocalWebcam != cam.LocalWebcam)
                return false;
            if (Fps != cam.Fps)
                return false;
            if (SecondTimeOut != cam.SecondTimeOut)
                return false;
            return true;
        }

        /// <summary>
        /// Method to collapse the Setting value in a string
        /// </summary>
        /// <returns>Collapse Setting Values</returns>
        public string Stringify()
        {
            string s;
            s = Id.ToString()+"|||";
            s += Type + "|||";
            s +=Location + "|||";
            s +=Position + "|||";
            s +=ConnectionString + "|||";
            s +=User + "|||";
            s +=Password + "|||";
            s +=MjpegStreamer.ToString() + "|||";
            s +=LocalWebcam.ToString() + "|||";
            s +=Fps + "|||";
            s +=SecondTimeOut;

            return s;
        }

        /// <summary>
        /// Method to return the Visible caracteristic of a camera
        /// </summary>
        public override string ToString()
        {
            return Type+": "+Location+" "+Position;
        }
    }
}
