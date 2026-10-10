using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Documents;

namespace WindowsUsageWidget
{
    public static class UI
    {
        public static bool Dark(Preferences p) { if(p.Theme=="Dark")return true;if(p.Theme=="Light")return false;try { return Convert.ToInt32(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize","AppsUseLightTheme",1))==0; }catch{return false;} }
        public static SolidColorBrush Brush(byte r,byte g,byte b) { return new SolidColorBrush(Color.FromRgb(r,g,b)); }
        public static TextBlock Text(string text,double size) { return new TextBlock { Text=text,FontSize=size,FontFamily=new FontFamily("Segoe UI"),VerticalAlignment=VerticalAlignment.Center }; }
        public static Button Icon(string glyph,string tooltip)
        {
            var icon=Text(glyph,18);icon.FontFamily=new FontFamily("Segoe MDL2 Assets");icon.HorizontalAlignment=HorizontalAlignment.Center;
            var button=new Button { Content=icon,Width=36,Height=36,Padding=new Thickness(0),ToolTip=tooltip,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Cursor=Cursors.Hand };
            var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(8));border.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty));
            var presenter=new FrameworkElementFactory(typeof(ContentPresenter));presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);presenter.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);border.AppendChild(presenter);
            var template=new ControlTemplate(typeof(Button));template.VisualTree=border;
            var hover=new Trigger { Property=UIElement.IsMouseOverProperty,Value=true };hover.Setters.Add(new Setter(Control.BackgroundProperty,new SolidColorBrush(Color.FromArgb(25,128,128,128))));template.Triggers.Add(hover);
            button.Template=template;System.Windows.Automation.AutomationProperties.SetName(button,tooltip);return button;
        }
        public static Grid TwoColumns(UIElement left,UIElement right)
        {
            var grid=new Grid();grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });grid.Children.Add(left);Grid.SetColumn(right,1);grid.Children.Add(right);return grid;
        }
        public static Grid Chrome(Window window)
        {
            var row=new Grid { Height=34,Background=Brushes.Transparent };
            row.MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e) { if(e.ChangedButton==MouseButton.Left)window.DragMove(); };
            var buttons=new StackPanel { Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right };
            var minimize=Icon("\uE921","最小化");minimize.Click+=delegate { window.WindowState=WindowState.Minimized; };
            var close=Icon("\uE8BB","关闭");close.Click+=delegate { window.Close(); };
            ((TextBlock)minimize.Content).FontSize=12;((TextBlock)close.Content).FontSize=12;
            buttons.Children.Add(minimize);buttons.Children.Add(close);row.Children.Add(buttons);return row;
        }
    }
    public static class Native
    {
        [StructLayout(LayoutKind.Sequential)] struct Margins { public int Left,Right,Top,Bottom; }
        [DllImport("dwmapi.dll")] static extern int DwmExtendFrameIntoClientArea(IntPtr handle,ref Margins margins);
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr handle,int attribute,ref int value,int size);
        public static void Apply(Window window,Panel root,Preferences settings)
        {
            var dark=UI.Dark(settings);root.SetValue(TextElement.ForegroundProperty,dark?UI.Brush(239,246,248):UI.Brush(20,32,36));
            TextOptions.SetTextRenderingMode(root,TextRenderingMode.Grayscale);
            TextOptions.SetTextFormattingMode(root,TextFormattingMode.Display);
            window.Resources["FormFill"]=new SolidColorBrush(dark?Color.FromRgb(39,46,56):Colors.White);
            window.Resources["FormText"]=dark?UI.Brush(239,246,248):UI.Brush(20,32,36);
            window.Resources["FormStroke"]=dark?UI.Brush(70,81,94):UI.Brush(211,222,229);
            var color=dark?Color.FromRgb(23,30,39):Color.FromRgb(245,248,250);
            window.Background=new SolidColorBrush(color);
            var handle=new WindowInteropHelper(window).Handle;if(handle==IntPtr.Zero)return;
            var source=HwndSource.FromHwnd(handle);source.CompositionTarget.BackgroundColor=color;
            var margins=new Margins();DwmExtendFrameIntoClientArea(handle,ref margins);
            if(Environment.OSVersion.Version.Build>=22000) { int corner=2;DwmSetWindowAttribute(handle,33,ref corner,4);int theme=dark?1:0;DwmSetWindowAttribute(handle,20,ref theme,4);int material=1;DwmSetWindowAttribute(handle,38,ref material,4); }
            root.Background=new SolidColorBrush(color);
            window.Opacity=settings.Material=="Transparent"?settings.OpacityPercent/100.0:1;
        }
    }
    public class Gauge
    {
        public StackPanel View=new StackPanel { Margin=new Thickness(0,0,0,20) };
        TextBlock title,number,caption,reset;
        Border track,fill;
        double ratio;
        public Gauge(string name)
        {
            title=UI.Text(name,13);number=UI.Text("—",32);number.FontWeight=FontWeights.SemiBold;number.HorizontalAlignment=HorizontalAlignment.Right;
            View.Children.Add(UI.TwoColumns(title,number));
            fill=new Border { Height=4,HorizontalAlignment=HorizontalAlignment.Left,CornerRadius=new CornerRadius(2) };
            track=new Border { Height=4,Margin=new Thickness(0,8,0,10),Background=new SolidColorBrush(Color.FromArgb(50,128,128,128)),CornerRadius=new CornerRadius(2),Child=fill };
            track.SizeChanged+=delegate { fill.Width=Math.Max(0,track.ActualWidth*ratio); };
            View.Children.Add(track);caption=UI.Text("暂无数据",11);reset=UI.Text("",11);reset.HorizontalAlignment=HorizontalAlignment.Right;
            caption.Opacity=.75;reset.Opacity=.75;View.Children.Add(UI.TwoColumns(caption,reset));
        }
        public void Update(Limit value,Preferences p)
        {
            if(value==null) { number.Text="—";ratio=0;fill.Width=0;caption.Text="暂无数据";reset.Text="";return; }
            var amount=p.ShowUsed?value.Used:value.Remaining;ratio=amount/100;fill.Width=Math.Max(0,track.ActualWidth*ratio);number.Text=amount.ToString("0.#")+"%";title.Text=value.Label;
            bool low=value.Remaining<=p.WarningPercent;bool dark=UI.Dark(p);
            var brush=low?(dark?UI.Brush(255,188,101):UI.Brush(111,63,0)):(dark?UI.Brush(108,224,207):UI.Brush(0,76,66));
            number.Foreground=brush;fill.Background=brush;caption.Text=(p.ShowUsed?"已用":"剩余")+(low?" · 偏低":"");reset.Text=value.Countdown();
            if(value.Reset>0)View.ToolTip="重置时间："+new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddSeconds(value.Reset).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
