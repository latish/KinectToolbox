using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using Microsoft.Kinect;

namespace Kinect.Toolbox
{
    public abstract class GestureDetector
    {       
        public int MinimalPeriodBetweenGestures { get; set; }

        readonly List<Entry> entries = new List<Entry>();

        public event Action<string> OnGestureDetected;

        DateTime lastGestureDate = DateTime.Now;

        readonly int windowSize; // Number of recorded positions

        // For drawing
        public Canvas DisplayCanvas
        {
            get;
            set;
        }

        public Color DisplayColor
        {
            get;
            set;
        }

        protected GestureDetector(int windowSize = 20)
        {
            this.windowSize = windowSize;
            MinimalPeriodBetweenGestures = 0;
            DisplayColor = Colors.Red;
        }

        protected List<Entry> Entries
        {
            get { return entries; }
        }

        public int WindowSize
        {
            get { return windowSize; }
        }

        public virtual void Add(SkeletonPoint position, KinectSensor sensor)
        {
            Entry newEntry = new Entry {Position = position.ToVector3(), Time = DateTime.Now};
            Entries.Add(newEntry);

            // Drawing
            if (DisplayCanvas != null)
            {
                newEntry.DisplayEllipse = new Ellipse
                {
                    Width = 4,
                    Height = 4,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    StrokeThickness = 2.0,
                    Stroke = new SolidColorBrush(DisplayColor),
                    StrokeLineJoin = PenLineJoin.Round
                };

                Vector2 vector2 = Tools.Convert(sensor, position);

                float x = (float)(vector2.X * DisplayCanvas.ActualWidth);
                float y = (float)(vector2.Y * DisplayCanvas.ActualHeight);

                Canvas.SetLeft(newEntry.DisplayEllipse, x - newEntry.DisplayEllipse.Width / 2);
                Canvas.SetTop(newEntry.DisplayEllipse, y - newEntry.DisplayEllipse.Height / 2);

                DisplayCanvas.Children.Add(newEntry.DisplayEllipse);
            }

            // Remove too old positions
            if (Entries.Count > WindowSize)
            {
                Entry entryToRemove = Entries[0];
                
                if (DisplayCanvas != null)
                {
                    DisplayCanvas.Children.Remove(entryToRemove.DisplayEllipse);
                }

                Entries.Remove(entryToRemove);
            }

            // Look for gestures
            LookForGesture();
        }

        protected abstract void LookForGesture();

        protected void RaiseGestureDetected(string gesture)
        {
            // Too close?
            if (DateTime.Now.Subtract(lastGestureDate).TotalMilliseconds > MinimalPeriodBetweenGestures)
            {
                if (OnGestureDetected != null)
                    OnGestureDetected(gesture);

                lastGestureDate = DateTime.Now;
            }

            Entries.ForEach(e=>
                                {
                                    if (DisplayCanvas != null)
                                        DisplayCanvas.Children.Remove(e.DisplayEllipse);
                                });
            Entries.Clear();
        }
    }
}