using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Samples.Kinect.WpfViewers;
using RmvKinect.Properties;

namespace RmvKinect
{
    /// <summary>
    ///     Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static readonly DependencyProperty KinectSensorManagerProperty =
            DependencyProperty.Register(
                "KinectSensorManager",
                typeof(KinectSensorManager),
                typeof(MainWindow),
                new PropertyMetadata(null));

        #region Private State

        private const int TimerResolution = 2; // ms
        private const int NumIntraFrame = 3;
        private const double MaxFramerate = 70;
        private const double MinFramerate = 15;

        private readonly Dictionary<int, Player> _players = new Dictionary<int, Player>();
        private readonly KinectSensorChooser _sensorChooser = new KinectSensorChooser();

        private DateTime _lastFrameDrawn = DateTime.MinValue;
        private DateTime _predNextFrame = DateTime.MinValue;
        private DateTime _lastPlayerFram = DateTime.MinValue;
        private double _actualFrameTime;

        private Skeleton[] _skeletonData;

        // Player(s) placement in scene (z collapsed):
        private Rect _playerBounds;

        public static Rect ScreenRect;

        private double _targetFramerate = MaxFramerate;
        private int _frameCount;
        private bool _runningGameThread;
        private int _playersAlive;

        #endregion Private State

        #region ctor + Window Events

        public MainWindow()
        {
            KinectSensorManager = new KinectSensorManager();
            KinectSensorManager.KinectSensorChanged += KinectSensorChanged;
            DataContext = KinectSensorManager;

            InitializeComponent();

            //De-comment if you want to see kinect statue
            //this.SensorChooserUI.KinectSensorChooser = sensorChooser;
            _sensorChooser.Start();

            // Bind the KinectSensor from the sensorChooser to the KinectSensor on the KinectSensorManager
            var kinectSensorBinding = new Binding("Kinect") {Source = _sensorChooser};
            BindingOperations.SetBinding(KinectSensorManager, KinectSensorManager.KinectSensorProperty,
                kinectSensorBinding);

            RestoreWindowState();
        }

        public KinectSensorManager KinectSensorManager
        {
            get => (KinectSensorManager) GetValue(KinectSensorManagerProperty);
            set => SetValue(KinectSensorManagerProperty, value);
        }

        // Since the timer resolution defaults to about 10ms precisely, we need to
        // increase the resolution to get framerates above between 50fps with any
        // consistency.
        [DllImport("Winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern int TimeBeginPeriod(uint period);

        private void RestoreWindowState()
        {
            // Restore window state to that last used
            var bounds = Settings.Default.PrevWinPosition;
            if (Math.Abs(bounds.Right - bounds.Left) > 0)
            {
                Top = bounds.Top;
                Left = bounds.Left;
                Height = bounds.Height;
                Width = bounds.Width;
            }

            WindowState = (WindowState) Settings.Default.WindowState;
        }

        private void WindowLoaded(object sender, EventArgs e)
        {
            Playfield.ClipToBounds = true;

            UpdatePlayfieldSize();

            TimeBeginPeriod(TimerResolution);
            var myGameThread = new Thread(GameThread);
            myGameThread.SetApartmentState(ApartmentState.STA);
            myGameThread.Start();

            //Question.MajQuestion();

            Question.Add("Avez-vous aimé cette présentation?", 40, 2);
            Question.Add("Aimez-vous la ville de Toulon?", 40, 4);
            //Question.Add("La foi d'aller au sport?", 40);

            FlyingText.NewFlyingText(ScreenRect.Width / 30, new Point(ScreenRect.Width / 2, ScreenRect.Height / 2),
                "Hey man!");
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            _sensorChooser.Stop();

            Settings.Default.PrevWinPosition = RestoreBounds;
            Settings.Default.WindowState = (int) WindowState;
            Settings.Default.Save();
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            KinectSensorManager.KinectSensor = null;
        }

        #endregion ctor + Window Events

        #region Kinect discovery + setup

        private void KinectSensorChanged(object sender, KinectSensorManagerEventArgs<KinectSensor> args)
        {
            if (null != args.OldValue)
                UninitializeKinectServices(args.OldValue);

            if (null != args.NewValue)
                InitializeKinectServices(KinectSensorManager, args.NewValue);
        }

        // Kinect enabled apps should customize which Kinect services it initializes here.
        private void InitializeKinectServices(KinectSensorManager kinectSensorManager, KinectSensor sensor)
        {
            // Application should enable all streams first.
            kinectSensorManager.ColorFormat = ColorImageFormat.RgbResolution640x480Fps30;
            kinectSensorManager.ColorStreamEnabled = true;

            sensor.SkeletonFrameReady += SkeletonsReady;
            kinectSensorManager.TransformSmoothParameters = new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.5f,
                Prediction = 0.5f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            };
            kinectSensorManager.SkeletonStreamEnabled = true;
            kinectSensorManager.KinectSensorEnabled = true;
        }

        // Kinect enabled apps should uninitialize all Kinect services that were initialized in InitializeKinectServices() here.
        private void UninitializeKinectServices(KinectSensor sensor)
        {
            sensor.SkeletonFrameReady -= SkeletonsReady;
        }

        #endregion Kinect discovery + setup

        #region Kinect Skeleton processing

        private void SkeletonsReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (var skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null) return;
                var skeletonSlot = 0;

                if (_skeletonData == null || _skeletonData.Length != skeletonFrame.SkeletonArrayLength)
                    _skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];

