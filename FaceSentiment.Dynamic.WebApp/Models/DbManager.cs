using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceSentiment.Dynamic.WebApp.Models
{
    /// <summary>
    /// Class to Access the WebDatabase in the most simple Way
    /// </summary>
    public class DbManager : IDbManager
    {
        //Connection to the DB
        private static Connections connections;

        //String to Execute the comands
        private const string savePersonString = "INSERT INTO [dbo].[Person] ([FaceId],[PersonId],[Date]) VALUES (@FaceId,@PersonId,@Date)";
        private const string readPersonString = "SELECT * FROM [dbo].[Person] WHERE [Date] BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 1, CAST(GETDATE() AS DATE))";
        private const string saveFaceSentimentString = @"INSERT INTO dbo.[Table] ([Age],  [Gender],  [EmotionAnger],  [EmotionContempt],  [EmotionDisgust],  [EmotionFear],  [EmotionHappiness],  [EmotionNeutral],  [EmotionSadness],  [EmotionSurprise],  [Blur], [Date],  [Smile],[FaceId],[HairBald],[HairInvisible],[HairColorFirst],[HairColorSecond],[Moustache],[Beard],[Sideburns],[Glasses],[MakeupEye],[MakeupLip],[Accessories1],[Accessories2],[CameraId]) VALUES (@Age, @Gender, @EmotionAnger, @EmotionContempt, @EmotionDisgust, @EmotionFear, @EmotionHappiness, @EmotionNeutral, @EmotionSadness, @EmotionSurprise, @Blur,@Date, @Smile,@FaceId,@HairBald,@HairInvisible,@HairColorFirst,@HairColorSecond,@Moustache,@Beard,@Sideburns,@Glasses,@MakeupEye,@MakeupLip,@Accessories1,@Accessories2,@CameraId)";
        private const string readCameraString = "SELECT * FROM [dbo].[Camera]";
        private const string saveCameraString = @"INSERT INTO [dbo].[Camera] ([IdCamera],  [Type],  [Locality],  [Position],[ConnectionString],[User],[Password],[UseDifferentStream],[UseComputerWebCam]) VALUES (@IdCamera, @Type, @Locality, @Position, @ConnectionString,@User,@Password,@UseDifferentStream,@UseComputerWebCam);";
        private const string deleteCameraString = "DELETE FROM [dbo].[Camera] WHERE [IdCamera]=@IdCamera;";
        private const string upDateCameraString = @"UPDATE [dbo].[Camera] SET [Type]=@Type, [Locality]=@Locality, [Position]=@Position, [ConnectionString]=@ConnectionString, [User]=@User,[Password]=@Password,[UseDifferentStream]=@UseDifferentStream,[UseComputerWebCam]=@UseComputerWebCam WHERE [IdCamera]=@IdCamera;";

        /// <summary>
        /// Default Constructor
        /// </summary>
        public DbManager()
        {
            if (connections == null)
            {
                Connections con = new Connections();
                con.DbConnectionString = "Server = tcp:cegekafacesentiment.database.windows.net,1433; Initial Catalog = FaceSentimentDatabase; Persist Security Info = False; User ID = ServerAdmin; Password = Familyfour4; MultipleActiveResultSets = False; Encrypt = True; TrustServerCertificate = False; Connection Timeout = 30;";
                con.FaceApiKey = "57807fd35bf54f0fa59be422bd39a3a5";
                con.FaceApiUrl = @"https://westeurope.api.cognitive.microsoft.com/face/v1.0";
                connections = con;
            }
        }

        /// <summary>
        /// Function to set the connection to Web DB
        /// </summary>
        /// <param name="conn">Connection to the DB</param>
        public void SetConnection(Connections conn)
        {
            connections = conn.Clone();
        }

        /// <summary>
        /// Function to Save a person to Db
        /// </summary>
        /// <param name="faceId">Guid Id of the faceDetected</param>
        /// <param name="personId">Guid Id of the person</param>
        /// <param name="date">Time of the rilevament</param>
        public async void SavePerson(Guid faceId, Guid personId, DateTime date)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connections.DbConnectionString))
                {
                    conn.Open();
                    SqlCommand savePersCom = new SqlCommand(savePersonString, conn);
                    savePersCom.Parameters.Add("@FaceId", SqlDbType.NVarChar).Value = faceId.ToString();
                    savePersCom.Parameters.Add("@PersonId", SqlDbType.NVarChar).Value = personId.ToString();
                    savePersCom.Parameters.Add("@Date", SqlDbType.DateTime).Value = date;

                    var done = await savePersCom.ExecuteNonQueryAsync();
                    if (done == -1)
                    {
                        throw new Exception(savePersCom.CommandText);
                    }
                    conn.Close();
                }
            }
            catch(Exception ex)
            {
                throw new Exception("Error on Saving Person on DB\n" + ex.Message);
            }
        }

        /// <summary>
        /// Read all people from DB detected in the current day 
        /// and save them into the passed variable
        /// </summary>
        /// <param name="people">Container of people seen in the current day</param>
        public async void ReadPerson(PersonSeenList people)
        {
            try
            {
                List<Guid> faceIds = new List<Guid>();
                List<Guid> peopleIds = new List<Guid>();
                List<DateTime> timeFace = new List<DateTime>();
                using (SqlConnection conn = new SqlConnection(connections.DbConnectionString))
                {
                    conn.Open();
                    SqlDataReader personReader;
                    try
                    {
                        SqlCommand readComand = new SqlCommand(readPersonString, conn);
                        personReader =await readComand.ExecuteReaderAsync();
                    } catch (Exception ex)
                    {
                        throw new Exception("Unable to Read People Data\n" + ex.Message);
                    }
                    if (personReader.HasRows)
                    {
                        while (personReader.Read())
                        {
                            try
                            {
                                faceIds.Add(new Guid((string)personReader["FaceId"]));
                                peopleIds.Add(new Guid((string)personReader["PersonId"]));
                                timeFace.Add((DateTime)personReader["Date"]);
                            }
                            catch
                            {

                            }
                        }
                    }
                    conn.Close();
                }
                people.Set(faceIds,peopleIds,timeFace);
            }
            catch (Exception ex)
            {
                throw new Exception("Error on Reading from DB\n" + ex.Message);
            }
        }

        /// <summary>
        /// Save the detected Face and their analysis on Web Db
        /// </summary>
        /// <param name="faces">Array Container of face analysis parameters</param>
        /// <param name="cameraId">String of the Guid Id of the camera which detected the face</param>
        /// <param name="date">Time of face Detection</param>
        /// <returns>
        /// Boolean if the savage went all right.
        /// The function is async so it return a Task of a Boolean
        /// </returns>
        public async Task<bool> SaveFaceSentiment(Face[] faces, string cameraId, DateTime date)
        {
            using (SqlConnection conn = new SqlConnection(connections.DbConnectionString))
            {
                foreach (var face in faces)
                {
                    try
                    {
                        conn.Open();
                        SqlCommand saveFaceCom = new SqlCommand(saveFaceSentimentString, conn);

                        saveFaceCom.Parameters.Add("@Age", SqlDbType.Float).Value = face.FaceAttributes.Age;
                        saveFaceCom.Parameters.Add("@Gender", SqlDbType.NChar).Value = face.FaceAttributes.Gender;
                        saveFaceCom.Parameters.Add("@EmotionAnger", SqlDbType.Float).Value = face.FaceAttributes.Emotion.Anger;
                        saveFaceCom.Parameters.Add("@EmotionContempt", SqlDbType.Float).Value = face.FaceAttributes.Emotion.Contempt;
                        saveFaceCom.Parameters.Add("@EmotionDisgust", SqlDbType.Float).Value = face.FaceAttributes.Emotion.Disgust;
                        saveFaceCom.Parameters.Add("@EmotionFear", SqlDbType.Float).Value = face.FaceAttributes.Emotion.Fear;
                        saveFaceCom.Parameters.Add("@EmotionHappiness", SqlDbType.Float).Value = face.FaceAttributes.Emotion.Happiness;
                        saveFaceCom.Parameters.Add("@EmotionNeutral", SqlDbType.Float).Value = face.FaceAttributes.Emotion.Neutral;
                        saveFaceCom.Parameters.Add("@EmotionSadness", SqlDbType.Float).Value = face.FaceAttributes.Emotion.Sadness;
                        saveFaceCom.Parameters.Add("@EmotionSurprise", SqlDbType.Float).Value = face.FaceAttributes.Emotion.Surprise;
                        saveFaceCom.Parameters.Add("@Blur", SqlDbType.Float).Value = face.FaceAttributes.Blur.Value;
                        saveFaceCom.Parameters.Add("@Date", SqlDbType.DateTime).Value = date;
                        saveFaceCom.Parameters.Add("@Smile", SqlDbType.Float).Value = face.FaceAttributes.Smile;
                        var faceID = face.FaceId.ToString();
                        saveFaceCom.Parameters.Add("@FaceId", SqlDbType.NVarChar).Value = faceID;

                        saveFaceCom.Parameters.Add("@HairBald", SqlDbType.Float).Value = face.FaceAttributes.Hair.Bald;
                        saveFaceCom.Parameters.Add("@HairInvisible", SqlDbType.Bit).Value = face.FaceAttributes.Hair.Invisible;

                        saveFaceCom.Parameters.Add("@Moustache", SqlDbType.Float).Value = face.FaceAttributes.FacialHair.Moustache;
                        saveFaceCom.Parameters.Add("@Beard", SqlDbType.Float).Value = face.FaceAttributes.FacialHair.Beard;
                        saveFaceCom.Parameters.Add("@Sideburns", SqlDbType.Float).Value = face.FaceAttributes.FacialHair.Sideburns;
                        saveFaceCom.Parameters.Add("@Glasses", SqlDbType.NChar).Value = face.FaceAttributes.Glasses.ToString();
                        saveFaceCom.Parameters.Add("@MakeupEye", SqlDbType.Bit).Value = face.FaceAttributes.Makeup.EyeMakeup;
                        saveFaceCom.Parameters.Add("@MakeupLip", SqlDbType.Bit).Value = face.FaceAttributes.Makeup.LipMakeup;


                        //Possible Null Value
                        var ColorList = face.FaceAttributes.Hair.HairColor;
                        if (ColorList == null || ColorList.Length == 0)
                        {
                            saveFaceCom.Parameters.Add("@HairColorFirst", SqlDbType.NChar).Value = DBNull.Value;
                            saveFaceCom.Parameters.Add("@HairColorSecond", SqlDbType.NChar).Value = DBNull.Value;
                        }
                        else if (ColorList.Length == 1)
                        {
                            saveFaceCom.Parameters.Add("@HairColorFirst", SqlDbType.NChar).Value = ColorList[0].Color.ToString();
                            saveFaceCom.Parameters.Add("@HairColorSecond", SqlDbType.NChar).Value = DBNull.Value;
                        }
                        else
                        {
                            ColorList.OrderByDescending(c => c.Confidence);
                            saveFaceCom.Parameters.Add("@HairColorFirst", SqlDbType.NChar).Value = ColorList[0].Color.ToString();
                            saveFaceCom.Parameters.Add("@HairColorSecond", SqlDbType.NChar).Value = ColorList[1].Color.ToString();
                        }
                        var ListAccessories = face.FaceAttributes.Accessories;
                        if (ListAccessories == null || ListAccessories.Length == 0)
                        {
                            saveFaceCom.Parameters.Add("@Accessories1", SqlDbType.NChar).Value = DBNull.Value;
                            saveFaceCom.Parameters.Add("@Accessories2", SqlDbType.NChar).Value = DBNull.Value;
                        }
                        else if (ListAccessories.Length == 1)
                        {
                            saveFaceCom.Parameters.Add("@Accessories1", SqlDbType.NChar).Value = ListAccessories[0].Type.ToString();
                            saveFaceCom.Parameters.Add("@Accessories2", SqlDbType.NChar).Value = DBNull.Value;
                        }
                        else
                        {

                            saveFaceCom.Parameters.Add("@Accessories1", SqlDbType.NChar).Value = ListAccessories[0].Type.ToString();
                            saveFaceCom.Parameters.Add("@Accessories2", SqlDbType.NChar).Value = ListAccessories[1].Type.ToString();
                        }
                        if (cameraId == null || cameraId == "")
                        {
                            saveFaceCom.Parameters.Add("@CameraId", SqlDbType.NVarChar).Value = DBNull.Value;
                        }
                        else
                        {
                            saveFaceCom.Parameters.Add("@CameraId", SqlDbType.NVarChar).Value = cameraId;
                        }

                        var result = await saveFaceCom.ExecuteNonQueryAsync();
                        if (result == -1)
                        {
                            throw new Exception(saveFaceCom.CommandText);
                        }
                        conn.Close();

                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error on Saving FaceOnDB\n" + ex.Message);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Read the camera setting from Web Db
        /// </summary>
        /// <returns>List of Camera settings read on Web Db (returned by Task)</returns>
        public async Task<List<CameraValues>> ReadCamera()
        {
            List<CameraValues> cameras = new List<CameraValues>();
            try
            {
                using (SqlConnection conn = new SqlConnection(connections.DbConnectionString))
                {
                    conn.Open();
                    SqlCommand readComand = new SqlCommand(readCameraString, conn);
                    SqlDataReader cameraReader = await readComand.ExecuteReaderAsync();
                    if (cameraReader.HasRows)
                    {
                        while (cameraReader.Read())
                        {
                            CameraValues cam = new CameraValues();
                            cam.Id = new Guid((string)cameraReader["IdCamera"]);
                            cam.Type = (string)cameraReader["Type"];
                            cam.Location = (string)cameraReader["Locality"];
                            cam.Position = (string)cameraReader["Position"];
                            cam.ConnectionString = (string)cameraReader["ConnectionString"];
                            cam.User = (string)cameraReader["User"];
                            cam.Password = (string)cameraReader["Password"];
                            cam.MjpegStreamer = !(bool)cameraReader["UseDifferentStream"];
                            cam.LocalWebcam = (bool)cameraReader["UseComputerWebCam"];
                            cam.Fps = 5;
                            cam.SecondTimeOut = 20;
                            cam.Status = "Stopped";
                            cameras.Add(cam);
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error on Reading Cameras From DB\n" + ex.Message);
            }
            return cameras;
        }

        /// <summary>
        /// Update/Create a Camera Setting on Web Db
        /// </summary>
        /// <param name="camera">Camera Setting to Update/Create</param>
        public async Task<bool> UpdateCamera(CameraValues camera)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connections.DbConnectionString))
                {
                    conn.Open();
                    SqlCommand readComand = new SqlCommand(readCameraString, conn);
                    bool UpDate = false;
                    using (SqlDataReader cameraReader = await readComand.ExecuteReaderAsync())
                    {
                        if (cameraReader.HasRows)
                        {
                            while (cameraReader.Read())
                            {
                                if (camera.Id == new Guid((string)cameraReader["IdCamera"]))
                                {
                                    UpDate = true;
                                    break;
                                }
                            }
                        }
                    }

                    SqlCommand updateComand;
                    if (UpDate)
                    {
                        updateComand = new SqlCommand(upDateCameraString, conn);
                    }
                    else
                    {
                        updateComand = new SqlCommand(saveCameraString, conn);
                    }
                    updateComand.Parameters.Add("@IdCamera", SqlDbType.NVarChar).Value = camera.Id.ToString();
                    updateComand.Parameters.Add("@Type", SqlDbType.NVarChar).Value = camera.Type;
                    updateComand.Parameters.Add("@Locality", SqlDbType.NVarChar).Value = camera.Location;
                    updateComand.Parameters.Add("@Position", SqlDbType.NVarChar).Value = camera.Position;
                    updateComand.Parameters.Add("@ConnectionString", SqlDbType.NVarChar).Value = camera.ConnectionString;
                    updateComand.Parameters.Add("@User", SqlDbType.NVarChar).Value = camera.User;
                    updateComand.Parameters.Add("@Password", SqlDbType.NVarChar).Value = camera.Password;
                    updateComand.Parameters.Add("@UseDifferentStream", SqlDbType.Bit).Value = !camera.MjpegStreamer;
                    updateComand.Parameters.Add("@UseComputerWebCam", SqlDbType.Bit).Value = camera.LocalWebcam;
                    var done = await updateComand.ExecuteNonQueryAsync();
                    if (done == -1)
                    {
                        throw new Exception(updateComand.CommandText);
                    }

                    conn.Close();
                }
            }catch(Exception ex)
            {
                throw new Exception("Error on Updating Camera DB\n" + ex.Message);
            }
            return true;
        }

        /// <summary>
        /// Delete a Camera Setting from Web Db
        /// </summary>
        /// <param name="camera">Camera Setting to delete</param>
        public async Task<bool> DeleteCamera(CameraValues camera)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connections.DbConnectionString))
                {
                    conn.Open();
                    SqlCommand deleteCameraComand = new SqlCommand(deleteCameraString, conn);
                    deleteCameraComand.Parameters.Add("@IdCamera", SqlDbType.NVarChar).Value = camera.Id.ToString();

                    var done = await deleteCameraComand.ExecuteNonQueryAsync();
                    if (done == -1)
                    {
                        throw new Exception(deleteCameraComand.CommandText);
                    }
                }
            }catch(Exception ex)
            {
                throw new Exception("Error on Deleting Camera from Db" + ex.Message);
            }
            return true;
            
        }

        public List<CameraValues> ReadCamera(string filePath)
        {
            var result = new List<CameraValues>();
            try
            {
                var cams=System.IO.File.ReadAllLines(filePath);
                foreach(var cam in cams)
                {
                    result.Add(new CameraValues(cam));
                }
            }catch(Exception ex){
                return null;
            }
            return result;
        }

        public void UpdateCamera(CameraValues camera, string filePath)
        {
            try
            {
                bool updated = false;
                var cams = System.IO.File.ReadAllLines(filePath).ToList();
                for(int i=0;i<cams.Count; i++)
                {
                    if (cams[i].Contains(camera.Id.ToString()))
                    {
                        cams[i] = camera.Stringify();
                        updated = true;
                        break;
                    }
                }
                if (!updated)
                {
                    cams.Add(camera.Stringify());
                }
                System.IO.File.WriteAllLines(filePath, cams.ToArray());
            }
            catch(Exception ex)
            {
                return;
            }
        }

        public void DeleteCamera(CameraValues camera, string filePath)
        {
            try
            {
                var cams = System.IO.File.ReadAllLines(filePath).ToList();
                for (int i = 0; i < cams.Count; i++)
                {
                    if (cams[i].Contains(camera.Id.ToString()))
                    {
                        cams.RemoveAt(i);
                        break;
                    }
                }
                System.IO.File.WriteAllLines(filePath, cams.ToArray());
            }
            catch (Exception ex)
            {
                return;
            }
        }

        public void UpdateConnections(Connections connections, string filePath)
        {
            try
            {
                System.IO.File.WriteAllLines(filePath, new string[] { connections.ToString() });
            }catch(Exception ex)
            {
                return;
            }
        }
        
        public void UpdateDetections(DetectionSetting detection, string filePath)
        {
            try
            {
                System.IO.File.WriteAllLines(filePath, new string[] { detection.ToString() });
            }
            catch (Exception ex)
            {
                return;
            }
        }

        public Connections ReadConnections(string filePath)
        {
            try
            {
                return new Connections(System.IO.File.ReadAllLines(filePath).FirstOrDefault());
            }
            catch
            {
                return null;
            }
        }

        public DetectionSetting ReadDetections(string filePath)
        {
            try
            {
                return new DetectionSetting(System.IO.File.ReadAllLines(filePath).FirstOrDefault());
            }
            catch
            {
                return null;
            }
        }
    }
}
