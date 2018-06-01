using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FaceSentiment.Dynamic.WebApp.Controllers
{
    public class MediaController : Controller
    {
        private string rootPath;
        private string imagePath;
        public MediaController()
        {
            rootPath = System.Web.HttpContext.Current.Server.MapPath("~/Settings");
            imagePath=$"{rootPath}\\{"Images"}";
        }
        // GET: Media
        public ActionResult GetCegekaIcon(string Im)=> base.File($"{imagePath}\\{"cegekaIcon.png"}", "image/png");
        public ActionResult FaceDetectionImage(string Im)=> base.File($"{imagePath}\\{"faceDetection.jpg"}", "image/jpeg");
        public ActionResult PrevisionImage(string Im)=> base.File($"{imagePath}\\{"Prevision.jpg"}", "image/jpeg");
        public ActionResult AnalysisImage(string Im)=> base.File($"{imagePath}\\{"sentimentAnalysis.jpg"}", "image/jpeg");
        public ActionResult WebcamImage(string Im)=> base.File($"{imagePath}\\{"Webcams.jpg"}", "image/jpeg");
        public ActionResult StartButtonImage(string Im)=> base.File($"{imagePath}\\{"StartButton.png"}", "image/png");
        public ActionResult StartButtonOnmouseImage(string Im)=> base.File($"{imagePath}\\{"StartButton_ONCLICK.png"}", "image/png");
        public ActionResult StopButtonImage(string Im)=> base.File($"{imagePath}\\{"stopButton.png"}", "image/png");
        public ActionResult StopButtonOnmouseImage(string Im)=> base.File($"{imagePath}\\{"stopButtonOnClick.png"}", "image/png");
        public ActionResult NewCameraImage(string Im)=> base.File($"{imagePath}\\{"new_button.png"}", "image/png");
        public ActionResult NewCameraOnMouseImage(string Im)=> base.File($"{imagePath}\\{"NewCamera_ONCLICK.png"}", "image/png");
        public ActionResult LoadingGif(string Im)=> base.File($"{imagePath}\\{"loading.gif"}", "image/gif" );
    }
}