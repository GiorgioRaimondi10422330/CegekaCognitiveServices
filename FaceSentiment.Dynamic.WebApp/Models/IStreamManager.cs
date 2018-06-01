using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceSentiment.Dynamic.WebApp.Models
{
    public interface IStreamManager
    {        
        /// <summary>
        /// Event Handler for Errors
        /// </summary>
        EventHandler<string> ErrorStreamHandler { get; set; }
        
        /// <summary>
        /// Event Handler for Frame
        /// </summary>
        EventHandler<Bitmap> FrameHandler { get; set; }

        /// <summary>
        /// Set all Connections for a given Camera Setup
        /// </summary>
        /// <param name="cam">Camera Set Up</param>
        void SetConnections(CameraValues cam);

        /// <summary>
        /// Try to Start the Camera stream
        /// </summary>
        void StartStream();

        /// <summary>
        /// Try to Stop the camera string
        /// </summary>
        void StopStream();
    }
}
