using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Kinect.Toolbox.Record;
using System.Linq;
using System.Windows.Shapes;
using System.Windows.Media;
using Microsoft.Kinect;

namespace Kinect.Toolbox
{
    public class SkeletonDisplayManager
    {
        readonly Canvas rootCanvas;
        readonly KinectSensor sensor;

        public SkeletonDisplayManager(KinectSensor kinectSensor, Canvas root)
        {
            rootCanvas = root;
            sensor = kinectSensor;
        }

        void GetCoordinates(JointType jointType, IEnumerable<Joint> joints, out float x, out float y)
        {
            var joint = joints.First(j => j.JointType == jointType);

            Vector2 vector2 = Tools.Convert(sensor, joint.Position);

            x = (float)(vector2.X * rootCanvas.ActualWidth);
            y = (float)(vector2.Y * rootCanvas.ActualHeight);
        }

        void Plot(JointType centerID, IEnumerable<Joint> joints)
        {
            float centerX;
            float centerY;

            GetCoordinates(centerID, joints, out centerX, out centerY);

            const double diameter = 8;

            Ellipse ellipse = new Ellipse
            {
                Width = diameter,
                Height = diameter,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                StrokeThickness = 4.0,
                Stroke = new SolidColorBrush(Colors.Green),
                StrokeLineJoin = PenLineJoin.Round
            };

            Canvas.SetLeft(ellipse, centerX - ellipse.Width / 2);
            Canvas.SetTop(ellipse, centerY - ellipse.Height / 2);

            rootCanvas.Children.Add(ellipse);
        }

        void Plot(JointType centerID, JointType baseID, JointCollection joints)
        {
            float centerX;
            float centerY;

            GetCoordinates(centerID, joints, out centerX, out centerY);

            float baseX;
            float baseY;

            GetCoordinates(baseID, joints, out baseX, out baseY);

            double diameter = Math.Abs(baseY - centerY);

            Ellipse ellipse = new Ellipse
            {
                Width = diameter,
                Height = diameter,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                StrokeThickness = 4.0,
                Stroke = new SolidColorBrush(Colors.Green),
                StrokeLineJoin = PenLineJoin.Round
            };

            Canvas.SetLeft(ellipse, centerX - ellipse.Width / 2);
            Canvas.SetTop(ellipse, centerY - ellipse.Height / 2);

            rootCanvas.Children.Add(ellipse);
        }

        void Trace(JointType sourceID, JointType destinationID, JointCollection joints)
        {
            float sourceX;
            float sourceY;

            GetCoordinates(sourceID, joints, out sourceX, out sourceY);

            float destinationX;
            float destinationY;

            GetCoordinates(destinationID, joints, out destinationX, out destinationY);

            Line line = new Line
                            {
                                X1 = sourceX,
                                Y1 = sourceY,
                                X2 = destinationX,
                                Y2 = destinationY,
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Top,
                                StrokeThickness = 4.0,                                
                                Stroke = new SolidColorBrush(Colors.Green),
                                StrokeLineJoin = PenLineJoin.Round
                            };


            rootCanvas.Children.Add(line);
        }

        public void Draw(Skeleton[] skeletons, bool seated)
        {
            rootCanvas.Children.Clear();
            foreach (Skeleton skeleton in skeletons)
            {
                if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
                    continue;

                Plot(JointType.HandLeft, skeleton.Joints);
                Trace(JointType.HandLeft, JointType.WristLeft, skeleton.Joints);
                Plot(JointType.WristLeft, skeleton.Joints);
                Trace(JointType.WristLeft, JointType.ElbowLeft, skeleton.Joints);
                Plot(JointType.ElbowLeft, skeleton.Joints);
                Trace(JointType.ElbowLeft, JointType.ShoulderLeft, skeleton.Joints);
                Plot(JointType.ShoulderLeft, skeleton.Joints);
                Trace(JointType.ShoulderLeft, JointType.ShoulderCenter, skeleton.Joints);
                Plot(JointType.ShoulderCenter, skeleton.Joints);

                Trace(JointType.ShoulderCenter, JointType.Head, skeleton.Joints);

                Plot(JointType.Head, JointType.ShoulderCenter, skeleton.Joints);

                Trace(JointType.ShoulderCenter, JointType.ShoulderRight, skeleton.Joints);
                Plot(JointType.ShoulderRight, skeleton.Joints);
                Trace(JointType.ShoulderRight, JointType.ElbowRight, skeleton.Joints);
                Plot(JointType.ElbowRight, skeleton.Joints);
                Trace(JointType.ElbowRight, JointType.WristRight, skeleton.Joints);
                Plot(JointType.WristRight, skeleton.Joints);
                Trace(JointType.WristRight, JointType.HandRight, skeleton.Joints);
                Plot(JointType.HandRight, skeleton.Joints);

                if (!seated)
                {
                    Trace(JointType.ShoulderCenter, JointType.Spine, skeleton.Joints);
                    Plot(JointType.Spine, skeleton.Joints);
                    Trace(JointType.Spine, JointType.HipCenter, skeleton.Joints);
                    Plot(JointType.HipCenter, skeleton.Joints);

                    Trace(JointType.HipCenter, JointType.HipLeft, skeleton.Joints);
                    Plot(JointType.HipLeft, skeleton.Joints);
                    Trace(JointType.HipLeft, JointType.KneeLeft, skeleton.Joints);
                    Plot(JointType.KneeLeft, skeleton.Joints);
                    Trace(JointType.KneeLeft, JointType.AnkleLeft, skeleton.Joints);
                    Plot(JointType.AnkleLeft, skeleton.Joints);
                    Trace(JointType.AnkleLeft, JointType.FootLeft, skeleton.Joints);
                    Plot(JointType.FootLeft, skeleton.Joints);

                    Trace(JointType.HipCenter, JointType.HipRight, skeleton.Joints);
                    Plot(JointType.HipRight, skeleton.Joints);
                    Trace(JointType.HipRight, JointType.KneeRight, skeleton.Joints);
                    Plot(JointType.KneeRight, skeleton.Joints);
                    Trace(JointType.KneeRight, JointType.AnkleRight, skeleton.Joints);
                    Plot(JointType.AnkleRight, skeleton.Joints);
                    Trace(JointType.AnkleRight, JointType.FootRight, skeleton.Joints);
                    Plot(JointType.FootRight, skeleton.Joints);
                }
            }
        }
    }
}
