using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceSentiment.Dynamic.WebApp.Models
{
    /// <summary>
    /// Class to Store the value of the main Connections variables
    /// </summary>
    public class Connections
    {
        //Connections Variables

        // Connection String to DB
        [Required]
        [DisplayName("Database Connectionstring")]
        public string DbConnectionString { get; set; }
        // Key Value to FaceApi
        [Required]
        [DisplayName("Azure Face api key")]
        public string FaceApiKey { get; set; }
        // Url to FaceApi
        [Required]
        [DisplayName("Azure Face api url")]
        public string FaceApiUrl { get; set; }
        //Time before try to restart Connection

        [Required]
        [DisplayName("Second of Timeout")]
        public int SecondTimeout { get; set; }
        //Times for trying to restart Connection
        [Required]
        [DisplayName("Times of Restart")]
        public int TimesOfRestart { get; set; }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public Connections()
        {
                
        }

        /// <summary>
        /// Constructor which create a Connection using a stringify Connection
        /// </summary>
        /// <param name="raw">Stringify Connection</param>
        public Connections(string raw)
        {
            var value = raw.Trim().Split(new string[] { "|||" }, StringSplitOptions.None);
            DbConnectionString = value[0];
            FaceApiKey = value[1];
            FaceApiUrl = value[2];
            SecondTimeout = Int32.Parse(value[3]);
            TimesOfRestart = Int32.Parse(value[4]);
        }

        /// <summary>
        /// Clone the current Connection
        /// </summary>
        /// <returns>Clone of the current Connection</returns>
        public Connections Clone()
        {
            Connections conn = new Connections();
            conn.DbConnectionString = this.DbConnectionString;
            conn.FaceApiKey = this.FaceApiKey;
            conn.FaceApiUrl = this.FaceApiUrl;
            conn.SecondTimeout = this.SecondTimeout;
            conn.TimesOfRestart = this.TimesOfRestart;
            return conn;
        }

        /// <summary>
        /// Overload of the toString method to Stringify the Connection
        /// </summary>
        /// <returns>Connection stringified</returns>
        public override string ToString()
        {
            string s;
            s = DbConnectionString + "|||";
            s += FaceApiKey + "|||";
            s += FaceApiUrl + "|||";
            s += SecondTimeout + "|||";
            s += TimesOfRestart ;
            return s;
        }
        
    }
}
