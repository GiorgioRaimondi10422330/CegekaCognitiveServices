using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceSentiment.Dynamic.WebApp.Models
{
    public class FaceDetectionManager
    {
        //Auxiliar Element
        private double precisionTrack ;
        private string errorMessage = "None";

        //Face and Eye Position Variable
        
        //-Classifier Class (to detect want is in prefix)
        private CascadeClassifier faceClassifier;
        private CascadeClassifier eyeLeftClassifier;
        private CascadeClassifier eyeRightClassifier;

        //-FilePath to Classifier xml
        private string faceHaarPath;
        private string eyeLeftPath;
        private string eyeRightPath;

        //-OldRectangle to Compare with new Rectangles
        private Rectangle[] oldFaceRectangles;
        private Rectangle[] oldEyeLeftRectangles;
        private Rectangle[] oldEyeRightRectangles;

        //-Empty Rectangle
        private Rectangle[] defaultRect = new Rectangle[] { new Rectangle(new Point(0, 0), new Size(0, 0)) };

        //Settings
        private int eyeMinDimension = 1;
        private int faceMinDimension = 1;
        private double eyePercentage = 1.1;
        private double facePercentage = 1.1;
        private int eyeNeighboors = 5;
        private int faceNeighboors = 3;


        /// <summary>
        /// Default Constructor
        /// </summary>
        public FaceDetectionManager()
        {
            string root = System.Web.HttpContext.Current.Server.MapPath("~/Settings");

            faceHaarPath = Path.Combine(root,"haarcascadeFile", "haarcascade_frontalface_alt_tree.xml");
            eyeLeftPath = Path.Combine(root, "haarcascadeFile", "haarcascade_lefteye_2splits.xml");
            eyeRightPath = Path.Combine(root, "haarcascadeFile", "haarcascade_righteye_2splits.xml");

            faceClassifier = new CascadeClassifier(faceHaarPath);
            eyeLeftClassifier = new CascadeClassifier(eyeLeftPath);
            eyeRightClassifier = new CascadeClassifier(eyeRightPath);

            oldFaceRectangles = defaultRect;
            oldEyeLeftRectangles = defaultRect;
            oldEyeRightRectangles = defaultRect;
        }


        //Properties
        public double PrecisionTrack{ get => precisionTrack; set => precisionTrack = SetPrecision(value); }
        public string ErrorMessage { get => errorMessage; }
        public int EyeMinDimension { get => eyeMinDimension; set => eyeMinDimension = value; }
        public int FaceMinDimension { get => faceMinDimension; set => faceMinDimension = value; }
        public double EyePercentage { get => eyePercentage; set => eyePercentage = value; }
        public double FacePercentage { get => facePercentage; set => facePercentage = value; }
        public int EyeNeighboors { get => eyeNeighboors; set => eyeNeighboors = value; }
        public int FaceNeighboors { get => faceNeighboors; set => faceNeighboors = value; }

        //Public Method

        /// <summary>
        /// Set the detection Settings
        /// </summary>
        /// <param name="det">Container of the detection setting to setup</param>
        public void SetDetectionsSetting(DetectionSetting det)
        {
            eyeMinDimension = det.EyeMinDimension;
            faceMinDimension = det.FaceMinDimension;
            eyeNeighboors = det.EyeNeighboors;
            faceNeighboors = det.FaceNeighboors;
            eyePercentage = det.EyePercentage;
            facePercentage = det.FacePercentage;
            precisionTrack = det.TrackPrecision;
        }

        /// <summary>
        /// Check if in the image there are any faces (face, eye)
        /// Compare the face/eye position rectangles with the previous detection
        /// </summary>
        /// <param name="image">Image where detect the face</param>
        /// <returns>True if the face is tracked</returns>
        public bool CheckAndTrackFaces(Bitmap image)
        {
            try
            {
                if (image == null)
                {
                    errorMessage = "Error Image for Facedetection is Empty";
                    return false;
                }

                errorMessage = "None";
                Image<Bgr, Byte> currentFrame = new Image<Bgr, byte>(image);
                Image<Gray, Byte> grayFrame = currentFrame.Convert<Gray, Byte>();

                var faceRectangles = faceClassifier.DetectMultiScale(grayFrame, facePercentage, faceNeighboors, new Size(faceMinDimension, faceMinDimension));
                var eyeLeftRectangles = eyeLeftClassifier.DetectMultiScale(grayFrame, eyePercentage, eyeNeighboors, new Size(eyeMinDimension, eyeMinDimension));
                var eyeRightRectangles = eyeRightClassifier.DetectMultiScale(grayFrame, eyePercentage, eyeNeighboors, new Size(eyeMinDimension, eyeMinDimension));

                if ((faceRectangles == null || faceRectangles.Length == 0) && (eyeLeftRectangles == null || eyeLeftRectangles.Length == 0) && (eyeRightRectangles == null || eyeRightRectangles.Length == 0))
                {
                    oldFaceRectangles = defaultRect;
                    oldEyeLeftRectangles = defaultRect;
                    oldEyeRightRectangles = defaultRect;
                    errorMessage = "No face detected";
                    return false;
                }

                bool noChange = TrackerNotChanged(faceRectangles, oldFaceRectangles, eyeLeftRectangles, oldEyeLeftRectangles, eyeRightRectangles, oldEyeRightRectangles, precisionTrack);

                if (noChange)
                {
                    errorMessage = "Face detected, but no Face tracking";
                    return false;
                }
                oldEyeLeftRectangles = eyeLeftRectangles;
                oldEyeRightRectangles = eyeRightRectangles;
                oldFaceRectangles = faceRectangles;
                return true;

            }
            catch (Exception ex)
            {
                errorMessage = "Error during face detection\n" + ex.ToString();
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    errorMessage += "\n" + ex.ToString();
                }
                //throw new Exception("Error during face detection", ex);
                return false;
            }
        }

        //Private Methods

        /// <summary>
        /// Try to track the rectangle of face and eye
        /// </summary>
        /// <param name="recsFace">Rectangles of the faces detected</param>
        /// <param name="oldRecsFace">Rectangles of the faces detected in the previous detection</param>
        /// <param name="recsLeftEye">Rectangles of the left eyes detected</param>
        /// <param name="oldRecsLeftEye">Rectangles of the left eyes detected in the previous detection</param>
        /// <param name="recsRightEye">Rectangles of the right eyes detected</param>
        /// <param name="oldRecsRightEye">Rectangles of the right eyes detected in the previous detection</param>
        /// <param name="prec">Precision parameter (0-1)</param>
        /// <returns>True if the face/eyes are in the same position of previous detection</returns>
        private bool TrackerNotChanged(Rectangle[] recsFace, Rectangle[] oldRecsFace, Rectangle[] recsLeftEye, Rectangle[] oldRecsLeftEye, Rectangle[] recsRightEye, Rectangle[] oldRecsRightEye, double prec = 0.1)
        {
            //Check changes of number of rectangles
            if ((recsFace != null && recsFace.Length != oldRecsFace.Length) ||
                (recsLeftEye != null && recsLeftEye.Length != oldRecsLeftEye.Length) ||
                (recsRightEye != null && recsRightEye.Length != oldRecsRightEye.Length))
                return false;

            //Check changes on Face rectangles
            if (recsFace != null && recsFace.Length != 0)
                return AllEqualRectangle(recsFace, oldRecsFace, (int)Math.Round(recsFace[0].Height * prec));

            //Check changes on Eyes
            bool equalRight;
            bool equalLeft;

            if (recsLeftEye == null || recsLeftEye.Length == 0)
                equalLeft = true;
            else
                equalLeft = AllEqualRectangle(recsLeftEye, oldRecsLeftEye, (int)Math.Round(recsLeftEye[0].Height * prec));

            if (recsRightEye == null || recsRightEye.Length == 0)
                equalRight = true;
            else
                equalRight = AllEqualRectangle(recsRightEye, oldRecsRightEye, (int)Math.Round(recsRightEye[0].Height * prec));

            return equalLeft && equalRight;
        }


        /// <summary>
        /// Track if a rectangles have the same position of the previous detection
        /// </summary>
        /// <param name="recs">Rectangles detected</param>
        /// <param name="oldRecs">Rectangles detected in the previous detection</param>
        /// <param name="delta">Precision pixel in order to say that two rectangles are the same</param>
        /// <returns>True if the rectangles are considered in the same position</returns>
        private bool AllEqualRectangle(Rectangle[] recs, Rectangle[] oldRecs, int delta = 3)
        {
            if (recs == null || recs.Length == 0 || oldRecs == null || oldRecs.Length == 0)
                return false;
            bool allEqual;
            bool equal;
            foreach (var rec in recs)
            {
                allEqual = false;
                foreach (var oldrec in oldRecs)
                {
                    equal = true;
                    if (rec.Location.X > oldrec.Location.X + delta || rec.Location.X < oldrec.Location.X - delta)
                        equal = false;
                    if (rec.Location.Y > oldrec.Location.Y + delta || rec.Location.Y < oldrec.Location.Y - delta)
                        equal = false;
                    if (rec.Location.X > oldrec.Location.X + delta || rec.Location.X < oldrec.Location.X - delta)
                        equal = false;
                    if (rec.Location.X > oldrec.Location.X + delta || rec.Location.X < oldrec.Location.X - delta)
                        equal = false;
                    if (equal)
                    {
                        allEqual = true;
                        break;
                    }
                }
                if (!allEqual)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Check if the passed parameter is in the correct interval (value in [0,1])
        /// </summary>
        /// <param name="value">Value of precision track</param>
        /// <returns>Return the value in the correct interval</returns>
        private double SetPrecision(double value)
        {
            if (value > 1.0)
                return 1.0;
            if (value < 0.0)
                return 0.0;
            return value;
        }
    }
}
