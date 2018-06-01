using System;
using System.Collections.Generic;

namespace FaceSentiment.Dynamic.WebApp.Models
{
    /// <summary>
    /// Class which contains and shares the People already during the current day
    /// </summary>
    public sealed class PersonSeenList
    {
        //Container of People

        //List of Person Id
        private List<Guid> peopleId;
        //List of Face Id
        private List<Guid> faceId;
        //List of Time of detection
        private List<DateTime> faceTime;

        //Manager to use Web Db
        private IDbManager DB= new DbManager();

        //STATIC VARIABLES
        //Shared Container of Person
        public static PersonSeenList instance;
        //Object to control multiple access to the same variable
        private static object lockInitialization = new object();
        private static object lockVariableAccess = new object();

        //Index of the first element to save
        public int firstToSave = 0;

        /// <summary>
        /// Default Constructor 
        /// (it requires that the DbManager connections is already set)
        /// </summary>
        public PersonSeenList()
        {
            if (instance == null)
            {
                lock (lockInitialization)
                {
                    if (instance == null)
                    {
                        peopleId = new List<Guid>();
                        faceId = new List<Guid>();
                        faceTime = new List<DateTime>();
                        instance = new PersonSeenList(true);
                        LoadFaceFromDb();
                    }
                }
            }
        }

        /// <summary>
        /// Constructor which sets up the DbConnections
        /// </summary>
        /// <param name="db">DbManager with already set connections</param>
        public PersonSeenList(IDbManager db)
        {
            if (instance == null)
            {
                lock (lockInitialization)
                {
                    if (instance == null)
                    {
                        peopleId = new List<Guid>();
                        faceId = new List<Guid>();
                        faceTime = new List<DateTime>();
                        instance = new PersonSeenList(true);
                        instance.DB = db;
                        LoadFaceFromDb();
                    }
                }
            }
        }

        /// <summary>
        /// Constructor for instance value
        /// </summary>
        public PersonSeenList(bool nothing)
        {
            peopleId = new List<Guid>();
            faceId = new List<Guid>();
            faceTime = new List<DateTime>();
        }

        /// <summary>
        /// Method to require the FaceId of people already Seen
        /// </summary>
        /// <returns></returns>
        public Guid[] GetFaceId()
        {
            Guid[] faceIdArray;
            lock (lockVariableAccess)
            {
                faceIdArray = instance.faceId.ToArray();
            }

            if (faceIdArray == null)
                return new Guid[] { };
            return faceIdArray;
        }
        public Guid AddFace(Guid newFaceId, bool similar = false, Guid newPersonId = new Guid())
        {
            Guid personId;
            lock (lockVariableAccess)
            {
                instance.faceTime.Add(DateTime.Now);
                instance.faceId.Add(newFaceId);
                if (similar)
                {
                    int i = instance.faceId.FindIndex(f => f == newPersonId);
                    instance.peopleId.Add(instance.peopleId[i]);
                    personId = instance.peopleId[i];
                }
                else
                {
                    instance.peopleId.Add(newFaceId);
                    personId = newFaceId;
                }
            }
            SaveOnDb();
            return personId;
        }
        public void SaveOnDb()
        {
            try
            {
                lock (lockVariableAccess)
                {
                    for (int i = instance.firstToSave; i < instance.faceId.Count; i++)
                    {
                        instance.DB.SavePerson(instance.faceId[i], instance.peopleId[i], instance.faceTime[i]);
                        instance.firstToSave++;
                        /*
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();
                            SqlCommand savePersCom = new SqlCommand(saveString, conn);
                            savePersCom.Parameters.Add("@FaceId", SqlDbType.NVarChar).Value = instance.faceId[i].ToString();
                            savePersCom.Parameters.Add("@PersonId", SqlDbType.NVarChar).Value = instance.peopleId[i].ToString();
                            savePersCom.Parameters.Add("@Date", SqlDbType.DateTime).Value = instance.faceTime[i];

                            var done = savePersCom.ExecuteNonQuery();
                            if (done == -1)
                            {
                                throw new Exception(savePersCom.CommandText);
                            }
                            instance.firstToSave++;
                            conn.Close();
                        }*/
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error on SavePerson on Db\n"+ex.Message);
            }
        }
        public void LoadFaceFromDb()
        {
            lock (lockVariableAccess)
            {
                if (instance.faceId.Count == 0)
                {
                    try
                    {
                        if (instance.DB == null)
                            return;
                        instance.DB.ReadPerson(this);
                        /*using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();
                            SqlCommand readComand = new SqlCommand(readString, conn);
                            SqlDataReader personReader = readComand.ExecuteReader();
                            if (personReader.HasRows)
                            {
                                while (personReader.Read())
                                {
                                    instance.faceId.Add(new Guid((string)personReader["FaceId"]));
                                    instance.peopleId.Add(new Guid((string)personReader["PersonId"]));
                                    instance.faceTime.Add((DateTime)personReader["Date"]);
                                }
                            }
                            conn.Close();
                            instance.firstToSave = instance.faceId.Count;
                            SmoothDimension();
                        }*/
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error on Db Reading Camera\n" + ex.Message, ex);
                    }
                }
            }
        }
        public void ReloadFaceFromDb()
        {
            lock (lockVariableAccess)
            {
                instance.faceId.Clear();
                instance.faceTime.Clear();
                instance.peopleId.Clear();
                if (instance.faceId.Count == 0)
                {
                    try
                    {
                        if (instance.DB == null)
                            return;
                        instance.DB.ReadPerson(this);
                        /*using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();
                            SqlCommand readComand = new SqlCommand(readString, conn);
                            SqlDataReader personReader = readComand.ExecuteReader();
                            if (personReader.HasRows)
                            {
                                while (personReader.Read())
                                {
                                    instance.faceId.Add(new Guid((string)personReader["FaceId"]));
                                    instance.peopleId.Add(new Guid((string)personReader["PersonId"]));
                                    instance.faceTime.Add((DateTime)personReader["Date"]);
                                }
                            }
                            conn.Close();
                            instance.firstToSave = instance.faceId.Count;
                            SmoothDimension();
                        }*/
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error on Db Reading Camera\n" + ex.Message, ex);
                    }
                }
            }
        }
        public void SmoothDimension(int maxNumberOfFaceComparable = 100)
        {
            int percentRemoved = (int)Math.Round(maxNumberOfFaceComparable * 0.3);

            lock (lockVariableAccess)
            {
                while (instance.faceId.Count > maxNumberOfFaceComparable)
                {
                    instance.firstToSave -= percentRemoved;
                    instance.faceId.RemoveRange(0, percentRemoved);
                    instance.faceTime.RemoveRange(0, percentRemoved);
                    instance.peopleId.RemoveRange(0, percentRemoved);
                }
            }
        }
        public void Set(List<Guid> faceIds, List<Guid> peopleIds, List<DateTime> dates)
        {
            instance.faceId = faceIds;
            instance.peopleId = peopleIds;
            instance.faceTime = dates;
            instance.firstToSave = instance.faceId.Count;
            SmoothDimension();
        }
    }
}
