using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RmvKinect
{
    public class QuestionChoice
    {
        private YesChoiceCase _caseYes;
        private NoChoiceCase _caseNo;
        private int _answerNo;
        private int _answerYes;
        private readonly CountDown _countDown;

        public QuestionChoice()
        {
            _countDown = new CountDown(3,
                new Point(MainWindow.ScreenRect.Width / 2, MainWindow.ScreenRect.Height / 2));
            _caseYes = null;
            _caseNo = null;
            _answerNo = 0;
            _answerYes = 0;
        }

        public void CreateChoice()
        {
            if (_caseYes == null)
                _caseYes = new YesChoiceCase(new Point(MainWindow.ScreenRect.Width * 0.33,
                    MainWindow.ScreenRect.Height * 0.25));
            if (_caseNo == null)
                _caseNo = new NoChoiceCase(new Point(MainWindow.ScreenRect.Width * 0.66,
                    MainWindow.ScreenRect.Height * 0.25));
        }

        public void DeleteChoice()
        {
            _caseYes = null;
            _caseNo = null;
        }

        public void CheckAnswer(Point handLeft, Point handRight)
        {
            if (YesChoice(handLeft) || YesChoice(handRight))
            {
                _countDown.Start();
                if (_countDown.CountDownFinished)
                {
                    SendAnswer(1);
                    _answerYes++;
//                    Question.Next();
                    _countDown.CountDownFinished = false;
                }
            }
            else if (NoChoice(handLeft) || NoChoice((handRight)))
            {
                _countDown.Start();
                if (_countDown.CountDownFinished)
                {
                    SendAnswer(0);
                    _answerNo++;
//                    Question.Next();
                    _countDown.CountDownFinished = false;
                }                    
            }
            else
            {
                _countDown.Stop();
            }
        }

        private bool YesChoice(Point point) => _caseYes.IsInside(point);

        private bool NoChoice(Point point) => _caseNo.IsInside(point);

        private async void SendAnswer(int val)
        {
            string page = "http://raisemyvoice.azurewebsites.net/Answer/Add";
            using (HttpClient client = new HttpClient())
            {
                HttpContent contentPost = new StringContent("{IdQuestion:\"" + Question.GetId() + "\", " + "Value:\"" + val + "\"}", Encoding.UTF8);
                await client.PostAsync(new Uri(page), contentPost);
            }
        }

        public void Draw(UIElementCollection children)
        {
            _caseYes?.Draw(children);
            _caseNo?.Draw(children);
        }
    }
}