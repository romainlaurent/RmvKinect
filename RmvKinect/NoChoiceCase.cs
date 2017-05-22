using System.Windows;
using System.Windows.Shapes;

namespace RmvKinect
{
    internal class NoChoiceCase : AbstractChoiceCase
    {
        public NoChoiceCase(Point center) : base(center)
        {
            Circle.Stroke = System.Windows.Media.Brushes.Black;
            Circle.Fill = System.Windows.Media.Brushes.Red;
            Circle.Name = "AnswerNo";
        }
    }
}
