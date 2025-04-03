using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Controls
{
    public sealed partial class WaveProgressBar : UserControl
    {
        public WaveProgressBar()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(WaveProgressBar), new PropertyMetadata(0.0, OnValueChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(WaveProgressBar), new PropertyMetadata(100.0));

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (WaveProgressBar)d;
            control.UpdateProgress();
        }

        private void UpdateProgress()
        {
            // 计算角度
            double angle = (Value / Maximum) * 360;
            var radians = angle * (Math.PI / 180);
            var x = 90 + 80 * Math.Cos(radians);
            var y = 90 + 80 * Math.Sin(radians);

            // 更新 ArcSegment 的终点
            ArcSegment.Point = new Point(x, y);

            // 更新文本
            PercentageText.Text = $"{(int)Value}%";
        }
    }
}
