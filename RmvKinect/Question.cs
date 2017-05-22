using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace RmvKinect
{
    public class Question
    {
        private static readonly List<Question> Questions = new List<Question>();
        private static int _index;
        private readonly string _text;
        private readonly double _fontSize;
        private Brush _brush;
        private Label _label;
        private readonly QuestionChoice _questionChoice;
        private int _id;

        private Question(string s, double size, int id)
        {
            _text = s;
            _fontSize = size;
            _brush = null;
            _label = null;
            _id = id;
            _questionChoice = new QuestionChoice();
        }

        public static void Add(string s, double size, int id)
        {
            Questions.Add(new Question(s, size, id));
        }

        public static void CheckAnswer(Point handLeft, Point handRight)
        {
            Questions[_index]._questionChoice.CheckAnswer(handLeft, handRight);
        }

        public static void Remove(int index)
        {
            Questions.Remove(Questions[index]);
        }

        public static bool IsEnable() => Questions[_index]?._label != null;

        public static void EnableDisplay()
        {
            if (Questions[_index]._label == null)
                Questions[_index].MakeLabel();
            Questions[_index].EnableChoice();
        }

        public static void DisableDisplay()
        {
            Questions[_index]._label = null;
            Questions[_index]._questionChoice.DeleteChoice();
        }

        public static void Next()
        {
            Questions[_index].DisableChoice();
            _index++;

            if (_index >= Questions.Count)
                _index = 0;
            if (_index < Questions.Count)
                Questions[_index].EnableChoice();
        }

        public static void Preview()
        {
            Questions[_index].DisableChoice();
            _index--;
            if (_index < 0)
                _index = 0;
        }

        private void MakeLabel()
        {
            if (_brush == null)
                _brush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            if (_label == null)
                _label = BannerText.MakeSimpleLabel(_text, MainWindow.ScreenRect, _brush);

            _label.Foreground = _brush;
            _label.FontSize = _fontSize;
            _label.HorizontalContentAlignment = HorizontalAlignment.Center;
            _label.VerticalContentAlignment = VerticalAlignment.Top;
            var renderRect = new Rect(_label.RenderSize);
            _label.SetValue(Canvas.TopProperty, renderRect.Height / 2);
        }

        private void EnableChoice()
        {
            _questionChoice.CreateChoice();
        }

        private void DisableChoice()
        {
            _questionChoice.DeleteChoice();
        }

        public static void Draw(UIElementCollection children)
        {
            Questions[_index]?._questionChoice.Draw(children);
            if (Questions[_index]?._label != null)
            {
                var renderRect = new Rect(Questions[_index]._label.RenderSize);
                Questions[_index]._label.SetValue(Canvas.LeftProperty,
                    MainWindow.ScreenRect.Width / 2 - renderRect.Height / 2);
                Questions[_index]._label.SetValue(Canvas.TopProperty, renderRect.Height / 2);
                children.Add(Questions[_index]._label);
            }
        }

        public static int GetId()
        {
            return Questions[_index]._id;
        }

        public static async void MajQuestion()
        {
            string result = await DownloadQuestion();
            dynamic questions = JsonConvert.DeserializeObject(result);
            foreach (var question in questions)
            {
                Add(questions.Name, 40, question.Id);
            }           
        }

        private static async Task<string> DownloadQuestion()
        {
            string page = "http://raisemyvoice.azurewebsites.net/Question/All";

            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(page))
                {
                    using (HttpContent content = response.Content)
                    {
                        string result = await content.ReadAsStringAsync();
                        return result;
                    }
                }
            }
        }
    }
}