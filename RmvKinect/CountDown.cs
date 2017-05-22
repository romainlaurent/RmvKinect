using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace RmvKinect
{
    public class CountDown
    {
        private readonly DispatcherTimer Timer;
        private readonly Point _center;
        private readonly int _val;
        private int _cpt;

        public bool CountDownFinished { get; set; }

        public CountDown(int val, Point center)
        {
            CountDownFinished = false;

            _center = center;
            _val = val;
            _cpt = val;

            Timer = new DispatcherTimer {Interval = new TimeSpan(0, 0, 1)};
            Timer.Tick += Timer_Tick;            
        }

        public void Start()
        {
            if (!Timer.IsEnabled)
            {
                FlyingText.NewFlyingText(100, _center, _val.ToString());
                Timer.Start();
            }
        }

        public void Stop()
        {
            _cpt = _val;
            if(Timer.IsEnabled)
                Timer.Stop();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _cpt--;
            if (_cpt <= 0)
            {
                Timer.Stop();
                FlyingText.NewFlyingText(100, _center, "A voté !");               
                CountDownFinished = true;
                Question.Next();
            }
            else
                FlyingText.NewFlyingText(100, _center, _cpt.ToString());
        }
    }
}