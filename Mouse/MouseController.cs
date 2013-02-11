using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using Microsoft.Kinect;

namespace Kinect.Toolbox
{
    public class MouseController
    {
        static MouseController current;
        public static MouseController Current
        {
            get
            {
                if (current == null)
                {
                    current = new MouseController();
                }

                return current;
            }
        }

        Vector2? lastKnownPosition;
        float previousDepth;

        // Filters
        Vector2 savedFilteredJointPosition;
        Vector2 savedTrend;
        Vector2 savedBasePosition;
        int frameCount;

        // Impostors
        Canvas impostorCanvas;
        Visual rootVisual;
        MouseImpostor impostor;

        bool isMagnetized;
        DateTime magnetizationStartDate;
        FrameworkElement previousMagnetizedElement;

        // Gesture detector for click
        GestureDetector clickGestureDetector;
        bool clickGestureDetected;
        public GestureDetector ClickGestureDetector
        {
            get
            {
                return clickGestureDetector;
            }
            set
            {
                if (value != null)
                {
                    value.OnGestureDetected += (obj) =>
                        {
                            clickGestureDetected = true;
                        };
                }

                clickGestureDetector = value;
            }
        }

        public bool DisableGestureClick
        {
            get;
            set;
        }

        public Canvas ImpostorCanvas
        {
            set
            {
                if (value == null)
                {
                    if (impostorCanvas != null)
                        impostorCanvas.Children.Remove(impostor);

                    impostor.OnProgressionCompleted -= impostor_OnProgressionCompleted;

                    impostor = null;
                    rootVisual = null;
                    return;
                }

                impostorCanvas = value;
                rootVisual = impostorCanvas.Parent as Visual;
                impostor = new MouseImpostor();

                impostor.OnProgressionCompleted += impostor_OnProgressionCompleted;

                value.Children.Add(impostor);
            }
        }

        void impostor_OnProgressionCompleted()
        {
            if (previousMagnetizedElement != null)
            {
                var peer = UIElementAutomationPeer.CreatePeerForElement(previousMagnetizedElement);

                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;

                if (invokeProv == null)
                {
                    var toggleProv = peer.GetPattern(PatternInterface.Toggle) as IToggleProvider;

                    toggleProv.Toggle();
                }
                else
                    invokeProv.Invoke();

                previousMagnetizedElement = null;
                isMagnetized = false;
            }
        }

        public float MagneticRange
        {
            get;
            set;
        }

        public List<FrameworkElement> MagneticsControl
        {
            get;
            private set;
        }

        // Filter parameters
        public float TrendSmoothingFactor
        {
            get;
            set;
        }

        public float JitterRadius
        {
            get;
            set;
        }

        public float DataSmoothingFactor
        {
            get;
            set;
        }

        public float PredictionFactor
        {
            get;
            set;
        }

        public float GlobalSmooth
        {
            get;
            set;
        }

        MouseController()
        {
            TrendSmoothingFactor = 0.25f;
            JitterRadius = 0.05f;
            DataSmoothingFactor = 0.5f;
            PredictionFactor = 0.5f;

            GlobalSmooth = 0.9f;

            MagneticsControl = new List<FrameworkElement>();

            MagneticRange = 25.0f;
        }

        Vector2 FilterJointPosition(KinectSensor sensor, Joint joint)
        {
            Vector2 filteredJointPosition;
            Vector2 differenceVector;
            Vector2 currentTrend;
            float distance;

            Vector2 baseJointPosition = Tools.Convert(sensor, joint.Position);
            Vector2 prevFilteredJointPosition = savedFilteredJointPosition;
            Vector2 previousTrend = savedTrend;
            Vector2 previousBaseJointPosition = savedBasePosition;

            // Checking frames count
            switch (frameCount)
            {
                case 0:
                    filteredJointPosition = baseJointPosition;
                    currentTrend = Vector2.Zero;
                    break;
                case 1:
                    filteredJointPosition = (baseJointPosition + previousBaseJointPosition) * 0.5f;
                    differenceVector = filteredJointPosition - prevFilteredJointPosition;
                    currentTrend = differenceVector * TrendSmoothingFactor + previousTrend * (1.0f - TrendSmoothingFactor);
                    break;
                default:
                    // Jitter filter
                    differenceVector = baseJointPosition - prevFilteredJointPosition;
                    distance = Math.Abs(differenceVector.Length);

                    if (distance <= JitterRadius)
                    {
                        filteredJointPosition = baseJointPosition * (distance / JitterRadius) + prevFilteredJointPosition * (1.0f - (distance / JitterRadius));
                    }
                    else
                    {
                        filteredJointPosition = baseJointPosition;
                    }

                    // Double exponential smoothing filter
                    filteredJointPosition = filteredJointPosition * (1.0f - DataSmoothingFactor) + (prevFilteredJointPosition + previousTrend) * DataSmoothingFactor;

                    differenceVector = filteredJointPosition - prevFilteredJointPosition;
                    currentTrend = differenceVector * TrendSmoothingFactor + previousTrend * (1.0f - TrendSmoothingFactor);
                    break;
            }

            // Compute potential new position
            Vector2 potentialNewPosition = filteredJointPosition + currentTrend * PredictionFactor;

            // Cache current value
            savedBasePosition = baseJointPosition;
            savedFilteredJointPosition = filteredJointPosition;
            savedTrend = currentTrend;
            frameCount++;

            return potentialNewPosition;
        }

