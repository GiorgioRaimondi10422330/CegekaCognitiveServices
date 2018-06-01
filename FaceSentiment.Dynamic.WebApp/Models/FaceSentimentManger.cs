using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceSentiment.Dynamic.WebApp.Models
{
    public class FaceSentimentManager
    {
        //Database Variables

        /// <summary>
        /// Web Database ConnectionString
        /// </summary>
        private string databaseConnection = "";
        
        //Face Api Variables

        /// <summary>
        /// Client for FaceApi request
        /// </summary>
        private IFaceServiceClient _faceServiceClient;

        /// <summary>
        /// Url of the FaceApi
        /// </summary>
        private string uriBase = "";

        /// <summary>
        /// KeyValue of the Face Api
        /// </summary>
        private string faceApiKey = "";


        //Generic Variable

        /// <summary>
        /// String of the error found during Execution
        /// </summary>
        private string errorMessage = "";

        /// <summary>
        /// Array of Face Detected (with all attributes of the analysis)
        /// </summary>
        private Face[] faces;

        /// <summary>
        /// Number of Face found during faceApi detection
        /// </summary>
        private int numOfFaceDetected = 0;


        //Face Tracking in Time

        /// <summary>
        /// Container of People previuosly seen
        /// </summary>
        PersonSeenList trackList;

        /// <summary>
        /// Confidence Value ([0-1]) to if a face is symilar to an other
        /// </summary>
        private double confidenceLevel = 0.4;

        /// <summary>
        /// Max number of comparable Faces
        /// </summary>
        private int maxNumberOfFaceComparable = 100;

        /// <summary>
        /// Identifier of the camera which detected the face
        /// </summary>
        private string cameraId = "";

        /// <summary>
        /// Manager to save results on Db
        /// </summary>
        private IDbManager dbManager = new DbManager();

        /// <summary>
        /// Default constructor
        /// </summary>
        public FaceSentimentManager()
        {
            _faceServiceClient = new FaceServiceClient(faceApiKey, uriBase);
            trackList = new PersonSeenList();
        }

        //Properties
        public string ErrorMessage { get => errorMessage; }
        public int NumOfFaceDetected { get => numOfFaceDetected; private set => numOfFaceDetected = value; }
        public string CameraId { get => cameraId; private set => cameraId = value; }


        //Public Methods

        /// <summary>
        /// Set the connections to the Db and the camera values
        /// </summary>
        /// <param name="connections">Connections Settings</param>
        /// <param name="camera">Camera Settings (CameraId required)</param>
        public void SetConnections(Connections connections, CameraValues camera)
        {
            databaseConnection = connections.DbConnectionString;
            faceApiKey = connections.FaceApiKey;
            uriBase = connections.FaceApiUrl;
            cameraId = camera.Id.ToString();

            _faceServiceClient = new FaceServiceClient(faceApiKey, uriBase);
        }

        /// <summary>
        /// Add the detected face to Db Asyncronously
        /// </summary>
        /// <returns>True if it all correctly saved</returns>
        public async Task<bool> AddToDbAsync()
        {
            if (faces == null || faces.Length == 0)
                return false;
            bool allCorrect = true;
            DateTime date = DateTime.Now;
            try
            {
                await dbManager.SaveFaceSentiment(faces, cameraId, date);
            }catch(Exception ex)
            {
                allCorrect = false;
                errorMessage = ex.Message;
            }
            if (allCorrect)
                errorMessage = "";
            return allCorrect;
        }
        
        /// <summary>
        /// Detect facesentiment from a bitmap frame 
        /// (it uses the Overload method with MemoryStream)
        /// </summary>
        /// <param name="bit">Bitmap containing the frame to Analyse</param>
        /// <returns>True if a face is detected with no error</returns>
        public async Task<bool> DetectFaceSentiment(Bitmap bit)
        {
            using (MemoryStream ms = new MemoryStream()) {
                bit.Save(ms, ImageFormat.Jpeg);
                return await DetectFaceSentiment(ms);
            }
        }
        
        /// <summary>
        /// Detect facesentiment from a MemoryStream containg the frame to analyse
        /// </summary>
        /// <param name="ms">MemoryStream containing the frame to Analyse</param>
        /// <returns>True if a face is detected with no error</returns>
        public async Task<bool> DetectFaceSentiment(MemoryStream ms)
        {
            bool result = true;
            numOfFaceDetected = 0;
            faces = null;
            Bitmap image = new Bitmap(ms);

            try
            {
                var faceAttributes = new FaceAttributeType[] {
                    FaceAttributeType.Emotion ,
                    FaceAttributeType.Age,
                    FaceAttributeType.Blur,
                    FaceAttributeType.Gender,
                    FaceAttributeType.Smile,
                    FaceAttributeType.FacialHair,
                    FaceAttributeType.Glasses,
                    FaceAttributeType.Hair,
                    FaceAttributeType.Makeup,
                    FaceAttributeType.Accessories
                };
                using (var fromBitmapToBit = new MemoryStream())
                {
                    image.Save(fromBitmapToBit, ImageFormat.Jpeg);
                    using (var imgStream = new MemoryStream(fromBitmapToBit.ToArray()))
                    {
                        faces = await _faceServiceClient.DetectAsync(imgStream, true, false, faceAttributes);
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Error on FaceSentiment Detection\n" + ex.Message;
                result = false;
            }
            numOfFaceDetected = faces != null ? faces.Length : 0;
            if (numOfFaceDetected == 0)
            {
                errorMessage = "Request Correctly sent but no face Detected";
                result = false;
            }
            else
            {
                try
                {
                    await FindSimilarFaces();
                }
                catch (Exception ex)
                {
                    foreach (var face in faces)
                    {
                        trackList.AddFace(face.FaceId);
                    }
                    errorMessage = "Error on Similar FaceDetection:\n" + ex.Message;
                }
            }
            return result;
        }

        //Private Method

        /// <summary>
        /// Check if in previous detections it was found the same face 
        /// and associate it to a PersonId
        /// </summary>
        private async Task FindSimilarFaces()
        {
            trackList.SmoothDimension();
            var faceIdList = trackList.GetFaceId();
            if (faceIdList.Length != 0)
            {
                foreach (var face in faces)
                {
                    Guid faceId = face.FaceId;
                    var similarFace = await _faceServiceClient.FindSimilarAsync(face.FaceId, faceIdList, FindSimilarMatchMode.matchFace, 1);

                    if (similarFace != null && similarFace[0].Confidence > confidenceLevel)
                    {
                        trackList.AddFace(faceId, true, similarFace[0].FaceId);
                    }
                    else
                    {
                        trackList.AddFace(faceId);
                    }
                }
            }
            else
            {
                foreach (var face in faces)
                {
                    trackList.AddFace(face.FaceId);
                }
            }
        }
    }
}
