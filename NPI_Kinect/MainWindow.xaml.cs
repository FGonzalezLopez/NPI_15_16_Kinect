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
        private float difficultyFactor;

        /// <summary>
        /// Error margin (%)
        /// </summary>
        private float errorMargin;


        //Global program control variables *******************************************************************

        /// <summary>
        /// Variables to track a max of 2 movements' length
        /// </summary>
        private float travelledDistance1 = 0.0f, travelledDistance2 = 0.0f;

        /// <summary>
        /// Variable to track the menu status (0 meaning show menu, 1,2,3... meaning each option)
        /// </summary>
        private int menuNumber = 0;

        /// <summary>
        /// Variable to track the menu current selection ( doesn't actually choose anything, it is only made effective once the "up arrow" is selected)
        /// </summary>
        private int menuSelection = 1;

        /// <summary>
        /// Size of the circular queue for the options menu
        /// </summary>
        private int menuOptionsCount = 2;

        /// <summary>
        /// Variables to hold a max of 2 positions through calls
        /// </summary>
        private SkeletonPoint lastPosition1, lastPosition2;

        /// <summary>
        /// The size of the different icons' boxes
        /// </summary>
        private float iconBoxSize = 100;

        /// <summary>
        /// Tracks wether the user is positioned correctly
        /// </summary>
        private bool isPositionedCorrectly = false;

        // Variables to track the state of each individual routine

        /// <summary>
        /// Tracks
        /// </summary>
        private bool repetitionCompleted = false;

        /// <summary>
        /// Repetition count for the exercise
        /// </summary>
        private int repetitionCount = 0;

        /// <summary>
        /// Repetition target count for the exercises
        /// </summary>
        private int targetRepetitions = 10;

        /// <summary>
        /// Timer for various time counts
        /// </summary>
        private System.Windows.Forms.Timer timer1;

        /// <summary>
        /// Timer for home button counter (must be sepparate)
        /// </summary>
        private System.Windows.Forms.Timer timerHome;

        /// <summary>
        /// Indicates towards which option is the timer counting, 1 left 2 right 3 up
        /// </summary>
        private int timerTarget=0;

        /// <summary>
        /// Indicates wether a countdown has finished. Must be set to false before calling timer.start()
        /// </summary>
        private bool countDownFinished = false;

        /// <summary>
        /// Indicates the time the user has to maintain a position for an action to occur (3s)
        /// </summary>
        private int waitingTime = 0;
        
        //******************************************************************************************************

        /// <summary>
        /// Imagen of a leftward arrow
        /// </summary>
        private ImageSource arrowLeftward;

        /// <summary>
        /// Imagen of a rightward arrow
        /// </summary>
        private ImageSource arrowRightward;

        /// <summary>
        /// Imagen of a upward arrow
        /// </summary>
        private ImageSource arrowUpward;

        /// <summary>
        /// Imagen of home icon
        /// </summary>
        private ImageSource homeIcon;

        /// <summary>
        /// Imagen of exer. 1 icon
        /// </summary>
        private ImageSource ej1Icon;

        /// <summary>
        /// Imagen of exer. 2 icon
        /// </summary>
        private ImageSource ej2Icon;



        /// <summary>
        /// Brush used for drawing visual cues to indicate a wait
        /// </summary>
        private readonly Brush waitingBrush = new SolidColorBrush(Color.FromRgb(0, 0, 255));

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

            //Initialize the images
            this.initializeResources();

            // Initialize the timer
            this.waitingTime = 3000;
            this.timer1 = new System.Windows.Forms.Timer();
            this.timer1.Tick += new EventHandler(timer_Tick);
            this.timer1.Interval = this.waitingTime;

            this.timerHome = new System.Windows.Forms.Timer();
            this.timerHome.Tick += new EventHandler(home_Tick);
            this.timerHome.Interval = this.waitingTime;




            //Set the difficulty and error margins to their defaults upon opening the program
            this.errorMargin = 0.2f;
            this.difficultyFactor = 1.6f;
            this.targetRepetitions = 5;
            this.inputBoxError.Text = this.errorMargin.ToString();
            this.inputBoxDifficulty.Text = this.difficultyFactor.ToString();
            this.inputBoxMaxReps.Text = this.targetRepetitions.ToString();
            this.menuNumber = 0;
            // Update error margin and difficulty to their defaults
            this.updateParameters();

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
                this.sensor.SkeletonStream.Enable(smoothingParam);

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
        /// Initializes image brushes (arrows)
        /// </summary>
        private void initializeResources()
        {
            this.arrowLeftward = new BitmapImage(new Uri(@"..\..\resources\arrow_left.png", UriKind.Relative));
            this.arrowRightward =new BitmapImage(new Uri(@"..\..\resources\arrow_right.png", UriKind.Relative));
            this.arrowUpward = new BitmapImage(new Uri(@"..\..\resources\arrow_up.png", UriKind.Relative));
            this.homeIcon = new BitmapImage(new Uri(@"..\..\resources\home.png", UriKind.Relative));
            this.ej1Icon = new BitmapImage(new Uri(@"..\..\resources\ej1.png", UriKind.Relative));
            this.ej2Icon = new BitmapImage(new Uri(@"..\..\resources\ej2.png", UriKind.Relative));
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
                            //Ahora vamos a llamar a la función de menú principal, enviándo el esqueleto y el draw. context
                            this.menuRoutine(skel, dc);
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
        /// Menu to choose exercise
        /// </summary>
        /// <param name="skeleton">skeleton for reference</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void menuRoutine(Skeleton skeleton, DrawingContext drawingContext)
        {
            // First we check if the user wants to go back to the menu (home) from an exercise
            this.homeButton(skeleton, drawingContext);

            // We assume that the skeleton is being correctly tracked, as this is called from within that condition
            // We will now check the person's position and make the required corrections
            if (!minDistance(skeleton))
                this.setDistance(skeleton);
            //If either hand goes untracked, we restart and go back to the menu
            else if( skeleton.Joints[JointType.HandLeft].TrackingState.Equals(JointTrackingState.NotTracked) 
                    ||
                     skeleton.Joints[JointType.HandRight].TrackingState.Equals(JointTrackingState.NotTracked) )
            {
                this.menuNumber = 0;
                this.pushPromptToReposition();
            }
            // If the menu number is 0, we show the menu
            else if (this.menuNumber == 0)
            {
                this.popPromtToReposition();
                this.repetitionCount = 0;
                this.updateRepetitionCount();
                this.optionMenu(skeleton, drawingContext);
            }
            // If the menu number is not 0, we close the menu and call the adecuate routine
            else
            {
                switch (this.menuNumber)
                {
                    case 1:
                        this.movementRoutine_1(skeleton, drawingContext);
                        break;
                    case 2:
                        this.movementRoutine_2(skeleton, drawingContext);
                        break;
                    //... rest of routines

                    //Should any problem appear, go back to the menu
                    default:
                        this.menuNumber = 0;
                        break;
                }
            }
            

        }

        

        /// <summary>
        /// Draws and detects the choices of the menu
        /// </summary>
        private void optionMenu(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Show the menu visuals
            //...
            SkeletonPoint rightArrowPosition, leftArrowPosition, upArrowPosition;
            Point leftArrowPoint, rightArrowPoint, upArrowPoint;

            // For the upwards arrow we take a base height of head, and then add the head to chest height
            upArrowPosition = skeleton.Joints[JointType.Head].Position;
            upArrowPosition.Y += (skeleton.Joints[JointType.Head].Position.Y - skeleton.Joints[JointType.ShoulderCenter].Position.Y)*2;

            // We take a base height halfway between hip and head for the left/right arrows
            rightArrowPosition = skeleton.Joints[JointType.HipCenter].Position;
            rightArrowPosition.Y += (skeleton.Joints[JointType.Head].Position.Y - skeleton.Joints[JointType.HipCenter].Position.Y) / 2;

            leftArrowPosition = rightArrowPosition;

            // Then move the x component of the position a distance around the spine's lenght (hip to head distance)
            float arrowLateralOffset;
            arrowLateralOffset = skeleton.Joints[JointType.Head].Position.Y - skeleton.Joints[JointType.HipCenter].Position.Y;

            rightArrowPosition.X -= arrowLateralOffset;
            leftArrowPosition.X += arrowLateralOffset;

            leftArrowPoint = this.SkeletonPointToScreen(rightArrowPosition);
            rightArrowPoint = this.SkeletonPointToScreen(leftArrowPosition);
            upArrowPoint = this.SkeletonPointToScreen(upArrowPosition);

            // We compensate for the fact that the arrows are painted using the point as the upper left corner
            leftArrowPoint.X -= this.iconBoxSize / 2;
            leftArrowPoint.Y -= this.iconBoxSize / 2;

            rightArrowPoint.X -= this.iconBoxSize / 2;
            rightArrowPoint.Y -= this.iconBoxSize / 2;

            upArrowPoint.X -= this.iconBoxSize / 2;
            upArrowPoint.Y -= this.iconBoxSize / 2;


            drawingContext.DrawImage(this.arrowRightward, new Rect( rightArrowPoint, new Size(this.iconBoxSize, this.iconBoxSize)));
            drawingContext.DrawImage(this.arrowLeftward, new Rect( leftArrowPoint, new Size(this.iconBoxSize,this.iconBoxSize)));
            drawingContext.DrawImage(this.arrowUpward, new Rect( upArrowPoint, new Size(this.iconBoxSize, this.iconBoxSize)));

            // Then we pass the skeleton, dc, and positions of the arrows (their centers) to the routine that detects the choice and draws a cue
            // Before that, we undo the corrections done for the boxes
            leftArrowPoint.X += this.iconBoxSize / 2;
            leftArrowPoint.Y += this.iconBoxSize / 2;

            rightArrowPoint.X += this.iconBoxSize / 2;
            rightArrowPoint.Y += this.iconBoxSize / 2;

            upArrowPoint.X += this.iconBoxSize / 2;
            upArrowPoint.Y += this.iconBoxSize / 2;

            int action = detectSelectionMenu(skeleton, drawingContext, leftArrowPoint, rightArrowPoint, upArrowPoint);

            // The user chose left
            if(action == 1)
            {
                this.menuSelection--;
            }
            // The user chose right
            else if (action == 2)
            {
                this.menuSelection++;
            }
            // The user chose the current exercise
            else if (action == 3)
            {
                this.menuNumber=this.menuSelection;
            }
        

            // Show the image for the exercise
            ImageSource exerciseImage = this.homeIcon;


            switch (menuSelection)
            {
                case 1:
                    exerciseImage = this.ej1Icon;
                    break;
                case 2:
                    exerciseImage = this.ej2Icon;
                    break;
            }

            this.drawExerciseImage(skeleton, drawingContext,exerciseImage);


            //If the option goes below 1, it is set to the last option, if goes above the number of options, it is set to 1
            if (this.menuSelection < 1)
                this.menuSelection = this.menuOptionsCount;
            else if (this.menuSelection > menuOptionsCount)
                this.menuSelection = 1;

            this.instructionsText.Text = "Opción del menú: " + this.menuSelection.ToString() ;
           
        }

        private void drawExerciseImage(Skeleton skeleton, DrawingContext drawingcontext, ImageSource image)
        {
            SkeletonPoint iconPosition;
            // We take the center hip as base point
            iconPosition = skeleton.Joints[JointType.HipCenter].Position;

            iconPosition.Y -= (skeleton.Joints[JointType.Head].Position.Y - skeleton.Joints[JointType.HipCenter].Position.Y)/4;

            Point drawingPoint = this.SkeletonPointToScreen(iconPosition);

            // We compensate for the fact that the arrows are painted using the point as the upper left corner
            drawingPoint.X -= this.iconBoxSize / 2;
            drawingPoint.Y -= this.iconBoxSize / 2;

            // We finally draw the icon in the desired position
            drawingcontext.DrawImage(image, new Rect(drawingPoint, new Size(this.iconBoxSize, this.iconBoxSize)));
        }


        /// <summary>
        /// Detects the motion of the user and translates it to a menu action, being 1 left, 2 right, 3 up (choose current)
        /// </summary>
        private int detectSelectionMenu(Skeleton skeleton, DrawingContext drawingContext, Point leftArrowPoint, Point rightArrowPoint, Point upArrowPoint)
        {
            int res = 0;
            Brush upBrush, leftBrush, rightBrush;

            // The three brushes default to red
            upBrush = this.CueNotAchieved;
            leftBrush = upBrush;
            rightBrush = upBrush;

            // We store the hands position for easier access
            Point lHand, rHand;

            lHand = this.SkeletonPointToScreen(skeleton.Joints[JointType.HandLeft].Position);
            rHand = this.SkeletonPointToScreen(skeleton.Joints[JointType.HandRight].Position);


            // It won't support simultaneous multi-choice, only the first option will be selected after a 2-3 seconds countdown marked by a red-orange-green color code via circle in the arrow
            // The upper choice zone will be wider than the others, as the sensor experiences problems when the joints are directly over the head.
            // We start by detecting a hand in the upper region
            if (
                    (Math.Abs(lHand.X - upArrowPoint.X) < (CueThickness * 10) * (1 + this.errorMargin)
                    &&
                    Math.Abs(lHand.Y - upArrowPoint.Y) < (CueThickness * 2) * (1 + this.errorMargin))
                ||
                    (Math.Abs(rHand.X - upArrowPoint.X) < (CueThickness * 10) * (1 + this.errorMargin)
                    &&
                    Math.Abs(rHand.Y - upArrowPoint.Y) < (CueThickness * 2) * (1 + this.errorMargin))
                )
            {
                // Change the timer count target to option 3 (up)
                // We could put this condition in the outer if, for clarity I will separate it
                if (this.timerTarget == 0)
                    this.timerTarget = 3;
            }
            // If the target is 3, but the user has stopped the gesture, stop the timer
            else if (timerTarget == 3)
            {
                this.timer1.Stop();
                this.timerTarget = 0;
            }

            if (
                    (Math.Abs(lHand.X - leftArrowPoint.X) < (CueThickness * 4) * (1 + this.errorMargin)
                    &&
                    Math.Abs(lHand.Y - leftArrowPoint.Y) < (CueThickness * 4) * (1 + this.errorMargin))
                ||
                    (Math.Abs(rHand.X - leftArrowPoint.X) < (CueThickness * 4) * (1 + this.errorMargin)
                    &&
                    Math.Abs(rHand.Y - leftArrowPoint.Y) > (CueThickness * 4) * (1 + this.errorMargin))
                )
            {
                // Change the timer count target to option 1 (left)
                // We could put this condition in the outer if, for clarity I will separate it
                if (this.timerTarget == 0)
                    this.timerTarget = 1;
            }
            // If the target is 1, but the user has stopped the gesture, stop the timer
            else if (timerTarget == 1)
            {
                this.timer1.Stop();
                this.timerTarget = 0;
            }


            if (
                    (Math.Abs(lHand.X - rightArrowPoint.X) < (CueThickness * 4) * (1 + this.errorMargin)
                    &&
                    Math.Abs(lHand.Y - rightArrowPoint.Y) < (CueThickness * 4) * (1 + this.errorMargin))
                ||
                    (Math.Abs(rHand.X - rightArrowPoint.X) < (CueThickness * 4) * (1 + this.errorMargin)
                    &&
                    Math.Abs(rHand.Y - rightArrowPoint.Y) < (CueThickness * 4) * (1 + this.errorMargin))
                )
            {
                // Change the timer count target to option 2 (right)
                // We could put this condition in the outer if, for clarity I will separate it
                if (this.timerTarget == 0)
                    this.timerTarget = 2;
            }
            // If the target is 2, but the user has stopped the gesture, stop the timer and reset the target
            else if (timerTarget == 2)
            {
                this.timer1.Stop();
                this.timerTarget = 0;
            }

            // Once we have set the target, we count 3 seconds, then change the result to the target, set the target to 0 and stop the timer 
            // If the timer is not started, and the target is non-zero, we start the timer
            if (this.timer1.Enabled == false && this.timerTarget != 0)
            {
                this.startCountdown();
            }
            else if (this.countDownFinished)
            {
                // When the timer counts the 3 seconds, we set the result to the target (the timer stops itself)
                res = this.timerTarget;
                this.timerTarget = 0;
                this.timer1.Stop();
                this.countDownFinished = false;
            }

            // Now we proceed to change the color of the cues to blue for the target while waiting and green to the result when the countdown ends
            switch (res)
            {
                //The countdown has finished
                case 1:
                    leftBrush = this.CueAchieved;
                    break;
                case 2:
                    rightBrush = this.CueAchieved;
                    break;
                case 3:
                    upBrush = this.CueAchieved;
                    break;

                // The countdown has not finished
                case 0:
                    switch (this.timerTarget)
                    {
                        case 1:
                            leftBrush = this.waitingBrush;
                            break;
                        case 2:
                            rightBrush = this.waitingBrush;
                            break;
                        case 3:
                            upBrush = this.waitingBrush;
                            break;
                    }
                    break;
            }
            

            //Once the detection is done, we draw the corresponding cues
            drawingContext.DrawEllipse(upBrush, null, upArrowPoint, CueThickness, CueThickness);
            drawingContext.DrawEllipse(leftBrush, null, leftArrowPoint, CueThickness, CueThickness);
            drawingContext.DrawEllipse(rightBrush, null, rightArrowPoint, CueThickness, CueThickness);


            return res;
        }

        /// <summary>
        /// Go back to the menu 
        /// </summary>
        private void homeButton(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Detect and show the home button
            //...
            SkeletonPoint homePosition;
            Point homePoint;
            

            // For the upwards arrow we take a base height of head, and then add the head to chest height
            homePosition = skeleton.Joints[JointType.Head].Position;

            // Then move the icon to where we want it
            // First, we translate it upwards the "neck" length (head to chest distance)
            homePosition.Y += skeleton.Joints[JointType.Head].Position.Y - skeleton.Joints[JointType.ShoulderCenter].Position.Y;
            //Then, move it to the left two torsos (chest to hip distance)
            homePosition.X -= (skeleton.Joints[JointType.ShoulderCenter].Position.Y - skeleton.Joints[JointType.HipCenter].Position.Y)*2;

            homePoint = this.SkeletonPointToScreen(homePosition);

            // We compensate for the fact that the arrows are painted using the point as the upper left corner
            homePoint.X -= this.iconBoxSize / 2;
            homePoint.Y -= this.iconBoxSize / 2;

            drawingContext.DrawImage(this.homeIcon, new Rect(homePoint, new Size(this.iconBoxSize, this.iconBoxSize)));

            // Then we pass the skeleton, dc, and positions of the icon (its center) to the routine that detects the choice and draws a cue
            // Before that, we undo the corrections done for the boxes
            homePoint.X += this.iconBoxSize / 2;
            homePoint.Y += this.iconBoxSize / 2;

            // Finally, we check if the user has pressed the button for 3 seconds and go back to the menu
            if (detectHomePressed(skeleton, drawingContext, homePoint))
            {
                this.menuNumber = 0;
                // Redundant?
                this.timer1.Stop();
            }
                
        }

        /// <summary>
        /// Draws a visual cue for the home button and detects wether it has been pressed or not
        /// </summary>
        private bool detectHomePressed(Skeleton skeleton, DrawingContext drawingContext, Point homePoint)
        {
            // Flag to control if the home button is being pressed
            bool homePressed = false;
            // Flag to control the result of the call (go home or not)
            bool goHome = false;
            // Brush to draw with
            Brush homeBrush = this.CueNotAchieved;

            // Each hand's positions
            Point lHand, rHand;

            lHand = this.SkeletonPointToScreen(skeleton.Joints[JointType.HandLeft].Position);
            rHand = this.SkeletonPointToScreen(skeleton.Joints[JointType.HandRight].Position);


            //First we check if either hand is on the button
            //Left hand
            homePressed = homePressed ||
                    (Math.Abs(lHand.X - homePoint.X) < (CueThickness * 4) * (1 + this.errorMargin)
                    &&
                    Math.Abs(lHand.Y - homePoint.Y) < (CueThickness * 4) * (1 + this.errorMargin));
            //Right hand
            homePressed = homePressed ||
                    (Math.Abs(rHand.X - homePoint.X) < (CueThickness * 4) * (1 + this.errorMargin)
                    &&
                    Math.Abs(rHand.Y - homePoint.Y) < (CueThickness * 4) * (1 + this.errorMargin));

            
            this.timerHome.Enabled = homePressed;

            // Decide which brush should we use based on the button status (if none of this conditions check out, the brush stays red)
            if(homePressed)
                homeBrush = waitingBrush;


            //Once the detection is done, we draw the corresponding cues
            drawingContext.DrawEllipse(homeBrush, null, homePoint, CueThickness, CueThickness);

            return goHome;
        }

        /// <summary>
        /// Method to start a countdown
        /// </summary>
        private void startCountdown()
        {
            //Set the countdown state to not finished and start the timer
            if(this.timer1.Enabled == false)
            {
                this.countDownFinished = false;
                this.timer1.Start();
            }
        }

        /// <summary>
        /// Callback for the 3 seconds of the timer
        /// </summary>
        private void timer_Tick(object sender, EventArgs e)
        {
            // Set the finalization flag to true
            this.countDownFinished = true;
        }

        /// <summary>
        /// Callback for the 3 seconds of the home timer, sets the option menu directly to 0 (menu)
        /// </summary>
        private void home_Tick(object sender, EventArgs e)
        {
            this.menuNumber = 0;
            this.timerHome.Stop();
        }



        /// <summary>
        /// Do the initial setup tasks for a routine's tracking
        /// </summary>
        private void resetParameters()
        {

            //Reset the tracking parameters
            this.poseAchieved = false;
            this.repetitionCompleted = false;

            this.travelledDistance1 = 0;
            this.travelledDistance2 = 0;
        }



        /// <summary>
        /// Pose+gesture movement routine
        /// </summary>
        /// <param name="skeleton">skeleton for reference</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void movementRoutine_1(Skeleton skeleton, DrawingContext drawingContext)
        {
            // While the initial pose is not achieved, we check for it
            if (!this.poseAchieved)
            {
                this.poseAchieved = this.pose_HoldHandsUp(skeleton, drawingContext);
                //Set the gesture completion to false
                this.repetitionCompleted = false;
            }
            //Then, while the user doesn't break gesture, which we will consider to be lowering the hands below head joint height
            else if (this.areHandsAboveHead(skeleton) && !this.repetitionCompleted)
            {
                //Once the user achieves the position, we will ask the user to make a gesture
                //It will involve raising his hands roughly twice the distance between his head and chest joints
                this.repetitionCompleted = this.gesture_RaiseHands(skeleton, drawingContext);
            }
            // If the user finishes the gesture, the cycle resets, again to the static position
            else if (this.repetitionCompleted && (repetitionCount < this.targetRepetitions))
            {
                this.repetitionCount++;
                this.updateRepetitionCount();

                //Reset the routine control parameters
                this.resetParameters();

                // Once the target reps. are achieved, go back to the menu
                if (this.repetitionCount == this.targetRepetitions)
                    this.menuNumber = 0;
            }
            // The user has broken gesture, we reset the pose and gesture completion parameters
            else
            {
                this.poseAchieved = false;
                travelledDistance1 = 0.0f;
                travelledDistance2 = 0.0f;
                this.repetitionCompleted = false;
            }

            //Update the hands' last positions
            this.lastPosition1 = skeleton.Joints[JointType.HandLeft].Position;
            this.lastPosition2 = skeleton.Joints[JointType.HandRight].Position;
        }

        /// <summary>
        /// Maintained gesture exercise
        /// </summary>
        /// <param name="skeleton">skeleton for reference</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void movementRoutine_2(Skeleton skeleton, DrawingContext drawingContext)
        {
            // We start from a arms relaxed, along the body position
            
            if(!this.poseAchieved)
            {
                this.poseAchieved = this.armsRelaxed(skeleton);
            }
            else if (this.poseAchieved && !this.repetitionCompleted)
            {
                // Stop the count if the user is not in the required pose
                if (!this.pose_holdHandsExtended(skeleton, drawingContext))
                    this.timer1.Stop();
                // If the user is in the required pose, count three seconds
                else
                {
                    //Start the comuntdown
                    this.startCountdown();
                    // If its finished
                    if (this.countDownFinished)
                    {
                        //Set the repetition as finished
                        this.repetitionCompleted = true;
                        this.timer1.Stop();
                        this.countDownFinished = false;
                    }
                }
            }
            // If the user finishes the gesture, the cycle resets, and the user is asked to lower his/her arms
            else if (this.repetitionCompleted && (this.repetitionCount < this.targetRepetitions))
            {
                this.repetitionCount++;
                this.updateRepetitionCount();

                //Reset the routine control parameters
                this.resetParameters();

                // Once the target reps. are achieved, go back to the menu
                if (this.repetitionCount == this.targetRepetitions)
                    this.menuNumber = 0;
            }
            // The user has broken gesture, we reset the pose and gesture completion parameters
            else
            {
                this.poseAchieved = false;
                travelledDistance1 = 0.0f;
                travelledDistance2 = 0.0f;
                this.repetitionCompleted = false;
            }

            //Update the hands' last positions
            this.lastPosition1 = skeleton.Joints[JointType.HandLeft].Position;
            this.lastPosition2 = skeleton.Joints[JointType.HandRight].Position;
        }

        //Utility functions***********************



        /// <summary>
        /// Updates the repetition count's box text
        /// </summary>
        private void updateRepetitionCount()
        {
            this.CurrentRepsText.Text = "Actuales: " + this.repetitionCount.ToString();
        }

        /// <summary>
        /// Checks if the user's hands can be tracked above his or her head
        /// </summary>
        /// <param name="skeleton">skeleton for reference</param>
        private bool minDistance(Skeleton skeleton)
        {
            bool positionCorrect = false;

            // Logic changed, now is true if either hand is untracked to make the app less jittery
            positionCorrect = positionCorrect || skeleton.Joints[JointType.HandLeft].TrackingState.Equals(JointTrackingState.NotTracked);
            positionCorrect = positionCorrect || skeleton.Joints[JointType.HandRight].TrackingState.Equals(JointTrackingState.NotTracked);

            //If any of the hands is not being tracked, we reset everything and ask the user to reposition again, and go back to the menu
            if (positionCorrect)
            {
                this.isPositionedCorrectly = false;
                this.menuNumber = 0;
            }

            // Invert the value to be congruent with the former logic
            positionCorrect = !positionCorrect;

            // Check if the hands are above the head and the user was not properly positioned, he will be asked to position correctly
            if(positionCorrect && !this.isPositionedCorrectly)
                positionCorrect= positionCorrect && this.areHandsAboveHead(skeleton);

            return this.isPositionedCorrectly=positionCorrect;
        }
        /// <summary>
        /// Leads the user to a distance from which his or her arms can be seen on the sensor even while raised
        /// </summary>
        /// <param name="skeleton">skeleton for reference</param>
        private void setDistance(Skeleton skeleton)
        {
            //We will ask the person to back up until the position is correct
            this.pushPromptToReposition();
        }
        /// <summary>
        /// Prompts the user to position properly
        /// </summary>
        private void pushPromptToReposition()
        {
            
            //this.outOfPlaceWarn.Text = "Estás fuera de la zona de actividad.\nPor favor, aléjate con las manos encima de\nla cabeza hasta que quepan cómodamente\nen la pantalla para ser reconocido";
            this.outOfPlace_bar.Background = new SolidColorBrush(Colors.Red);
            this.instructionsText.Text = "";

            this.outOfPlace_bar.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Hides the reposition prompt
        /// </summary>
        private void popPromtToReposition()
        {
            this.outOfPlace_bar.Visibility = Visibility.Collapsed;
            //this.outOfPlaceWarn.Text = "";
            this.outOfPlace_bar.Background = new SolidColorBrush(Colors.Transparent);
        }

        /// <summary>
        /// Checks if the user's hands are above his head
        /// </summary>
        /// <param name="skeleton">skeleton for reference</param>
        private bool areHandsAboveHead(Skeleton skeleton)
        {
            return this.isRightHandAboveHead(skeleton) && this.isLeftHandAboveHead(skeleton);
        }

        /// <summary>
        /// Checks if the user's left hand is above his head
        /// </summary>
        private bool isLeftHandAboveHead(Skeleton skeleton)
        {
            //We proceed to check if the left hand is above head height minus the rror margin
            return (skeleton.Joints[JointType.HandLeft].Position.Y > (1 - this.errorMargin) * skeleton.Joints[JointType.Head].Position.Y);
        }

        /// <summary>
        /// Checks if the user's right hand is above his head
        /// </summary>
        private bool isRightHandAboveHead(Skeleton skeleton)
        {
            //We proceed to check if the left hand is above head height minus the rror margin
            return (skeleton.Joints[JointType.HandRight].Position.Y > (1 - this.errorMargin) * skeleton.Joints[JointType.Head].Position.Y);
        }

        /// <summary>
        /// Checks if the user's arms are relaxed along the body (which we will consider when his hands are under hip height
        /// </summary>
        private bool armsRelaxed(Skeleton skeleton)
        {
            bool res=true;

            // We take a threshold to pass as the hips average height
            float threshold;

            // We give the instructions
            this.instructionsText.Text = "Relaja tus brazos a lo largo del cuerpo";
    
            threshold = (skeleton.Joints[JointType.HipLeft].Position.Y + skeleton.Joints[JointType.HipRight].Position.Y) / 2;

            // This won't take into account the error margin, as the position is very easy to achieve due to the arms being longer than the torso
            res = res && (skeleton.Joints[JointType.HandLeft].Position.Y < threshold);
            res = res && (skeleton.Joints[JointType.HandRight].Position.Y < threshold);

            return res;
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
            this.instructionsText.Text = "Mantén tus brazos paralelos al suelo\n y sube las manos hasta las marcas";

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

        

        private bool pose_holdHandsExtended(Skeleton skeleton, DrawingContext dc)
        {
            // We give the instructions
            this.instructionsText.Text = "Extiende tus brazos paralelos al suelo.";

            //Radius at wich the hands are considered to be in place
            float detectionRadius = 0.09f;

            // Vertical distance hip-head
            float verticalDistanceHipChest;

            // Array containing both hands
            Joint[] hands = new Joint[2];
            // Booleans containing wether the hands are in the right position
            bool isLeftInPosition = false, isRightInPosition = false;

            // Store each hand in the array
            hands[0] = skeleton.Joints[JointType.HandLeft];
            hands[1] = skeleton.Joints[JointType.HandRight];


            //Now we compute the position of the cues, wether the hands are in place, and draw the cues


            // For the distance to the head we will take the twice the average (being 2 values, the combined lengths) horizontal distance of the chest to each shoulder plus shoulder lenght
            //Now we have distance from head to shoulder (this one should be relatively stable, only changes by moving the scapulae)
            verticalDistanceHipChest = ((skeleton.Joints[JointType.ShoulderCenter].Position.Y - skeleton.Joints[JointType.HipCenter].Position.Y))*1.75f;


            // We proceed to draw two cues at the desired position
            SkeletonPoint leftCuePosition, rightCuePosition;
            // Compute the position of the cues
            //Take the head as a reference
            leftCuePosition = skeleton.Joints[JointType.ShoulderCenter].Position;
            rightCuePosition = leftCuePosition;

            //Move the cue left or right the desired distance
            leftCuePosition.X -= verticalDistanceHipChest;
            rightCuePosition.X += verticalDistanceHipChest;

            //If the hands are being tracked, we proceed to determine wether they are in the right place
            if (hands[0].TrackingState.Equals(JointTrackingState.Tracked))
            {
                // Check if left hand is in place
                if (
                    (Math.Abs(hands[0].Position.X - leftCuePosition.X) < detectionRadius*(1+errorMargin))
                    &&
                    (Math.Abs(hands[0].Position.Y - leftCuePosition.Y) < detectionRadius*(1 + errorMargin))
                    )
                    isLeftInPosition = true;
                else
                    isLeftInPosition = false;
            }

            if (hands[1].TrackingState.Equals(JointTrackingState.Tracked))
            {
                // Check if left hand is in place
                if (
                    (Math.Abs(hands[1].Position.X - rightCuePosition.X) < detectionRadius * (1 + errorMargin))
                    &&
                    (Math.Abs(hands[1].Position.Y - rightCuePosition.Y) < detectionRadius * (1 + errorMargin))
                    )
                    isRightInPosition = true;
                else
                    isRightInPosition = false;
            }

            // We will now draw a visual cue for each hand's placement
            // The ideal method would take an array of cues, and array of colors, and the drawingcontext in which they are to be drawn 
            this.drawCues_holdHandsExtended(leftCuePosition, rightCuePosition, dc, isLeftInPosition, isRightInPosition);

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
        private void drawCues_holdHandsExtended(SkeletonPoint leftCuePosition, SkeletonPoint rightCuePosition, DrawingContext dc, bool left, bool right)
        {
            Brush drawBrush;

            //Left cue
            if (left)
                drawBrush = this.waitingBrush;
            else
                drawBrush = this.CueNotAchieved;

            dc.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(leftCuePosition), CueThickness, CueThickness);

            //Right Cue
            if (right)
                drawBrush = this.waitingBrush;
            else
                drawBrush = this.CueNotAchieved;

            dc.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(rightCuePosition), CueThickness, CueThickness);
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
            // Ask the user to make the movement
            this.instructionsText.Text = "Ahora, lleva tus manos hacia arriba";
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

        /// <summary>
        /// Updates the difficulty setting, the error margin and the target rep. counter
        /// </summary>
        public void updateParameters()
        {
            if (!String.IsNullOrEmpty(this.inputBoxError.Text))
            {
                this.errorMargin = float.Parse(this.inputBoxError.Text);

                //The error margin will be limited between 0.1 and 0.9
                if (this.errorMargin > 0.9)
                    this.errorMargin = 0.9f;
                else if (this.errorMargin < 0.1)
                    this.errorMargin = 0.1f;

                this.inputBoxError.Text = this.errorMargin.ToString();
            }

            if (!String.IsNullOrEmpty(this.inputBoxDifficulty.Text))
            {
                this.difficultyFactor = float.Parse(this.inputBoxDifficulty.Text);

                //The difficulty factor will be limited between 1 and 2
                if (this.difficultyFactor > 2)
                    this.difficultyFactor = 2f;
                else if (this.difficultyFactor < 1)
                    this.difficultyFactor = 1f;

                this.inputBoxDifficulty.Text = this.difficultyFactor.ToString();
            }

            if (!String.IsNullOrEmpty(this.inputBoxMaxReps.Text))
            {
                this.targetRepetitions = int.Parse(this.inputBoxMaxReps.Text);

                //The max reps will be between 20 and 3
                if (this.targetRepetitions > 20)
                    this.targetRepetitions = 20;
                else if (this.targetRepetitions < 3)
                    this.targetRepetitions = 3;

                this.inputBoxMaxReps.Text = this.targetRepetitions.ToString();
            }
        }

        //As seen in the reference from the comment... http://stackoverflow.com/questions/816334/wpf-a-textbox-that-has-an-event-that-fires-when-the-enter-key-is-pressed
        private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;

            // your event handler here
            this.updateParameters();

            e.Handled = true;
        }

        private void inputBoxError_Copy_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void inputBoxError_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void inputBoxDifficulty_TextChanged(object sender, TextChangedEventArgs e)
        {
        }


    }
}