                skeletonFrame.CopySkeletonDataTo(_skeletonData);

                foreach (var skeleton in _skeletonData)
                {
                    if (SkeletonTrackingState.Tracked == skeleton.TrackingState)
                    {
                        Player player;
                        if (_players.ContainsKey(skeletonSlot))
                        {
                            player = _players[skeletonSlot];
                        }
                        else
                        {
                            player = new Player(skeletonSlot);
                            player.SetBounds(_playerBounds);
                            _players.Add(skeletonSlot, player);
                        }

                        player.LastUpdated = DateTime.Now;
                        _lastPlayerFram = player.LastUpdated;

                        // Update player's bone and joint positions
                        if (skeleton.Joints.Count > 0)
                        {
                            player.IsAlive = true;

                            // Head, hands, feet (hit testing happens in order here)
                            player.UpdateJointPosition(skeleton.Joints, JointType.Head);
                            player.UpdateJointPosition(skeleton.Joints, JointType.HandLeft);
                            player.UpdateJointPosition(skeleton.Joints, JointType.HandRight);
                            player.UpdateJointPosition(skeleton.Joints, JointType.FootLeft);
                            player.UpdateJointPosition(skeleton.Joints, JointType.FootRight);

                            // Hands and arms
                            player.UpdateBonePosition(skeleton.Joints, JointType.HandRight, JointType.WristRight);
                            player.UpdateBonePosition(skeleton.Joints, JointType.WristRight, JointType.ElbowRight);
                            player.UpdateBonePosition(skeleton.Joints, JointType.ElbowRight, JointType.ShoulderRight);

                            player.UpdateBonePosition(skeleton.Joints, JointType.HandLeft, JointType.WristLeft);
                            player.UpdateBonePosition(skeleton.Joints, JointType.WristLeft, JointType.ElbowLeft);
                            player.UpdateBonePosition(skeleton.Joints, JointType.ElbowLeft, JointType.ShoulderLeft);

                            // Head and Shoulders
                            player.UpdateBonePosition(skeleton.Joints, JointType.ShoulderCenter, JointType.Head);
                            player.UpdateBonePosition(skeleton.Joints, JointType.ShoulderLeft,
                                JointType.ShoulderCenter);
                            player.UpdateBonePosition(skeleton.Joints, JointType.ShoulderCenter,
                                JointType.ShoulderRight);

                            // Legs
                            player.UpdateBonePosition(skeleton.Joints, JointType.HipLeft, JointType.KneeLeft);
                            player.UpdateBonePosition(skeleton.Joints, JointType.KneeLeft, JointType.AnkleLeft);
                            player.UpdateBonePosition(skeleton.Joints, JointType.AnkleLeft, JointType.FootLeft);

                            player.UpdateBonePosition(skeleton.Joints, JointType.HipRight, JointType.KneeRight);
                            player.UpdateBonePosition(skeleton.Joints, JointType.KneeRight, JointType.AnkleRight);
                            player.UpdateBonePosition(skeleton.Joints, JointType.AnkleRight, JointType.FootRight);

                            player.UpdateBonePosition(skeleton.Joints, JointType.HipLeft, JointType.HipCenter);
                            player.UpdateBonePosition(skeleton.Joints, JointType.HipCenter, JointType.HipRight);

                            // Spine
                            player.UpdateBonePosition(skeleton.Joints, JointType.HipCenter, JointType.ShoulderCenter);
                        }
                    }

                    skeletonSlot++;
                }
            }
        }

        private void CheckPlayers()
        {
            foreach (var player in _players)
                if (!player.Value.IsAlive)
                {
                    // Player left scene since we aren't tracking it anymore, so remove from dictionary
                    _players.Remove(player.Value.GetId());
                    break;
                }

            // Count alive players
            var alive = _players.Count(player => player.Value.IsAlive);

            if (alive == _playersAlive) return;
            switch (alive)
            {
                case 2:
                    //this.myFallingThings.SetGameMode(GameMode.TwoPlayer);
                    break;
                case 1:
                    //this.myFallingThings.SetGameMode(GameMode.Solo);
                    break;
                default:
                    //this.myFallingThings.SetGameMode(GameMode.Off);
                    break;
            }

            _playersAlive = alive;
        }

        private void PlayfieldSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePlayfieldSize();
        }

        private void UpdatePlayfieldSize()
        {
            // Size of player wrt size of playfield, putting ourselves low on the screen.
            ScreenRect.X = 0;
            ScreenRect.Y = 0;
            ScreenRect.Width = Playfield.ActualWidth;
            ScreenRect.Height = Playfield.ActualHeight;

            BannerText.UpdateBounds(ScreenRect);

            _playerBounds.X = 0;
            _playerBounds.Width = Playfield.ActualWidth;
            _playerBounds.Y = Playfield.ActualHeight * 0.2;
            _playerBounds.Height = Playfield.ActualHeight * 0.75;

            foreach (var player in _players)
                player.Value.SetBounds(_playerBounds);
        }

        #endregion Kinect Skeleton processing

        #region GameTimer/Thread

        private void GameThread()
        {
            _runningGameThread = true;
            _predNextFrame = DateTime.Now;
            _actualFrameTime = 1000.0 / _targetFramerate;

            // Try to dispatch at as constant of a framerate as possible by sleeping just enough since
            // the last time we dispatched.
            while (_runningGameThread)
            {
                // Calculate average framerate.  
                var now = DateTime.Now;
                if (_lastFrameDrawn == DateTime.MinValue)
                    _lastFrameDrawn = now;

                var ms = now.Subtract(_lastFrameDrawn).TotalMilliseconds;
                _actualFrameTime = _actualFrameTime * 0.95 + 0.05 * ms;
                _lastFrameDrawn = now;

                // Adjust target framerate down if we're not achieving that rate
                _frameCount++;
                if (_frameCount % 100 == 0 && 1000.0 / _actualFrameTime < _targetFramerate * 0.92)
                    _targetFramerate = Math.Max(MinFramerate, (_targetFramerate + 1000.0 / _actualFrameTime) / 2);

                if (now > _predNextFrame)
                {
                    _predNextFrame = now;
                }
                else
                {
                    var milliseconds = _predNextFrame.Subtract(now).TotalMilliseconds;
                    if (milliseconds >= TimerResolution)
                        Thread.Sleep((int) (milliseconds + 0.5));
                }

                _predNextFrame += TimeSpan.FromMilliseconds(1000.0 / _targetFramerate);

                Dispatcher.Invoke(DispatcherPriority.Send, new Action<int>(HandleGameTimer), 0);
            }
        }

        private void HandleGameTimer(int param)
        {
            var now = DateTime.Now;

            //Si un joueur est present devant l'ecran
            if (_players.Count > 0 && !Question.IsEnable())
            {
                Question.EnableDisplay();
                Grid.Children.Remove(Logo);
            }

            foreach (var player in _players)
            {
                Question.CheckAnswer(player.Value.HandRightPosition, player.Value.HandLeftPosition);
            }

            var delay = now.Subtract(_lastPlayerFram).Seconds;

            //Si un joueur n'est present devant l'ecran depuis plus de 5 secondes
            if (delay > 5 && Question.IsEnable())
            {
                Question.DisableDisplay();
                Grid.Children.Add(Logo);
            }

        // Draw new Wpf scene by adding all objects to canvas
            Playfield.Children.Clear();

            Question.Draw(Playfield.Children);

            foreach (var player in _players)
            {
                player.Value.Draw(Playfield.Children);
            }

            BannerText.Draw(Playfield.Children);
            FlyingText.Draw(Playfield.Children);
            CheckPlayers();
        }

        #endregion GameTimer/Thread
    }
}