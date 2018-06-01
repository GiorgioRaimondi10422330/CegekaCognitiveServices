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
    /// Container for Detection Setting Variables
    /// </summary>
    public class DetectionSetting
    {
        //Detection Setting Variables

        //Seconds for any detection
        [Required]
        [DisplayName("Second for Detection")]
        [Range(1,120,ErrorMessage ="Insert a value between 1 to 120")]
        public int SecondForDetection { get; set; }
        //Minimal dimension in pixel of rectangles in which search for Eye
        [Required]
        [DisplayName("Minimal Eye Resolution")]
        [Range(1,100,ErrorMessage ="Insert a value between 1 to 100")]
        public int EyeMinDimension { get; set; }
        //Minimal dimension in pixel of rectangles in which search for Face
        [Required]
        [DisplayName("Minimal Face Resolution")]
        [Range(1, 100, ErrorMessage = "Insert a value between 1 to 100")]
        public int FaceMinDimension { get; set; }
        //Number of Neighrboors rectangle to say that an eye is contained
        [Required]
        [DisplayName("Number of Eye's Neighboors")]
        [Range(1, 100, ErrorMessage = "Insert a value between 1 to 100")]
        public int EyeNeighboors { get; set; }
        //Number of Neighrboors rectangle to say that a face is contained
        [Required]
        [DisplayName("Number of Face's Neighboors")]
        [Range(1, 100, ErrorMessage = "Insert a value between 1 to 100")]
        public int FaceNeighboors { get; set; }
        //Precision percentage for Eye

        [Required]
        [DisplayName("Eye Percentage")]
        [Range(1.1, 10.0, ErrorMessage = "Insert a value between 1.1 to 10")]
        public double EyePercentage { get; set; }
        //Precision percentage for Face
        [Required]
        [DisplayName("Face Percentage")]
        [Range(1.1, 10.0, ErrorMessage = "Insert a value between 1.1 to 10")]
        public double FacePercentage { get; set; }
        //Precision to say that a face rectagle moved

        [Required]
        [DisplayName("Squares tracking precision")]
        [Range(0.0,1.0, ErrorMessage = "Insert a value between 0.0 to 1.0")]
        public double TrackPrecision { get; set; }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public DetectionSetting()
        {
                
        }

        /// <summary>
        /// Constructor that create a new Detection Setting 
        /// from a Stringify detection setting
        /// </summary>
        /// <param name="row">Stringify detection setting</param>
        public DetectionSetting(string row)
        {
            var value = row.Trim().Split(new string[] { "|||" }, StringSplitOptions.None);
            SecondForDetection = Int32.Parse(value[0]);
            EyeMinDimension = Int32.Parse(value[1]);
            FaceMinDimension = Int32.Parse(value[2]);
            EyeNeighboors = Int32.Parse(value[3]);
            FaceNeighboors = Int32.Parse(value[4]);
            EyePercentage = Double.Parse(value[5]);
            FacePercentage = Double.Parse(value[6]);
            TrackPrecision = Double.Parse(value[7]);
        }

        /// <summary>
        /// Method to Clone the current Detection setting
        /// </summary>
        /// <returns>Detections Setting Cloned</returns>
        public DetectionSetting Clone()
        {
            return new DetectionSetting()
            {
                SecondForDetection = this.SecondForDetection,
                EyeMinDimension = this.EyeMinDimension,
                FaceMinDimension = this.FaceMinDimension,
                EyeNeighboors = this.EyeNeighboors,
                FaceNeighboors = this.FaceNeighboors,
                EyePercentage = this.EyePercentage,
                FacePercentage = this.FacePercentage,
                TrackPrecision = this.TrackPrecision
            };
        }

        /// <summary>
        /// Overload of the toString method to Stringify the Detection Setting
        /// </summary>
        /// <returns>Detection Setting Stringified</returns>
        public override string ToString()
        {
            string s;
            s = SecondForDetection + "|||";
            s +=EyeMinDimension + "|||";
            s +=FaceMinDimension + "|||";
            s +=EyeNeighboors + "|||";
            s +=FaceNeighboors + "|||";
            s +=EyePercentage + "|||";
            s +=FacePercentage + "|||";
            s +=TrackPrecision ;
            return s;
        }
    }
}