        public void SetHandPosition(KinectSensor sensor, Joint joint, Skeleton skeleton)
        {
            Vector2 vector2 = FilterJointPosition(sensor, joint);

            if (!lastKnownPosition.HasValue)
            {
                lastKnownPosition = vector2;
                previousDepth = joint.Position.Z;
                return;
            }

            bool isClicked = false;

            if (DisableGestureClick)
            {
            }
            else
            {
                if (ClickGestureDetector == null)
                    isClicked = Math.Abs(joint.Position.Z - previousDepth) > 0.05f;
                else
                    isClicked = clickGestureDetected;
            }

            if (impostor != null)
            {
                // Still magnetized ? 
                if ((vector2 - lastKnownPosition.Value).Length > 0.1f)
                {
                    impostor.Progression = 0;
                    isMagnetized = false;
                    previousMagnetizedElement = null;
                }

                // Looking for nearest magnetic control
                float minDistance = float.MaxValue;
                FrameworkElement nearestElement = null;
                var impostorPosition = new Vector2((float)(vector2.X * impostorCanvas.ActualWidth), (float)(vector2.Y * impostorCanvas.ActualHeight));

                foreach (FrameworkElement element in MagneticsControl)
                {
                    // Getting the four corners
                    var position = element.TransformToAncestor(rootVisual).Transform(new Point(0, 0));
                    var p1 = new Vector2((float)position.X, (float)position.Y);
                    var p2 = new Vector2((float)(position.X + element.ActualWidth), (float)position.Y);
                    var p3 = new Vector2((float)(position.X + element.ActualWidth), (float)(position.Y + element.ActualHeight));
                    var p4 = new Vector2((float)position.X, (float)(position.Y + element.ActualHeight));

                    // Minimal distance
                    float previousMinDistance = minDistance;
                    minDistance = Math.Min(minDistance, (impostorPosition - p1).Length);
                    minDistance = Math.Min(minDistance, (impostorPosition - p2).Length);
                    minDistance = Math.Min(minDistance, (impostorPosition - p3).Length);
                    minDistance = Math.Min(minDistance, (impostorPosition - p4).Length);

                    if (minDistance != previousMinDistance)
                    {
                        nearestElement = element;
                    }
                }

                // If a control is at a sufficient distance
                if (minDistance < MagneticRange || isMagnetized)
                {
                    // Magnetic control found
                    var position = nearestElement.TransformToAncestor(rootVisual).Transform(new Point(0, 0));

                    Canvas.SetLeft(impostor, position.X + nearestElement.ActualWidth / 2 - impostor.ActualWidth / 2);
                    Canvas.SetTop(impostor, position.Y + nearestElement.ActualHeight / 2);
                    lastKnownPosition = vector2;

                    if (!isMagnetized || previousMagnetizedElement != nearestElement)
                    {
                        isMagnetized = true;
                        magnetizationStartDate = DateTime.Now;
                    }
                    else
                    {
                        impostor.Progression = (int)(((DateTime.Now - magnetizationStartDate).TotalMilliseconds * 100) / 2000.0);
                    }
                }
                else
                {
                    Canvas.SetLeft(impostor, impostorPosition.X - impostor.ActualWidth / 2);
                    Canvas.SetTop(impostor, impostorPosition.Y);
                }

                if (!isMagnetized)
                    lastKnownPosition = vector2;

                previousMagnetizedElement = nearestElement;
            }
            else
            {
                MouseInterop.ControlMouse((int)((vector2.X - lastKnownPosition.Value.X) * Screen.PrimaryScreen.Bounds.Width * GlobalSmooth), (int)((vector2.Y - lastKnownPosition.Value.Y) * Screen.PrimaryScreen.Bounds.Height * GlobalSmooth), isClicked);
                lastKnownPosition = vector2;
            }


            previousDepth = joint.Position.Z;

            clickGestureDetected = false;
        }
    }
}
