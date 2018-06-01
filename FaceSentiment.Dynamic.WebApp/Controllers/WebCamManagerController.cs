using FaceSentiment.Dynamic.WebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;

namespace FaceSentiment.Dynamic.WebApp.Controllers
{
    public class WebCamManagerController : Controller
    {
        private IDbManager db = new DbManager();
        private string cameraPath;
        public WebCamManagerController()
        {
            new VideoController().InitializeAnalyzer();
            string root = System.Web.HttpContext.Current.Server.MapPath("~/Settings");
            cameraPath = string.Format("{0}\\{1}\\{2}", root, "Connections", "CameraData.txt");
            ViewBag.Error = "";
        }

        [WebcamControllerAuthorize]
        public ActionResult WebCams()
        {
            return RedirectToAction("WebCamsAuthorize");
        }

        // GET: FaceSentiment
        [Authorize]
        public ActionResult WebCamsAuthorize()
        {
            var analyzers = (System.Web.HttpContext.Current.Application["Analyzers"] as List<VideoAnalyzer>);
            var listOfCamera = new List<CameraValues>();
            foreach(var analyzer in analyzers)
            {
                listOfCamera.Add(analyzer.Camera);
            }
            return View(listOfCamera);
        }

        public ActionResult WebCamAnauthorize()
        { 
            var analyzers = (System.Web.HttpContext.Current.Application["Analyzers"] as List<VideoAnalyzer>);
            var listOfCamera = new List<CameraValues>();
            foreach (var analyzer in analyzers)
            {
                listOfCamera.Add(analyzer.Camera);
            }
            return View(listOfCamera);
        }


        // GET: FaceSentiment/Details/5
        public ActionResult Details(string id)
        {
            var analyzer= (System.Web.HttpContext.Current.Application["Analyzers"] as List<VideoAnalyzer>).FirstOrDefault(a => a.CameraId.ToString() == id);
            //var camera = db.ReadCamera(cameraPath).FirstOrDefault(c => c.Id.ToString() == id);
            if(analyzer!=null)
                return View(analyzer.Camera);
            else
            {
                ViewBag.Error = "Camera not Found";
                return RedirectToAction("WebCams");
            }
        }

        // GET: FaceSentiment/Create
        public ActionResult Create()
        {
            CameraValues cam = new CameraValues();
            cam.Fps = 30;
            cam.MjpegStreamer = true;
            cam.SecondTimeOut = 2;
            cam.LocalWebcam = false;
            return View(cam);
        }

        // POST: FaceSentiment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(FormCollection collection)
        {
            try
            {
               
                CameraValues cameraVal = new CameraValues();
                if (TryUpdateModel(cameraVal))
                {
                    cameraVal.Id = Guid.NewGuid();
                    cameraVal.Status = "Stopped";
                    var analyzers = (System.Web.HttpContext.Current.Application["Analyzers"] as List<VideoAnalyzer>);
                    var analyzer = analyzers
                        .FirstOrDefault(a=> a.Camera.IsEqual(cameraVal));

                    if (analyzer != null)
                    {
                        ViewBag.Error="Already exist a camera with this settings";
                        return View(cameraVal);
                    }
                    var connections = (System.Web.HttpContext.Current.Application["Connections"] as Connections);
                    var detections= (System.Web.HttpContext.Current.Application["Detection"] as DetectionSetting);
                    analyzers.Add(new VideoAnalyzer(cameraVal, connections,detections));
                    await db.UpdateCamera(cameraVal);
                    db.UpdateCamera(cameraVal, cameraPath);
                    return RedirectToAction("WebCams");
                }
                else
                {
                    ViewBag.Error = "Model doesn't match";
                    return RedirectToAction("Create");
                }
            }
            catch
            {
                return RedirectToAction("Create");
            }
        }

        // GET: FaceSentiment/Edit/5
        public ActionResult Edit(string id)
        {
            var analyzer = (System.Web.HttpContext.Current.Application["Analyzers"] as List<VideoAnalyzer>).FirstOrDefault(a=> a.CameraId.ToString()== id);
            //var camera = db.ReadCamera(cameraPath).FirstOrDefault(c => c.Id.ToString() == id);
            if (analyzer != null && analyzer.Camera.Status.Contains("Stopped"))
            {
                return View(analyzer.Camera);
            }
            else if(analyzer!=null)
            {
                ViewBag.Error = $"Impossible to Edit Camera \"{analyzer.Camera}\" because is running";
                return RedirectToAction("WebCams");
            }
            else
            {
                ViewBag.Error = $"Impossible to Edit the camera, Camera not Found";
                return RedirectToAction("WebCams");
            }
        }

        // POST: FaceSentiment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, FormCollection collection)
        {
            try
            {
                CameraValues cam = new CameraValues();
                if (TryUpdateModel(cam))
                {

                    cam.Id = new Guid(id);
                    var analyzer = (System.Web.HttpContext.Current.Application["Analyzers"] as List<VideoAnalyzer>)
                        .FirstOrDefault(a => a.CameraId == cam.Id);
                    if (analyzer == null)
                    {
                        return new HttpNotFoundResult();
                    }

                    analyzer.UpdateCamera(cam);
                    await db.UpdateCamera(cam);
                    db.UpdateCamera(cam,cameraPath);
                    return RedirectToAction("WebCams");
                }
                else
                {
                    ViewBag.Error ="Model doesn't fit";
                    return RedirectToAction("Edit", id);
                }
            }
            catch(Exception ex)
            {

                ViewBag.Error = "Generic Error occurred:\n"+ex.Message;
                return RedirectToAction("Edit", id);
            }
        }

        // GET: FaceSentiment/Delete/5
        public ActionResult Delete(string id)
        {
            var analyzer = (System.Web.HttpContext.Current.Application["Analyzers"] as List<VideoAnalyzer>).FirstOrDefault(a => a.CameraId.ToString() == id);
            //var camera = db.ReadCamera(cameraPath).FirstOrDefault(c => c.Id.ToString() == id);
            if (analyzer != null && !analyzer.Camera.Status.Contains("Stopped"))
            {
                return View(analyzer.Camera);
            }
            else
            {
                ViewBag.Error = $"Impossible to Delete the camera, Camera not Found";
                return RedirectToAction("WebCams");
            }
        }

        // POST: FaceSentiment/Delete/5
        public async Task<ActionResult> DeleteOK(string Id)
        {
            try
            {                
                CameraValues cam = new CameraValues() { Id= new Guid(Id) };

                var analyzers = (System.Web.HttpContext.Current.Application["Analyzers"] as List<VideoAnalyzer>);
                var analyzer = analyzers.FirstOrDefault(a => a.CameraId == cam.Id);
                if (analyzer == null)
                {
                    ViewBag.Error = "Impossible to Delete the camera,\n camera not found";
                    return RedirectToAction("WebCams");
                }
                analyzer.Stop();
                analyzers.RemoveAll(a => a.CameraId == cam.Id);
                await db.DeleteCamera(cam);
                db.DeleteCamera(cam,cameraPath);
                return RedirectToAction("WebCams");
            }
            catch(Exception ex)
            {
                ViewBag.Error = "Generic Error Occurred\n"+ex.Message;
                return RedirectToAction("Delete", Id);
            }
        }
        
    }

    public class WebcamControllerAuthorize : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "WebCamManager", action = "WebCamAnauthorize" }));
            }
            else
            {
                //logged and wihout the role to access it - redirect to the custom controller action
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "WebCamManager", action = "WebCamAuthorize" }));
            }
        }
    }
}
