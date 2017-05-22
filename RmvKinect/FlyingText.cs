//------------------------------------------------------------------------------
// <copyright file="FlyingText.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RmvKinect
{
    // FlyingText creates text that flys out from a given point, and fades as it gets bigger.
    // NewFlyingText() can be called as often as necessary, and there can be many texts flying out at once.
    public class FlyingText
    {
        private static readonly List<FlyingText> FlyingTexts = new List<FlyingText>();
        private readonly double _fontGrow;
        private readonly string _text;
        private double _alpha;
        private Brush _brush;
        private Point _center;
        private double _fontSize;
        private Label _label;

        public FlyingText(string s, double size, Point center)
        {
            _text = s;
                _fontSize = Math.Max(1, size);
                _fontGrow = Math.Sqrt(size) * 0.4;
                _center = center;
                _alpha = 1.0;
            _label = null;
            _brush = null;
        }

        public static void NewFlyingText(double size, Point center, string s)
        {
            FlyingTexts.Add(new FlyingText(s, size, center));
        }

        public static void Draw(UIElementCollection children)
        {
            for (var i = 0; i < FlyingTexts.Count; i++)
            {
                var flyout = FlyingTexts[i];
                if (flyout._alpha <= 0)
                {
                    FlyingTexts.Remove(flyout);
                    i--;
                }
            }

            foreach (var flyout in FlyingTexts)
            {
                flyout.Advance();
                children.Add(flyout._label);
            }
        }

        private void Advance()
        {
            _alpha -= 0.01;
            if (_alpha < 0)
                _alpha = 0;

            if (_brush == null)
                _brush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            if (_label == null)
                _label = BannerText.MakeSimpleLabel(_text, new Rect(0, 0, 0, 0), _brush);

            _brush.Opacity = Math.Pow(_alpha, 1.5);
            _label.Foreground = _brush;
            _fontSize += _fontGrow;
            _label.FontSize = Math.Max(1, _fontSize);
            var renderRect = new Rect(_label.RenderSize);
            _label.SetValue(Canvas.LeftProperty, _center.X - renderRect.Width / 2);
            _label.SetValue(Canvas.TopProperty, _center.Y - renderRect.Height / 2);
        }
    }
}