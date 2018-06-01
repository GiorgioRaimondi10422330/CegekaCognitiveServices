using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FaceSentiment.Dynamic.WebApp.Models
{
    public enum FacesentimentWebResult: byte { Success, NotFound, UncatchError, CameraWorking, CameraConnecting, CameraStopped, CameraCrushed};
}