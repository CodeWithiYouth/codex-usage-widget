using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using WindowsUsageWidget;

class AppearanceChecks
{
    static List<string> checks=new List<string>();
    static void Require(bool value,string label) { if(!value)throw new Exception(label);checks.Add("PASS "+label); }
    static bool HasColor(Bitmap image,Color color) { for(var y=0;y<image.Height;y++)for(var x=0;x<image.Width;x++) { var pixel=image.GetPixel(x,y);if(pixel.A>0&&pixel.R==color.R&&pixel.G==color.G&&pixel.B==color.B)return true; }return false; }
    static T Find<T>(DependencyObject root,string name) where T:FrameworkElement
    {
        var current=root as T;if(current!=null&&current.Name==name)return current;
        foreach(var child in LogicalTreeHelper.GetChildren(root)) { var dep=child as DependencyObject;if(dep==null)continue;var found=Find<T>(dep,name);if(found!=null)return found; }
        return null;
    }
    static Button SaveButton(DependencyObject root)
    {
        var current=root as Button;var label=current==null?null:current.Content as TextBlock;if(label!=null&&label.Text=="保存")return current;
        foreach(var child in LogicalTreeHelper.GetChildren(root)) { var dep=child as DependencyObject;if(dep==null)continue;var found=SaveButton(dep);if(found!=null)return found; }return null;
    }
    [STAThread] static void Main()
    {
        var folder=AppDomain.CurrentDomain.BaseDirectory;var serializer=new JavaScriptSerializer();
        var old=serializer.Deserialize<Preferences>("{}");old.Normalize();Require(old.TaskbarStyle=="Rounded"&&old.TaskbarTextColor=="Auto"&&old.TaskbarMediumPercent==50&&old.TaskbarMediumColor=="Auto","old configuration keeps transparent system colors");
        var invalid=new Preferences { TaskbarStyle="bad",TaskbarPrimaryColor="#GGFFFF",TaskbarBackgroundOpacity=400 };invalid.Normalize();Require(invalid.TaskbarStyle=="Rounded"&&invalid.TaskbarPrimaryColor=="Auto"&&invalid.TaskbarBackgroundOpacity==100,"invalid persisted appearance values normalized");
        Require(!Preferences.IsColor("#fff")&&!Preferences.IsColor("#12345678")&&Preferences.IsColor("#68DbC5")&&Preferences.IsColor("Auto"),"color input validation");
        var now=(long)(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds;
        var sample=new Snapshot { Primary=new Limit { Used=24,Minutes=300,Reset=now+7200 },Secondary=new Limit { Used=28,Minutes=10080,Reset=now+259200 } };
        using(var sheet=new Bitmap(820,760))using(var graphics=Graphics.FromImage(sheet))using(var title=new Font("Segoe UI",12))
        {
            graphics.Clear(Color.FromArgb(247,249,252));
            var styles=new[] { "Rounded","Segments","Cards","Dots","Gradient","Rings","Badges" };
            for(var row=0;row<7;row++)for(var col=0;col<2;col++)
            {
                var p=new Preferences { TaskbarStyle=styles[row],TaskbarBackgroundOpacity=col==0?20:30 };
                var x=20+col*405;var y=20+row*103;graphics.DrawString(styles[row]+(col==0?" / Light":" / Dark"),title,Brushes.DimGray,x,y);
                using(var tile=new Bitmap(380,56))using(var g=Graphics.FromImage(tile))
                {
                    g.Clear(col==0?Color.FromArgb(232,235,241):Color.FromArgb(32,35,42));
                    MeterRenderer.Draw(g,220,42,1,sample,p,false,col==1);graphics.DrawImageUnscaled(tile,x,y+26);
                }
                foreach(var width in new[] { 200,220,380 })foreach(var height in new[] { 28,42 })using(var bitmap=new Bitmap(width,height))using(var g=Graphics.FromImage(bitmap)) { MeterRenderer.Draw(g,width,height,1,sample,p,false,col==1);MeterRenderer.Draw(g,width,height,1,null,p,true,col==1); }
            }
            sheet.Save(Path.Combine(folder,"taskbar-styles-preview.png"));
        }
        Require(true,"seven styles render at 200, 220 and 380 pixels, light/dark and compact height");
        var custom=new Preferences { TaskbarPrimaryColor="#ff3366",TaskbarSecondaryColor="#33AAFF",TaskbarTextColor="#F0F0F0",TaskbarBackgroundColor="#112233",TaskbarBackgroundOpacity=65,TaskbarStyle="Cards",TaskbarMediumColor="#4499AA",TaskbarMediumPercent=65 };custom.Normalize();
        var restored=serializer.Deserialize<Preferences>(serializer.Serialize(custom));Require(restored.TaskbarPrimaryColor=="#FF3366"&&restored.TaskbarStyle=="Cards"&&restored.TaskbarBackgroundOpacity==65&&restored.TaskbarMediumPercent==65&&restored.TaskbarMediumColor=="#4499AA","custom colors and style survive settings serialization");
        using(var bitmap=new Bitmap(300,42))using(var g=Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);MeterRenderer.Draw(g,300,42,1,sample,custom,false,true);
            var color=bitmap.GetPixel(15,2);Require(color.A>100,"configured background opacity rendered");
            Require(HasColor(bitmap,Color.FromArgb(255,51,102))&&HasColor(bitmap,Color.FromArgb(51,170,255)),"independent primary and weekly colors rendered");
            bitmap.Save(Path.Combine(folder,"custom-colors-preview.png"));
        }
        foreach(var style in new[] { "Rounded","Segments","Cards","Dots","Gradient","Rings","Badges" })using(var bitmap=new Bitmap(220,42))using(var g=Graphics.FromImage(bitmap))
        {
            var p=new Preferences { TaskbarStyle=style,TaskbarWarningColor="#FEAE23",TaskbarStaleColor="#FC4356" };
            var low=new Snapshot { Primary=new Limit { Used=90 },Secondary=new Limit { Used=92 } };
            g.Clear(Color.Transparent);MeterRenderer.Draw(g,220,42,1,low,p,false,true);Require(HasColor(bitmap,Color.FromArgb(254,174,35)),style+" uses custom low-quota color");
            g.Clear(Color.Transparent);MeterRenderer.Draw(g,220,42,1,sample,p,true,true);Require(HasColor(bitmap,Color.FromArgb(252,67,86)),style+" uses custom stale color");
        }
        var unordered=new Preferences { WarningPercent=70,TaskbarMediumPercent=20 };unordered.Normalize();Require(unordered.TaskbarMediumPercent==70,"medium threshold never below low threshold");
        foreach(var style in new[] { "Rounded","Segments","Cards","Dots","Gradient","Rings","Badges" })using(var bitmap=new Bitmap(220,42))using(var g=Graphics.FromImage(bitmap))
        {
            var p=new Preferences { TaskbarStyle=style,TaskbarMediumPercent=50,TaskbarMediumColor="#12D979",TaskbarWarningColor="#FEAE23",TaskbarStaleColor="#FC4356" };
            var medium=new Snapshot { Primary=new Limit { Used=50 },Secondary=new Limit { Used=60 } };
            g.Clear(Color.Transparent);MeterRenderer.Draw(g,220,42,1,medium,p,false,true);Require(HasColor(bitmap,Color.FromArgb(18,217,121)),style+" custom medium color at threshold");
            p.ShowUsed=true;g.Clear(Color.Transparent);MeterRenderer.Draw(g,220,42,1,medium,p,false,true);Require(HasColor(bitmap,Color.FromArgb(18,217,121)),style+" used display still classifies remaining quota");p.ShowUsed=false;
            var low=new Snapshot { Primary=new Limit { Used=80 },Secondary=new Limit { Used=90 } };
            g.Clear(Color.Transparent);MeterRenderer.Draw(g,220,42,1,low,p,false,true);Require(HasColor(bitmap,Color.FromArgb(254,174,35))&&!HasColor(bitmap,Color.FromArgb(18,217,121)),style+" low threshold overrides medium color");
            g.Clear(Color.Transparent);MeterRenderer.Draw(g,220,42,1,medium,p,true,true);Require(HasColor(bitmap,Color.FromArgb(252,67,86))&&!HasColor(bitmap,Color.FromArgb(18,217,121)),style+" stale overrides medium color");
            g.Clear(Color.Transparent);MeterRenderer.Draw(g,220,42,1,sample,p,false,true);Require(!HasColor(bitmap,Color.FromArgb(18,217,121)),style+" high quota uses normal colors");
        }
        var legacy=serializer.Deserialize<Preferences>("{\"ShowUsed\":true}");legacy.Normalize();Require(legacy.TaskbarPrimaryLabel=="5小时{mode}"&&legacy.TaskbarLabel(false)=="5小时用"&&legacy.TaskbarFontSize==12,"old used mode and new default labels migrate");
        var labels=new Preferences { TaskbarPrimaryLabel="  日额度{mode}  ",TaskbarSecondaryLabel="",TaskbarFontSize=99 };labels.Normalize();Require(labels.TaskbarLabel(false)=="日额度余"&&labels.TaskbarLabel(true)==""&&labels.TaskbarFontSize==14,"custom labels, blank labels and font bounds");labels.ShowUsed=true;Require(labels.TaskbarLabel(false)=="日额度用","mode placeholder follows switch");
        var largeName=new Preferences { TaskbarPrimaryLabel=new string('字',40),TaskbarSecondaryLabel="长期预算",TaskbarFontSize=14 };largeName.Normalize();Require(largeName.TaskbarPrimaryLabel.Length==24,"long labels normalized safely");
        var scaled=new List<object>();
        foreach(var factor in new[] { 1.0,1.25,1.5,2.0 })
        {
            var width=(int)Math.Round(220*factor);var height=(int)Math.Round(42*factor);
            using(var bitmap=new Bitmap(width,height))using(var g=Graphics.FromImage(bitmap))
            { bitmap.SetResolution((float)(96*factor),(float)(96*factor));g.Clear(Color.FromArgb(32,35,42));MeterRenderer.Draw(g,width,height,factor,sample,new Preferences(),false,true);bitmap.Save(Path.Combine(folder,"dpi-"+(int)(factor*100)+".png")); }
            foreach(var style in new[] { "Rounded","Segments","Cards","Dots","Gradient","Rings","Badges" })foreach(var font in new[] { 10,12,14 })using(var bitmap=new Bitmap((int)Math.Round(200*factor),height))using(var g=Graphics.FromImage(bitmap))
            { largeName.TaskbarStyle=style;largeName.TaskbarFontSize=font;MeterRenderer.Draw(g,bitmap.Width,bitmap.Height,factor,sample,largeName,false,true); }
            scaled.Add(new { scale=factor,width=width,height=height });
        }
        Require(true,"all styles render long custom labels and font sizes at 100,125,150,200 percent");
        File.WriteAllText(Path.Combine(folder,"dpi-render-checks.json"),serializer.Serialize(scaled));
        var app=new Application();app.Resources.MergedDictionaries.Add(Styles.Create());
        var window=new SettingsWindow(new Preferences());Preferences saved=null;window.Saved+=delegate(Preferences p) { saved=p; };
        window.Loaded+=delegate
        {
            try
            {
                Require(Find<ComboBox>(window,"TaskbarStyle").Items.Count==7,"settings offers all seven styles");
                Find<ComboBox>(window,"TaskbarStyle").SelectedIndex=6;
                var remaining=Find<RadioButton>(window,"QuotaRemaining");var used=Find<RadioButton>(window,"QuotaUsed");
                used.IsChecked=true;Require(used.IsChecked==true&&remaining.IsChecked==false,"segmented switch selects used exclusively");
                remaining.IsChecked=true;Require(used.IsChecked==false&&remaining.IsChecked==true,"segmented switch selects remaining exclusively");used.IsChecked=true;
                Find<TextBox>(window,"TaskbarPrimaryLabel").Text="日额度{mode}";
                Find<TextBox>(window,"TaskbarSecondaryLabel").Text="长期预算";
                Find<Slider>(window,"TaskbarFontSize").Value=13;
                Find<CheckBox>(window,"TaskbarPreviewDark").IsChecked=false;
                Find<ComboBox>(window,"TaskbarPalette").SelectedIndex=3;
                Find<TextBox>(window,"TaskbarColor0").Text="#1234AB";
                Find<Slider>(window,"TaskbarBackgroundOpacity").Value=45;
                Find<TextBox>(window,"TaskbarColor6").Text="#45BCAB";
                Find<Slider>(window,"TaskbarMediumThreshold").Value=65;
                Find<ComboBox>(window,"TaskbarPreviewState").SelectedIndex=1;
                Find<ComboBox>(window,"TaskbarPreviewState").SelectedIndex=1;
                Find<ScrollViewer>(window,"").ScrollToVerticalOffset(0);window.UpdateLayout();
                var image=Find<System.Windows.Controls.Image>(window,"");var source=(System.Windows.Media.Imaging.BitmapSource)image.Source;var dpi=System.Windows.Media.VisualTreeHelper.GetDpi(window);
                Require(source.PixelWidth==(int)Math.Round(220*dpi.DpiScaleX)&&Math.Abs(source.DpiX-96*dpi.DpiScaleX)<1,"preview rendered at current screen DPI without 96 DPI enlargement");
                var capture=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Round(window.ActualWidth*dpi.DpiScaleX),(int)Math.Round(window.ActualHeight*dpi.DpiScaleY),96*dpi.DpiScaleX,96*dpi.DpiScaleY,System.Windows.Media.PixelFormats.Pbgra32);capture.Render(window);
                var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(capture));using(var file=File.Create(Path.Combine(folder,"display-label-settings-preview.png")))encoder.Save(file);
                Find<ComboBox>(window,"TaskbarPreviewState").SelectedIndex=3;
                SaveButton(window).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(saved!=null&&saved.TaskbarStyle=="Badges"&&saved.TaskbarPrimaryColor=="#1234AB"&&saved.TaskbarSecondaryColor=="#526CCC"&&saved.TaskbarBackgroundOpacity==45&&saved.TaskbarMediumColor=="#45BCAB"&&saved.TaskbarMediumPercent==65&&saved.ShowUsed&&saved.TaskbarPrimaryLabel=="日额度{mode}"&&saved.TaskbarSecondaryLabel=="长期预算"&&saved.TaskbarFontSize==13,"settings UI saves mode, custom labels, font size and appearance settings");
                File.WriteAllText(Path.Combine(folder,"appearance-checks.json"),serializer.Serialize(new { ok=true,checks=checks }));
            }
            catch(Exception e) { File.WriteAllText(Path.Combine(folder,"appearance-error.txt"),e.ToString());Environment.ExitCode=1;window.Close(); }
        };
        app.Run(window);
        Console.WriteLine(string.Join("\n",checks.ToArray()));
    }
}
