using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using FaceSentiment.Dynamic.WebApp.Models;
using System.Drawing;
using System.Drawing.Imaging;

namespace FaceSentiment.Dynamic.WebApp.Controllers
{
    public class VideoController : Controller
    {
        private List<VideoAnalyzer> Analyzers = new List<VideoAnalyzer>();
        private IDbManager db = new DbManager();
        private Connections connections;
        private List<CameraValues> cameras;
        private DetectionSetting detectionSetting;

        //FacesentimentWebService fsWS = new FacesentimentWebService();
        // GET: Video
        [Authorize]
        [HttpGet]
        public ActionResult Start(string Id)
        {
            try
            {
                
                this.CreateAnalyzers();

                Analyzers = System.Web.HttpContext.Current.Application["Analyzers"] as List<VideoAnalyzer>;
                var analyser= Analyzers.FirstOrDefault(a => a.CameraId.ToString() == Id);
                if (analyser != null)
                {
                    analyser.Start();
                    return Json(analyser.Camera, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return new HttpNotFoundResult();
                }
            }
            catch(Exception ex)
            {
                return new HttpStatusCodeResult(401);
            }
        }
        
        [Authorize]
        [HttpGet]
        public ActionResult Stop(string Id)
        {
            try
            {
                
                this.CreateAnalyzers();

                Analyzers = System.Web.HttpContext.Current.Application["Analyzers"] as List<VideoAnalyzer>;
                var analyser = Analyzers.FirstOrDefault(a => a.CameraId.ToString() == Id);
                if (analyser != null)
                {
                    analyser.Stop();
                    return Json(analyser.Camera, JsonRequestBehavior.AllowGet);
                }
                else
                    return new HttpNotFoundResult();
                
                /*var result = fsWS.Stop(Id);
                switch (result)
                {
                    case FacesentimentWebResult.Success:
                        return Json("Success", JsonRequestBehavior.AllowGet);

                    case FacesentimentWebResult.NotFound:
                        return new HttpNotFoundResult();

                    default:
                        return new HttpStatusCodeResult(401);
                }*/
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(401);
            }
        }
        
        [Authorize]
        [HttpGet]
        public ActionResult ViewVideo(string Id)
        {
            try
            {
                this.CreateAnalyzers();

                try
                {
                    Analyzers = System.Web.HttpContext.Current.Application["Analyzers"] as List<VideoAnalyzer>;
                    var analyser = Analyzers.FirstOrDefault(a => a.CameraId.ToString() == Id);
                    if (analyser != null && analyser.Camera.Status.Contains("Working"))
                    {
                        ViewBag.Size = analyser.ViewSize();
                        return View(analyser.Camera);
                    }
                    else
                        return RedirectToAction("WebCams","WebCamManager");
                }catch(Exception ex)
                {
                    return RedirectToAction("WebCams", "WebCamManager");
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(404);
            }
        }

        [HttpGet]
        public void InitializeAnalyzer()
        {
            CreateAnalyzers();
            return;
        }

        [Authorize]
        [HttpGet]
        public ActionResult GetFrame(string Id)
        {
            try
            {
                this.CreateAnalyzers();

                try
                {
                    Analyzers = System.Web.HttpContext.Current.Application["Analyzers"] as List<VideoAnalyzer>;
                    var analyser = Analyzers.FirstOrDefault(a => a.CameraId.ToString() == Id);
                    if (analyser != null)
                    {
                        var frame = new Bitmap(analyser.ViewFrame());
                        using (MemoryStream ms = new MemoryStream())
                        {
                            frame.Save(ms, ImageFormat.Jpeg);
                            return File(ms, "image/jpeg");
                        }
                    }
                    else
                        return new HttpNotFoundResult();
                }
                catch (Exception ex)
                {
                    return new HttpStatusCodeResult(404);
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(404);
            }
        }

        [HttpGet]
        public ActionResult GetStatus(string Id)
        {
            try
            {
                this.CreateAnalyzers();

                try
                {
                    Analyzers = System.Web.HttpContext.Current.Application["Analyzers"] as List<VideoAnalyzer>;
                    var analyser = Analyzers.FirstOrDefault(a => a.CameraId.ToString() == Id);
                    if (analyser != null)
                        return Json(analyser.Camera, JsonRequestBehavior.AllowGet);
                    else
                        return new HttpNotFoundResult();
                }
                catch (Exception ex)
                {
                    return new HttpStatusCodeResult(401);
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(404);
            }
        }

        
        private void CreateAnalyzers()
        {
            object locker;
            if (System.Web.HttpContext.Current.Application["lockCreation"] == null)
                System.Web.HttpContext.Current.Application["lockCreation"] = new object();
            locker = System.Web.HttpContext.Current.Application["lockCreation"];

            lock (locker)
            {
                if (System.Web.HttpContext.Current.Application["Analyzers"] == null)
                {
                    //Setting Strings
                    string root = System.Web.HttpContext.Current.Server.MapPath("~/Settings");
                    var ConnectionPath = string.Format("{0}\\{1}\\{2}", root,"Connections", "Connections.txt");
                    var DetectionPath = string.Format("{0}\\{1}\\{2}", root, "Connections", "DetectionSetting.txt");
                    var CameraPath = string.Format("{0}\\{1}\\{2}", root, "Connections", "CameraData.txt");


                    if (System.Web.HttpContext.Current.Application["ConnectionPath"] == null) 
                        System.Web.HttpContext.Current.Application["ConnectionPath"] = ConnectionPath;
                    
                    if(System.Web.HttpContext.Current.Application["DetectionPath"]==null)
                        System.Web.HttpContext.Current.Application["DetectionPath"] = DetectionPath;
                    if(System.Web.HttpContext.Current.Application["CameraPath"]==null)
                        System.Web.HttpContext.Current.Application["CameraPath"] = CameraPath;


                    //Setting Connection
                    connections = db.ReadConnections(ConnectionPath);
                    System.Web.HttpContext.Current.Application["Connections"] = connections;

                    //Setting DB
                    if (System.Web.HttpContext.Current.Application["DbManager"] == null) {
                        db.SetConnection(connections);
                        System.Web.HttpContext.Current.Application["DbManager"] = db;
                    }

                    //Setting detectionSettings
                    detectionSetting = db.ReadDetections(DetectionPath);
                    System.Web.HttpContext.Current.Application["Detection"] = detectionSetting;

                    //Setting cameras and Analyzer
                    cameras = db.ReadCamera(CameraPath);
                    Analyzers = new List<VideoAnalyzer>();
                    foreach (var camera in cameras)
                    {
                        Analyzers.Add(
                            new VideoAnalyzer(camera, connections, detectionSetting)
                            );
                    }
                    System.Web.HttpContext.Current.Application["Cameras"] = cameras;
                    System.Web.HttpContext.Current.Application["Analyzers"] = Analyzers;

                }
            }
        }
    }
}