using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NPI_Kinect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Difficulty factor, defaulting at 1.6
        /// </summary>
        private float difficultyFactor = 1.6f;

        /// <summary>
        /// Error margin (%)
        /// </summary>
        private float errorMargin = 0.2f;

        /// <summary>
        /// Variables to track a max of 2 movements' length
        /// </summary>
        private float travelledDistance1 = 0.0f, travelledDistance2 = 0.0f;

        /// <summary>
        /// Variables to hold a max of 2 positions through calls
        /// </summary>
        private SkeletonPoint lastPosition1, lastPosition2;

        /// <summary>
        /// Pen used for drawing visual cues for unachieved motions
        /// </summary>
        private readonly Pen MotionNotAchieved = new Pen(new SolidColorBrush(Color.FromRgb(255, 0, 0)), 10);

        /// <summary>
        /// Pen used for drawing visual cues for achieved motions
        /// </summary>
        private readonly Pen MotionAchieved = new Pen(new SolidColorBrush(Color.FromRgb(0, 255, 0)), 10);

        /// <summary>
        /// Brush used for drawing visual cues for unachieved positions
        /// </summary>
        private readonly Brush CueNotAchieved = new SolidColorBrush(Color.FromRgb(255,0,0));

        /// <summary>
        /// Brush used for drawing visual cues for achieved positions
        /// </summary>
        private readonly Brush CueAchieved = new SolidColorBrush(Color.FromRgb(0, 255, 0));

        /// <summary>
        /// Thickness of cues
        /// </summary>
        private const double CueThickness = 10;

        /// <summary>
        /// First status track of the movement routine
        /// </summary>
        private bool poseAchieved=false;

        /// <summary>
        /// Second status track of the movement routine
        /// </summary>
        private bool end = false;


        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;


        //Imagen color

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Intermediate storage for the color data received from the camera
        /// </summary>
        private byte[] colorPixels;


        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Skeleton.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Smoothed with some latency.
                // Filters out medium jitters.
                // Good for a menu system that needs to be smooth but
                // doesn't need the reduced latency as much as gesture recognition does.
                TransformSmoothParameters smoothingParam = new TransformSmoothParameters();
                {
                    smoothingParam.Smoothing = 0.5f;
                    smoothingParam.Correction = 0.1f;
                    smoothingParam.Prediction = 0.5f;
                    smoothingParam.JitterRadius = 0.1f;
                    smoothingParam.MaxDeviationRadius = 0.1f;
                };

                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set the image we display to point to the bitmap where we'll put the image data
                this.Image.Source = this.colorBitmap;

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's ColorFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                            //Aquí vamos a añadir la llamada a la función para dibujar las marcas de posición y gestos
                            this.movementRoutine(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        /// <summary>
        /// Detects the desired position
        /// </summary>
        /// <param name="skeleton">skeleton for reference</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void movementRoutine(Skeleton skeleton, DrawingContext drawingContext)
        {
            // We assume that the skeleton is being correctly tracked, as this is called from within that condition
            // We will now check the person's position and make the required corrections
            if (!minDistance(skeleton))
                this.setDistance(skeleton);
            // If the user is at the desired distance, we will start giving cues to position him or her in the first static position, until he or she reaches it
            else if (!this.poseAchieved)
                this.poseAchieved = this.pose_HoldHandsUp(skeleton, drawingContext);
            //Then, while the user doesn't break gesture, which we will consider to be lowering the hands below head joint height
            else if (this.areHandsAboveHead(skeleton) && !this.end)
            {
                //Once the user achieves the position, we will ask the user to make a gesture
                //It will involve raising his hands roughly twice the distance between his head and chest joints
                if (this.end = this.gesture_RaiseHands(skeleton, drawingContext))
                    this.positionCuesHelp.Text = "Gesto completado!**********************************************************************";
            }
            //If the user breaks gesture (hands below head) or finishes the gesture, the cycle resets, again to a static position
            else if (this.end)
            {
                this.poseAchieved = false;
                travelledDistance1 = 0.0f;
                travelledDistance2 = 0.0f;
                this.end = false;
            }

            //Update the last hands' positions
            this.lastPosition1 = skeleton.Joints[JointType.HandLeft].Position;
            this.lastPosition2 = skeleton.Joints[JointType.HandRight].Position;
        }

        //Utility functions***********************

        /// <summary>
        /// Checks if the user's head can be tracked
        /// </summary>
        /// <param name="skeleton">skeleton for reference</param>
        private bool minDistance(Skeleton skeleton)
        {
            bool res = false;

            //Inefficient way, a more direct one not involving going through every joint is preferred
            if (skeleton.Joints[JointType.Head].TrackingState.Equals(JointTrackingState.Tracked))
                res = true;
            /*foreach (Joint joint in skeleton.Joints)
            {
                if (joint.JointType.Equals(JointType.Head) && joint.TrackingState.Equals(JointTrackingState.Tracked))
                    res = true;
            }*/
            return res;
        }
        /// <summary>
        /// Leads the user to a distance from which his or her arms can be seen on the sensor even while raised
        /// </summary>
        /// <param name="skeleton">skeleton for reference</param>
        private void setDistance(Skeleton skeleton)
        {
            //We will ask the person to back up until the head is being tracked
            this.instructionsText.Text = "Aléjate del sensor hasta que tu cabeza entre cómodamente en su rango";
        }
        /// <summary>
        /// Checks if the user's head can be tracked
        /// </summary>
        /// <param name="skeleton">skeleton for reference</param>
        private bool areHandsAboveHead(Skeleton skeleton)
        {
            return this.isRightHandAboveHead(skeleton) && this.isLeftHandAboveHead(skeleton);
        }
        private bool isLeftHandAboveHead(Skeleton skeleton)
        {
            //We proceed to check if the left hand is above head height minus the rror margin
            return (skeleton.Joints[JointType.HandLeft].Position.Y > (1 - this.errorMargin) * skeleton.Joints[JointType.Head].Position.Y);
        }
        private bool isRightHandAboveHead(Skeleton skeleton)
        {
            //We proceed to check if the left hand is above head height minus the rror margin
            return (skeleton.Joints[JointType.HandRight].Position.Y > (1 - this.errorMargin) * skeleton.Joints[JointType.Head].Position.Y);
        }

        //Utility functions***********************


        /// <summary>
        /// Leads the user through visual cues to a position. As the name states, the keys to this position are arms parallel to the ground and vertical forearms, around head height
        /// </summary>
        /// <param name="skeleton">skeleton for reference</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <returns>ture if the pose is achieved, false otherwise</returns>
        private bool pose_HoldHandsUp(Skeleton skeleton, DrawingContext dc)
        {
            // We give the instructions
            this.instructionsText.Text = "Mantén tus brazos paralelos al suelo y sube las manos";

            //Radius at wich the hands are considered to be in place
            float detectionRadius = 0.11f;

            // Horizontal elbow-head distance
            float horizontalDistanceToHead;

            // Array containing both hands
            Joint[] hands = new Joint[2];
            // Booleans containing wether the hands are in the right position
            bool isLeftInPosition=false, isRightInPosition=false;

            // Store each hand in the array
            hands[0] = skeleton.Joints[JointType.HandLeft];
            hands[1] = skeleton.Joints[JointType.HandRight];


            //Now we compute the position of the cues, wether the hands are in place, and draw the cues
            

            // For the distance to the head we will take the  average horizontal distance of the chest to each shoulder plus shoulder lenght
            //Now we have distance from head to shoulder (this one should be relatively stable, only changes by moving the scapulae)
            horizontalDistanceToHead = ((skeleton.Joints[JointType.ElbowLeft].Position.X - skeleton.Joints[JointType.ShoulderCenter].Position.X)
                                        +   (skeleton.Joints[JointType.ShoulderCenter].Position.X - skeleton.Joints[JointType.ElbowRight].Position.X)
                                        ) / 2;


            // We proceed to draw two cues at the desired position
            SkeletonPoint leftCuePosition, rightCuePosition;
            // Compute the position of the cues
            //Take the head as a reference
            leftCuePosition = skeleton.Joints[JointType.Head].Position;
            rightCuePosition = skeleton.Joints[JointType.Head].Position;

            //Move the cue left or right the desired distance
            leftCuePosition.X += horizontalDistanceToHead;
            rightCuePosition.X -= horizontalDistanceToHead;

            //If the hands are being tracked, we proceed to determine wether they are in the right place
            if (hands[0].TrackingState.Equals(JointTrackingState.Tracked))
            {
                // Check if left hand is in place
                if (
                    (Math.Abs(hands[0].Position.X - leftCuePosition.X) < detectionRadius)
                    &&
                    (Math.Abs(hands[0].Position.Y - leftCuePosition.Y) < detectionRadius)
                    )
                    isLeftInPosition = true;
                else
                    isLeftInPosition = false;
            }

            if (hands[1].TrackingState.Equals(JointTrackingState.Tracked))
            {
                // Check if left hand is in place
                if (
                    (Math.Abs(hands[1].Position.X - rightCuePosition.X) < detectionRadius)
                    &&
                    (Math.Abs(hands[1].Position.Y - rightCuePosition.Y) < detectionRadius)
                    )
                    isRightInPosition = true;
                else
                    isRightInPosition = false;
            }

            // We will now draw a visual cue for each hand's placement
            this.drawCues_HoldHandsUp(leftCuePosition, rightCuePosition, dc, isLeftInPosition, isRightInPosition);

            return isLeftInPosition && isRightInPosition;
        }

        /// <summary>
        /// Draws the cues for the position
        /// </summary>
        /// <param name="leftCuePosition">position of left cue</param>
        /// <param name="rightCuePosition">position of right cue</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="left">wether left hand is in place</param>
        /// <param name="right">wether right hand is in place</param>
        private void drawCues_HoldHandsUp(SkeletonPoint leftCuePosition, SkeletonPoint rightCuePosition, DrawingContext dc, bool left, bool right)
        {
            Brush drawBrush;

            //Left cue
            if (left)
                drawBrush = this.CueAchieved;
            else
                drawBrush = this.CueNotAchieved;

            dc.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(leftCuePosition), CueThickness, CueThickness);

            //Right Cue
            if (right)
                drawBrush = this.CueAchieved;
            else
                drawBrush = this.CueNotAchieved;

            dc.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(rightCuePosition), CueThickness, CueThickness);
        }

        /// <summary>
        /// Leads the user to perform the gesture: raise hands, through a length around his head to chest distance
        /// </summary>
        /// <param name="skeleton">skeleton for reference</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <returns>ture if the pose is achieved, false otherwise</returns>
        private bool gesture_RaiseHands(Skeleton skeleton, DrawingContext dc)
        {
            //Booleans to mark the completion status of each hand separately
            bool left=false,right=false;

            //The target distance to move the hands is the head to chest distance, minus an error margin factor
            float targetDistance = skeleton.Joints[JointType.Head].Position.Y - skeleton.Joints[JointType.ShoulderRight].Position.Y;

            //Correct for the error margin
            targetDistance *= (1 - errorMargin);

            //This 1.6 distance factor for the distance, is not too taxative, not to easy. A good "normal" difficulty value
            //Varying this will increase/decrease dificulty respectively
            targetDistance *= this.difficultyFactor;

            //We track the distance between the former and current positions of each hand
            travelledDistance1 += skeleton.Joints[JointType.HandLeft].Position.Y - this.lastPosition1.Y;
            travelledDistance2 += skeleton.Joints[JointType.HandRight].Position.Y - this.lastPosition2.Y;

            left = this.travelledDistance1 > targetDistance;
            right = this.travelledDistance2 > targetDistance;

            //We proceed to draw two cues in each hand, in the form of two upward arrows, red when the movement is yet uncomplete, and green otherwise
            this.drawCues_RaiseHands(skeleton, dc, left, right);

            //Return the conjunction of both movements (completion status of the whole gesture)
            return left && right;
        }

        /// <summary>
        /// Draws the cues for the position
        /// </summary>
        /// <param name="leftCuePosition">position of left cue</param>
        /// <param name="rightCuePosition">position of right cue</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="left">wether left hand is in place</param>
        /// <param name="right">wether right hand is in place</param>
        private void drawCues_RaiseHands(Skeleton skeleton, DrawingContext dc, bool left, bool right)
        {
            Pen pen;
            SkeletonPoint init, end;


            //Left cue
            if (left)
                pen = this.MotionAchieved;
            else
                pen = this.MotionNotAchieved;

            //Compute init/end points
            init = skeleton.Joints[JointType.HandLeft].Position;
            end = init;
            end.Y = ((skeleton.Joints[JointType.Head].Position.Y * (1 + this.difficultyFactor)) - (skeleton.Joints[JointType.ShoulderRight].Position.Y * this.difficultyFactor));

            dc.DrawLine(pen, SkeletonPointToScreen(init), SkeletonPointToScreen(end));

            //Right Cue
            if (right)
                pen = this.MotionAchieved;
            else
                pen = this.MotionNotAchieved;

            //Compute init/end points
            init = skeleton.Joints[JointType.HandRight].Position;
            end = init;
            end.Y = ((skeleton.Joints[JointType.Head].Position.Y * (1+this.difficultyFactor)) - (skeleton.Joints[JointType.ShoulderRight].Position.Y * this.difficultyFactor));

            dc.DrawLine(pen, SkeletonPointToScreen(init), SkeletonPointToScreen(end));
        }



        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }
    }
}
