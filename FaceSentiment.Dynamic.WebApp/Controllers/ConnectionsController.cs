using FaceSentiment.Dynamic.WebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FaceSentiment.Dynamic.WebApp.Controllers
{
    public class ConnectionsController : Controller
    {
        string rootPath;
        string connectionPath;
        string detectionPath;
        IDbManager db = new DbManager();
        public ConnectionsController()
        {
            rootPath = System.Web.HttpContext.Current.Server.MapPath("~/Settings");
            connectionPath = $"{rootPath}\\{"Connections"}\\{"Connections.txt"}";
            detectionPath = $"{rootPath}\\{"Connections"}\\{"DetectionSetting.txt"}";
        }

        // GET: Connections
        [Authorize]
        public ActionResult Index()
        {
            new VideoController().InitializeAnalyzer();
            SetUp setUp = new SetUp();
            setUp.connections = System.Web.HttpContext.Current.Application["Connections"] as Connections;
            setUp.detection = System.Web.HttpContext.Current.Application["Detection"] as DetectionSetting;
            return View(setUp);
        }
        

        [Authorize]
        public ActionResult EditConnections()
        {
            var con=System.Web.HttpContext.Current.Application["Connections"] as Connections;
            return View(con);
        }

        [Authorize]
        [HttpPost]
        public ActionResult EditConnections(Connections con)
        {
            try
            {
                //Connections con = new Connections();
                if (true && TryUpdateModel(con))
                {
                    var OldConnections = System.Web.HttpContext.Current.Application["Connections"] as Connections;
                    System.Web.HttpContext.Current.Application["Connections"] = con;
                    var analyzers = System.Web.HttpContext.Current.Application["Analyzers"] as List<VideoAnalyzer>;
                    foreach (var analyzer in analyzers)
                    {
                        analyzer.UpdateConnections(con);
                    }
                    var d = DateTime.Now;
                    db.UpdateConnections(con, connectionPath);
                    db.UpdateConnections(OldConnections, $"{rootPath}\\Connections\\OldSetUp\\Connections_{d.Day}_{d.Month}_{d.Year}_{d.Hour}_{d.Minute}.txt");
                    (System.Web.HttpContext.Current.Application["DbManager"] as IDbManager).SetConnection(con);
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.Error = "Model doesn't fit";
                    return RedirectToAction("EditConnections");
                }
            }
            catch(Exception ex)
            {
                ViewBag.Error = "Generic Error Occurred\n"+ex.Message;
                return RedirectToAction("EditConnections");
            }
        }


        [Authorize]
        public ActionResult EditDetections()
        {
            var det = System.Web.HttpContext.Current.Application["Detection"] as DetectionSetting;
            return View(det);
        }

        [Authorize]
        [HttpPost]
        public ActionResult EditDetections(FormCollection collection)
        {
            try
            {
                DetectionSetting det = new DetectionSetting();
                if (TryUpdateModel(det))
                {
                    var OldDetection = System.Web.HttpContext.Current.Application["Detection"] as DetectionSetting;
                    System.Web.HttpContext.Current.Application["Detection"] = det;
                    var analyzers = System.Web.HttpContext.Current.Application["Analyzers"] as List<VideoAnalyzer>;
                    foreach (var analyzer in analyzers)
                    {
                        analyzer.UpdateDetection(det);
                    }
                    var d = DateTime.Now;
                    db.UpdateDetections(det, detectionPath);
                    db.UpdateDetections(OldDetection, $"{rootPath}\\Connections\\OldSetUp\\Detections_{d.Day}_{d.Month}_{d.Year}_{d.Hour}_{d.Minute}.txt");
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.Error = "Model doesn't fit";
                    return RedirectToAction("EditDetections");
                }
            }
            catch(Exception ex)
            {
                ViewBag.Error = "Generic Error occured\n"+ex.Message;
                return RedirectToAction("EditDetections");
            }
        }


    }
}

