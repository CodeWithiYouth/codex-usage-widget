using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WindowsUsageWidget
{
    public class ThresholdRange : Control
    {
        int low,medium=50;bool draggingLow=true;
        public event Action Changed;
        public int Low { get { return low; } set { var next=Math.Max(0,Math.Min(medium,value));if(next==low)return;low=next;Notify(); } }
        public int Medium { get { return medium; } set { var next=Math.Max(low,Math.Min(100,value));if(next==medium)return;medium=next;Notify(); } }
        public Color LowColor=Color.FromRgb(235,176,72),MediumColor=Color.FromRgb(230,207,135),NormalColor=Color.FromRgb(104,219,197);
        public ThresholdRange() { Height=38;Focusable=true;Cursor=Cursors.Hand;ToolTip="拖动两个端点调整低、中余量阈值；方向键微调，Tab 切换端点。"; }
        void Notify() { InvalidateVisual();var handler=Changed;if(handler!=null)handler(); }
        double X(int value) { return 10+Math.Max(1,ActualWidth-20)*value/100.0; }
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);double y=19;dc.DrawRectangle(Brushes.Transparent,null,new Rect(0,0,ActualWidth,ActualHeight));
            dc.DrawLine(new Pen(new SolidColorBrush(LowColor),5) { StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round },new Point(10,y),new Point(X(low),y));
            dc.DrawLine(new Pen(new SolidColorBrush(MediumColor),5),new Point(X(low),y),new Point(X(medium),y));
            dc.DrawLine(new Pen(new SolidColorBrush(NormalColor),5) { EndLineCap=PenLineCap.Round },new Point(X(medium),y),new Point(Math.Max(10,ActualWidth-10),y));
            dc.DrawEllipse(new SolidColorBrush(LowColor),new Pen(Brushes.White,2),new Point(X(low),y),7,7);
            dc.DrawEllipse(new SolidColorBrush(MediumColor),new Pen(Brushes.White,2),new Point(X(medium),y),7,7);
            if(IsKeyboardFocused)dc.DrawEllipse(null,new Pen(UI.Brush(99,207,255),1),new Point(X(draggingLow?low:medium),y),10,10);
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) { base.OnMouseLeftButtonDown(e);Focus();var x=e.GetPosition(this).X;draggingLow=low==medium?x<X(low):Math.Abs(x-X(low))<=Math.Abs(x-X(medium));CaptureMouse();Move(x);e.Handled=true; }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e);if(IsMouseCaptured)Move(e.GetPosition(this).X); }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) { base.OnMouseLeftButtonUp(e);if(IsMouseCaptured) { ReleaseMouseCapture();e.Handled=true; } }
        void Move(double x) { var value=(int)Math.Round((x-10)/Math.Max(1,ActualWidth-20)*100);if(draggingLow)Low=value;else Medium=value; }
        protected override void OnKeyDown(KeyEventArgs e) { if(e.Key==Key.Tab&&!Keyboard.IsKeyDown(Key.LeftShift)&&!Keyboard.IsKeyDown(Key.RightShift)&&draggingLow) { draggingLow=false;InvalidateVisual();e.Handled=true;return; }if(e.Key==Key.Left||e.Key==Key.Right) { var step=e.Key==Key.Left?-1:1;if(draggingLow)Low+=step;else Medium+=step;e.Handled=true; }else base.OnKeyDown(e); }
    }
}
