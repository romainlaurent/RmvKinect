using System.Windows;
using System.Windows.Shapes;

namespace RmvKinect
{
    internal class YesChoiceCase : AbstractChoiceCase
    {
        public YesChoiceCase(Point center) : base(center)
        {
            Circle.Stroke = System.Windows.Media.Brushes.Black;
            Circle.Fill = System.Windows.Media.Brushes.Green;
            Circle.Name = "AnswerYes";
        }
    }
}
