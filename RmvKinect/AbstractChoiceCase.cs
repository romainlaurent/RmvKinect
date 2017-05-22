using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace RmvKinect
{
    internal abstract class AbstractChoiceCase
    {
        protected readonly Ellipse Circle;
        private Point _center;

        protected AbstractChoiceCase(Point center)
        {
            Circle = new Ellipse
            {
                Width = 0,
                Height = 0
            };
            _center = center;

            var diameter = 120;

            Circle.SetValue(Canvas.LeftProperty, center.X);
            Circle.SetValue(Canvas.TopProperty, center.Y);

            var storyboard = new Storyboard();
            var animation1 = new DoubleAnimation(Circle.Width, diameter, new Duration(new TimeSpan(0, 0, 1)));
            var animation2 = new DoubleAnimation(Circle.Height, diameter, new Duration(new TimeSpan(0, 0, 1)));
            var animation3 = new DoubleAnimation(_center.X, _center.X - diameter / 2, new Duration(new TimeSpan(0, 0, 1)));
            var animation4 = new DoubleAnimation(_center.Y, _center.Y - diameter / 2, new Duration(new TimeSpan(0, 0, 1)));

            animation1.BeginTime = new TimeSpan(0, 0, 2);
            animation2.BeginTime = new TimeSpan(0, 0, 2);
            animation3.BeginTime = new TimeSpan(0, 0, 2);
            animation4.BeginTime = new TimeSpan(0, 0, 2);

            Storyboard.SetTarget(animation1, Circle);
            Storyboard.SetTarget(animation2, Circle);
            Storyboard.SetTarget(animation3, Circle);
            Storyboard.SetTarget(animation4, Circle);

            Storyboard.SetTargetProperty(animation1, new PropertyPath("(Ellipse.Width)"));
            Storyboard.SetTargetProperty(animation2, new PropertyPath("(Ellipse.Height)"));
            Storyboard.SetTargetProperty(animation3, new PropertyPath("(Canvas.Left)"));
            Storyboard.SetTargetProperty(animation4, new PropertyPath("(Canvas.Top)"));

            storyboard.Children.Add(animation1);
            storyboard.Children.Add(animation2);
            storyboard.Children.Add(animation3);
            storyboard.Children.Add(animation4);

            Circle.BeginStoryboard(storyboard);

        }

        public bool IsInside(Point point)
        {
            var dist = Math.Sqrt(Math.Pow(_center.X - point.X, 2) +
                                 Math.Pow(_center.Y - point.Y, 2));       
            return dist < Circle.Width;
        }

        public void Draw(UIElementCollection children)
        {
            children.Add(Circle);
        }
    }
}