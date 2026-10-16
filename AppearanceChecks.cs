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
    sealed class TestStartupRegistration : StartupRegistration
    {
        public bool Value=true;
        public int Writes;
        public override bool Enabled { get { return Value; } }
        public override void SetEnabled(bool enabled) { Value=enabled;Writes++; }
    }
    static List<string> checks=new List<string>();
    static void Require(bool value,string label) { if(!value)throw new Exception(label);checks.Add("PASS "+label); }
    static bool HasColor(Bitmap image,Color color) { for(var y=0;y<image.Height;y++)for(var x=0;x<image.Width;x++) { var pixel=image.GetPixel(x,y);if(pixel.A>0&&pixel.R==color.R&&pixel.G==color.G&&pixel.B==color.B)return true; }return false; }
    static bool SameColor(System.Windows.Media.Color actual,Color expected) { return actual.R==expected.R&&actual.G==expected.G&&actual.B==expected.B; }
    static Rectangle ColorBounds(Bitmap image,Color color) { int left=image.Width,top=image.Height,right=-1,bottom=-1;for(var y=0;y<image.Height;y++)for(var x=0;x<image.Width;x++) { var pixel=image.GetPixel(x,y);if(pixel.A>0&&pixel.R==color.R&&pixel.G==color.G&&pixel.B==color.B) { left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y); } }return right<0?Rectangle.Empty:Rectangle.FromLTRB(left,top,right+1,bottom+1); }
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
        Require(old.TaskbarMeterHeight==42,"old settings keep default 42 pixel taskbar height");
        var heightSaved=serializer.Deserialize<Preferences>(serializer.Serialize(new Preferences { TaskbarMeterHeight=32 }));heightSaved.Normalize();Require(heightSaved.TaskbarMeterHeight==32,"configured area height survives save and load");
        var invalidHeight=new Preferences { TaskbarMeterHeight=100 };invalidHeight.Normalize();Require(invalidHeight.TaskbarMeterHeight==64,"area height clamps to supported maximum");
        Require(EmbeddedMeter.MeterPixelHeight(new Preferences { TaskbarMeterHeight=64 },1,48)==44&&EmbeddedMeter.MeterPixelHeight(new Preferences { TaskbarMeterHeight=24 },1,48)==24&&EmbeddedMeter.MeterPixelHeight(new Preferences { TaskbarMeterHeight=64 },1.25,60)==56,"area height respects taskbar bounds at normal and high DPI");
        foreach(var styleName in new[] { "Rounded","Segments","Cards","Dots","Gradient","Rings","Badges" })foreach(var areaHeight in new[] { 24,32,42,64 })using(var bitmap=new Bitmap(220,areaHeight))using(var g=Graphics.FromImage(bitmap)) { var sized=new Preferences { TaskbarStyle=styleName,TaskbarMeterHeight=areaHeight,TaskbarVerticalSpacing=8 };MeterRenderer.Draw(g,bitmap.Width,bitmap.Height,1,sample,sized,false,true); }
        Require(old.TaskbarHorizontalSpacing==10&&old.TaskbarVerticalSpacing==0,"legacy settings keep original quota spacing");
        var spacingSaved=serializer.Deserialize<Preferences>(serializer.Serialize(new Preferences { TaskbarHorizontalSpacing=18,TaskbarVerticalSpacing=6 }));spacingSaved.Normalize();Require(spacingSaved.TaskbarHorizontalSpacing==18&&spacingSaved.TaskbarVerticalSpacing==6,"both spacing values survive save and load");
        var badSpacing=new Preferences { TaskbarHorizontalSpacing=-5,TaskbarVerticalSpacing=100 };badSpacing.Normalize();Require(badSpacing.TaskbarHorizontalSpacing==0&&badSpacing.TaskbarVerticalSpacing==8,"spacing is bounded for the available taskbar area");
        foreach(var styleName in new[] { "Rounded","Segments","Cards","Dots","Gradient","Rings","Badges" })foreach(var factor in new[] { 1.0,1.25,1.5,2.0 })foreach(var spacing in new[] { 0,4,8 })using(var bitmap=new Bitmap((int)(220*factor),(int)(42*factor)))using(var g=Graphics.FromImage(bitmap))
        {
            var spaced=new Preferences { TaskbarStyle=styleName,TaskbarHorizontalSpacing=spacing*4,TaskbarVerticalSpacing=spacing };var regions=MeterRenderer.LayoutRegions(bitmap.Width,bitmap.Height,factor,spaced);
            Require(styleName=="Rings"?regions[1].Left-regions[0].Right==(int)Math.Round(spacing*4*factor):regions[1].Top-regions[0].Bottom==(int)Math.Round(spacing*factor),styleName+" uses configured spacing at "+factor+" scale, gap="+spacing);
            MeterRenderer.Draw(g,bitmap.Width,bitmap.Height,factor,sample,spaced,false,true);MeterRenderer.DrawPreviewBounds(g,bitmap.Width,bitmap.Height,factor,spaced);
        }
        using(var sheet=new Bitmap(820,760))using(var graphics=Graphics.FromImage(sheet))using(var title=new Font(UI.TextFontFamily,12))
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
        var resetDefault=serializer.Deserialize<Preferences>("{}");resetDefault.Normalize();Require(resetDefault.TaskbarPrimaryResetStyle=="Clock"&&resetDefault.TaskbarWeeklyResetStyle=="Weekday","old reset configuration keeps clock and weekday");
        var resetInvalid=new Preferences { TaskbarPrimaryResetStyle="bad",TaskbarWeeklyResetStyle=null };resetInvalid.Normalize();Require(resetInvalid.TaskbarPrimaryResetStyle=="Clock"&&resetInvalid.TaskbarWeeklyResetStyle=="Weekday","invalid reset styles fall back safely");
        var epoch=new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc);var reference=new DateTime(2026,10,10,0,0,0,DateTimeKind.Utc);
        var resetLimit=new Limit { Reset=(long)(reference.AddDays(4).AddHours(23)-epoch).TotalSeconds };
        Require(resetLimit.ResetDisplay("Countdown",reference)=="4天23时","weekly countdown uses days and hours");resetLimit.Reset=(long)(reference.AddHours(2).AddMinutes(3)-epoch).TotalSeconds;
        Require(resetLimit.ResetDisplay("Countdown",reference)=="2时3分","short countdown uses hours and minutes");resetLimit.Reset=(long)(reference.AddSeconds(20)-epoch).TotalSeconds;Require(resetLimit.ResetDisplay("Countdown",reference)=="不足1分","sub-minute countdown remains visible");resetLimit.Reset=(long)(reference.AddSeconds(-1)-epoch).TotalSeconds;Require(resetLimit.ResetDisplay("Countdown",reference)=="待更新","expired countdown waits for server data");Require(new Limit().ResetDisplay("Countdown")=="—"&&resetLimit.ResetDisplay("Hidden")=="","unknown and hidden reset states");
        var localReset=new DateTime(2026,10,15,14,27,0,DateTimeKind.Local);resetLimit.Reset=(long)(localReset.ToUniversalTime()-epoch).TotalSeconds;
        Require(resetLimit.ResetDisplay("Clock")=="14:27"&&resetLimit.ResetDisplay("Weekday")=="周四 14:27"&&resetLimit.ResetDisplay("DateTime")=="10-15 14:27"&&resetLimit.ResetDisplay("Date")=="10-15","absolute reset formats use local time");
        var resetStyles=new[] { "Weekday","Clock","Countdown","DateTime","Date","Hidden" };
        using(var image=new Bitmap(820,530))using(var graphics=Graphics.FromImage(image))using(var title=new Font(UI.TextFontFamily,12))
        {
            graphics.Clear(Color.FromArgb(247,249,252));
            for(var row=0;row<resetStyles.Length;row++)for(var col=0;col<2;col++)
            {
                var x=20+col*405;var y=10+row*86;graphics.DrawString(resetStyles[row]+(col==0?" / Light":" / Dark"),title,Brushes.DimGray,x,y);
                var p=new Preferences { TaskbarStyle="Rings",TaskbarWeeklyResetStyle=resetStyles[row],TaskbarBackgroundOpacity=20 };
                using(var tile=new Bitmap(360,42))using(var g=Graphics.FromImage(tile)) { g.Clear(col==0?Color.FromArgb(232,235,241):Color.FromArgb(32,35,42));MeterRenderer.Draw(g,260,42,1,sample,p,false,col==1);graphics.DrawImageUnscaled(tile,x,y+26); }
            }
            image.Save(Path.Combine(folder,"horizontal-rings-reset-preview.png"));
        }
        foreach(var style in resetStyles)foreach(var meterStyle in new[] { "Rounded","Segments","Cards","Dots","Gradient","Rings","Badges" })foreach(var factor in new[] { 1.0,1.25,1.5,2.0 })using(var image=new Bitmap((int)Math.Round(200*factor),(int)Math.Round(42*factor)))using(var g=Graphics.FromImage(image))
        { var p=new Preferences { TaskbarStyle=meterStyle,TaskbarPrimaryResetStyle=style,TaskbarWeeklyResetStyle=style };MeterRenderer.Draw(g,image.Width,image.Height,factor,sample,p,false,true);MeterRenderer.Draw(g,image.Width,image.Height,factor,null,p,true,true); }
        Require(true,"six reset formats render in all seven styles and four DPI scales");
        using(var image=new Bitmap(260,42))using(var g=Graphics.FromImage(image))
        {
            var p=new Preferences { TaskbarStyle="Rings",TaskbarPrimaryColor="#FF3366",TaskbarSecondaryColor="#33AAFF" };MeterRenderer.Draw(g,260,42,1,sample,p,false,true);
            var leftColor=false;var rightColor=false;
            for(var y=0;y<image.Height;y++)for(var x=0;x<image.Width;x++) { var pixel=image.GetPixel(x,y);if(x<130&&pixel.R==255&&pixel.G==51&&pixel.B==102)leftColor=true;if(x>=130&&pixel.R==51&&pixel.G==170&&pixel.B==255)rightColor=true; }
            Require(leftColor&&rightColor,"ring quotas render in separate left and right panels");
        }
        var oldNumbers=serializer.Deserialize<Preferences>("{}");Require(oldNumbers.TaskbarShowPercentage&&oldNumbers.TaskbarShowPercentSign&&oldNumbers.TaskbarRingThickness==3,"legacy settings retain percentage and default ring thickness");
        var numeric=new Preferences();Require(MeterRenderer.PercentageCaption(new Limit { Used=24 },numeric)=="76%","percentage includes optional sign");numeric.TaskbarShowPercentSign=false;Require(MeterRenderer.PercentageCaption(new Limit { Used=24 },numeric)=="76","percentage sign can be hidden");numeric.TaskbarShowPercentage=false;Require(MeterRenderer.PercentageCaption(new Limit { Used=24 },numeric)=="","percentage number can be hidden");
        foreach(var meterStyle in new[] { "Rounded","Segments","Cards","Dots","Gradient","Rings","Badges" })foreach(var factor in new[] { 1.0,1.25,1.5,2.0 })foreach(var thickness in new[] { 1,3,6 })using(var bitmap=new Bitmap((int)(220*factor),(int)(42*factor)))using(var g=Graphics.FromImage(bitmap))
        { numeric.TaskbarStyle=meterStyle;numeric.TaskbarRingThickness=thickness;MeterRenderer.Draw(g,bitmap.Width,bitmap.Height,factor,sample,numeric,false,true); }
        Require(true,"hidden percentages and ring thickness bounds render across seven styles and four DPI scales");
        Require(EmbeddedMeter.ClampTaskbarPosition(8,-4,1,1500)==4&&EmbeddedMeter.ClampTaskbarPosition(8,100,1.25,1500)==133&&EmbeddedMeter.ClampTaskbarPosition(1000,-100,1,1500)==900,"manual offset moves either anchor freely including towards the left");
        Require(EmbeddedMeter.ClampTaskbarPosition(8,-100,1,1500)==0&&EmbeddedMeter.ClampTaskbarPosition(1400,200,1,1500)==1500,"manual placement remains inside taskbar bounds");
        foreach(var fontName in Preferences.FontChoices)foreach(var styleName in new[] { "Rounded","Rings" })using(var bitmap=new Bitmap(220,42))using(var g=Graphics.FromImage(bitmap)) { var fontPreferences=new Preferences { TaskbarTextFont=fontName,TaskbarNumberFont=fontName,TaskbarStyle=styleName };MeterRenderer.Draw(g,220,42,1.25,sample,fontPreferences,false,true); }
        Require(true,"taskbar font choices render labels and numbers");
        var range=new ThresholdRange { Medium=65,Low=25 };range.Low=90;Require(range.Low==65,"low handle cannot cross medium handle");range.Medium=10;Require(range.Medium==65,"medium handle cannot cross low handle");range.Low=-1;range.Medium=101;Require(range.Low==0&&range.Medium==100,"threshold handles clamp to zero and one hundred");
        foreach(var fontName in Preferences.FontChoices)foreach(var caption in new[] { "1","4","44","49","55","99.9","100%" })foreach(var factor in new[] { 1.0,1.25,2.0 })using(var bitmap=new Bitmap((int)(64*factor),(int)(64*factor)))using(var g=Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;var circle=new RectangleF((float)(12*factor),(float)(12*factor),(float)(40*factor),(float)(40*factor));MeterRenderer.DrawCenteredNumber(g,circle,caption,fontName,(float)(18*factor),(float)(9*factor),Color.White);
            int minX=bitmap.Width,maxX=-1,minY=bitmap.Height,maxY=-1;for(var y=0;y<bitmap.Height;y++)for(var x=0;x<bitmap.Width;x++)if(bitmap.GetPixel(x,y).A>10) { minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y); }
            Require(maxX>=minX&&Math.Abs((minX+maxX+1)/2.0-32*factor)<=1&&Math.Abs((minY+maxY+1)/2.0-32*factor)<=1,"ring glyph bounds centered: "+fontName+" "+caption+" at "+factor);
        }
        using(var bitmap=new Bitmap(240,240))using(var g=Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            MeterRenderer.DrawCenteredNumber(g,new RectangleF(0,0,240,240),"4","Bahnschrift",180,0,Color.White);
            int minX=240,maxX=-1,minY=240,maxY=-1;
            for(var y=0;y<240;y++)for(var x=0;x<240;x++)if(bitmap.GetPixel(x,y).A>10) { minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y); }
            var crossingX=minX+(int)((maxX-minX)*.73);var crossingY=minY+(int)((maxY-minY)*.76);
            Require(bitmap.GetPixel(crossingX,crossingY).A>240,"Bahnschrift four overlapping strokes remain filled");
        }
        foreach(var fontName in Preferences.ChineseFontChoices)foreach(var factor in new[] { 1.0,1.25,2.0 })foreach(var caption in new[] { "日额度余","周六","14:28" })using(var bitmap=new Bitmap((int)(180*factor),(int)(32*factor)))using(var g=Graphics.FromImage(bitmap))using(var font=new Font(fontName,(float)(12*factor),System.Drawing.FontStyle.Regular,GraphicsUnit.Pixel))
        {
            g.Clear(Color.Transparent);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;MeterRenderer.DrawAlignedRingText(g,caption,font,new RectangleF((float)(10*factor),0,(float)(160*factor),(float)(32*factor)),Color.White);
            int left=bitmap.Width;for(var y=0;y<bitmap.Height;y++)for(var x=0;x<bitmap.Width;x++)if(bitmap.GetPixel(x,y).A>10)left=Math.Min(left,x);
            Require(Math.Abs(left-10*factor)<=1,"ring text visible left edge aligned: "+fontName+" "+caption+" at "+factor);
        }
        var legacyQuotas=serializer.Deserialize<Preferences>("{\"WarningPercent\":35,\"TaskbarMediumPercent\":70,\"TaskbarWarningColor\":\"#112233\"}");legacyQuotas.Normalize();Require(legacyQuotas.LowThreshold(true)==35&&legacyQuotas.MediumThreshold(true)==70&&legacyQuotas.LowColor(true)=="#112233","old shared quota settings migrate to both groups");
        var separate=new Preferences { WarningPercent=20,TaskbarMediumPercent=50,WeeklyWarningPercent=40,WeeklyMediumPercent=80,TaskbarWarningColor="#112233",WeeklyWarningColor="#AABBCC" };separate.Normalize();var sameQuota=new Limit { Used=70 };Require(MeterRenderer.QuotaColor(sameQuota,separate,false,true,false)!=MeterRenderer.QuotaColor(sameQuota,separate,false,true,true),"same remaining amount uses independent quota thresholds and colors");var separateSaved=serializer.Deserialize<Preferences>(serializer.Serialize(separate));Require(separateSaved.LowThreshold(false)==20&&separateSaved.LowThreshold(true)==40&&separateSaved.LowColor(true)=="#AABBCC","weekly thresholds and colors survive serialization");
        foreach(var entry in new[] { new[] { 1,82,81 },new[] { 2,51,49 },new[] { 3,13,11 } }) { var preview=SettingsWindow.PreviewSnapshot(entry[0]);Require(preview.Primary.Remaining==entry[1]&&preview.Secondary.Remaining==entry[2],"fixed quota preview sample "+entry[0]); }
        var hoverSnapshot=SettingsWindow.PreviewSnapshot(1);hoverSnapshot.Received=DateTime.Now;var hover=TaskbarDisplay.From(hoverSnapshot,new Preferences(),false,false).Description;Require(hover.Split('\n').Length==4&&hover.IndexOf("82%")>=0&&hover.IndexOf("82%")==hover.LastIndexOf("82%"),"hover describes each quota once in four compact lines");
        foreach(var styleName in new[] { "Rounded","Segments","Cards","Dots","Gradient","Rings","Badges" })using(var before=new Bitmap(220,42))using(var after=new Bitmap(220,42))
        {
            var layout=new Preferences { TaskbarStyle=styleName,TaskbarPrimaryColor="#FF3366",TaskbarSecondaryColor="#33AAFF",TaskbarPrimaryLabel="短",TaskbarSecondaryLabel="周",TaskbarPrimaryResetStyle="Clock",TaskbarWeeklyResetStyle="Clock" };var layoutSample=SettingsWindow.PreviewSnapshot(1);
            using(var g=Graphics.FromImage(before))MeterRenderer.Draw(g,220,42,1,layoutSample,layout,false,true);layout.TaskbarPrimaryLabel="很长的自定义额度名称";layout.TaskbarSecondaryLabel="另一组很长的自定义名称";layout.TaskbarPrimaryResetStyle="DateTime";layout.TaskbarWeeklyResetStyle="DateTime";using(var g=Graphics.FromImage(after))MeterRenderer.Draw(g,220,42,1,layoutSample,layout,false,true);
            Require(ColorBounds(before,Color.FromArgb(255,51,102))==ColorBounds(after,Color.FromArgb(255,51,102))&&ColorBounds(before,Color.FromArgb(51,170,255))==ColorBounds(after,Color.FromArgb(51,170,255)),styleName+" quota graphics keep positions when labels and times change");
        }
        var app=new Application();app.Resources.MergedDictionaries.Add(Styles.Create());
        var startup=new TestStartupRegistration();
        var window=new SettingsWindow(new Preferences(),startup);Preferences saved=null;window.Saved+=delegate(Preferences p) { saved=p; };
        window.Loaded+=async delegate
        {
            try
            {
                Require(Find<TextBlock>(window,"AppVersionLabel").Text==AppVersion.Display,"settings displays application version");
                Require(StartupRegistration.CommandFor(@"C:\测试目录\Codex Widget.exe")=="\"C:\\测试目录\\Codex Widget.exe\"","startup command quotes executable paths with spaces and Chinese characters");
                var autoStart=Find<CheckBox>(window,"StartWithWindows");
                Require(autoStart.IsChecked==true&&startup.Writes==0,"startup switch reads system state without changing registration");
                autoStart.IsChecked=false;Require(startup.Value&&startup.Writes==0,"changing startup checkbox waits for apply or save");
                Find<Button>(window,"ApplySettings").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(!startup.Value&&startup.Writes==1&&window.IsVisible,"apply disables startup and keeps settings open");
                autoStart.IsChecked=true;Find<Button>(window,"ApplySettings").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(startup.Value&&startup.Writes==2,"apply enables startup registration");
                autoStart.IsChecked=false;Find<Button>(window,"ApplySettings").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                for(var mode=0;mode<2;mode++)for(var preset=1;preset<SettingsWindow.PaletteNames.Length;preset++)
                {
                    Find<RadioButton>(window,mode==0?"TaskbarPreviewLight":"TaskbarPreviewDark").IsChecked=true;Find<ComboBox>(window,"TaskbarPalette").SelectedIndex=preset;Find<Button>(window,"ApplySettings").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    var primary=Find<ThresholdRange>(window,"TaskbarThresholdRange");var weekly=Find<ThresholdRange>(window,"WeeklyThresholdRange");var dark=mode==1;
                    Require(SameColor(primary.LowColor,MeterRenderer.SettingColor(saved,dark,4))&&SameColor(primary.MediumColor,MeterRenderer.SettingColor(saved,dark,6))&&SameColor(primary.NormalColor,MeterRenderer.SettingColor(saved,dark,0))&&SameColor(weekly.LowColor,MeterRenderer.SettingColor(saved,dark,7))&&SameColor(weekly.MediumColor,MeterRenderer.SettingColor(saved,dark,8))&&SameColor(weekly.NormalColor,MeterRenderer.SettingColor(saved,dark,1)),"preset "+preset+" threshold rails match applied quota colors, dark="+dark);
                    for(var colorIndex=0;colorIndex<9;colorIndex++) { var chip=(Border)Find<Button>(window,"TaskbarColorPicker"+colorIndex).Content;Require(SameColor(((System.Windows.Media.SolidColorBrush)chip.Background).Color,MeterRenderer.SettingColor(saved,dark,colorIndex)),"preset swatch matches rendered color "+preset+"/"+mode+"/"+colorIndex); }
                }
                var previewWidth=Find<TextBox>(window,"TaskbarMeterWidth");var previewGraphic=Find<System.Windows.Controls.Image>(window,"");var testDpi=System.Windows.Media.VisualTreeHelper.GetDpi(window);
                previewWidth.Text="380";window.UpdateLayout();Require(((System.Windows.Media.Imaging.BitmapSource)previewGraphic.Source).PixelWidth==(int)Math.Round(380*testDpi.DpiScaleX)&&Math.Abs(previewGraphic.ActualWidth-380)<.1,"width immediately expands preview to full 380 pixel area; raster="+((System.Windows.Media.Imaging.BitmapSource)previewGraphic.Source).PixelWidth+", actual="+previewGraphic.ActualWidth+", dpi="+testDpi.DpiScaleX);
                Find<CheckBox>(window,"TaskbarPreviewBounds").IsChecked=true;var guided=(System.Windows.Media.Imaging.BitmapSource)previewGraphic.Source;var guidePixels=new byte[guided.PixelWidth*guided.PixelHeight*4];guided.CopyPixels(guidePixels,guided.PixelWidth*4,0);Find<CheckBox>(window,"TaskbarPreviewBounds").IsChecked=false;var plain=(System.Windows.Media.Imaging.BitmapSource)previewGraphic.Source;var plainPixels=new byte[guidePixels.Length];plain.CopyPixels(plainPixels,plain.PixelWidth*4,0);var guideDiff=false;for(var pixel=0;pixel<guidePixels.Length;pixel++)if(guidePixels[pixel]!=plainPixels[pixel]) { guideDiff=true;break; }Require(guideDiff,"preview bounds toggle changes raster only when enabled");
                previewWidth.Text="200";Require(((System.Windows.Media.Imaging.BitmapSource)previewGraphic.Source).PixelWidth==(int)Math.Round(200*testDpi.DpiScaleX),"width immediately contracts preview to 200 pixel area");previewWidth.Text="220";
                var previewHeight=Find<TextBox>(window,"TaskbarMeterHeight");previewHeight.Text="24";window.UpdateLayout();var fixedTaskbarHeight=EmbeddedMeter.PreviewTaskbarPixelHeight(testDpi.DpiScaleX);var previewOuterHeight=previewGraphic.ActualHeight;var controlsTop=Find<ComboBox>(window,"TaskbarPreviewState").TranslatePoint(new System.Windows.Point(),window).Y;
                var smallSource=(System.Windows.Media.Imaging.BitmapSource)previewGraphic.Source;Require(smallSource.PixelHeight==fixedTaskbarHeight&&Math.Abs(previewOuterHeight-fixedTaskbarHeight/testDpi.DpiScaleX)<.1,"preview outer height matches current system taskbar height");
                var smallPixels=new byte[smallSource.PixelWidth*smallSource.PixelHeight*4];smallSource.CopyPixels(smallPixels,smallSource.PixelWidth*4,0);bool blankTop=true;for(var pixel=3;pixel<smallSource.PixelWidth*4;pixel+=4)if(smallPixels[pixel]!=0)blankTop=false;Require(blankTop,"smaller area is centered inside fixed preview with top margin");
                previewHeight.Text="64";window.UpdateLayout();var effectiveHeight=EmbeddedMeter.PreviewPixelHeight(new Preferences { TaskbarMeterHeight=64 },testDpi.DpiScaleX);Require(((System.Windows.Media.Imaging.BitmapSource)previewGraphic.Source).PixelHeight==fixedTaskbarHeight&&effectiveHeight<=fixedTaskbarHeight&&Math.Abs(previewGraphic.ActualHeight-previewOuterHeight)<.1&&Math.Abs(Find<ComboBox>(window,"TaskbarPreviewState").TranslatePoint(new System.Windows.Point(),window).Y-controlsTop)<.1,"changing area height keeps preview and controls fixed at system taskbar height");Find<Button>(window,"ApplySettings").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Require(saved.TaskbarMeterHeight==64,"apply saves requested height while preview shows effective height");previewHeight.Text="42";
                Find<Slider>(window,"TaskbarHorizontalSpacing").Value=14;Find<Slider>(window,"TaskbarVerticalSpacing").Value=4;Find<Button>(window,"ApplySettings").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Require(saved.TaskbarHorizontalSpacing==14&&saved.TaskbarVerticalSpacing==4,"apply persists both control spacing settings");
                Find<ComboBox>(window,"TaskbarStyle").SelectedIndex=0;Require(!Find<Slider>(window,"TaskbarHorizontalSpacing").IsEnabled&&Find<Slider>(window,"TaskbarVerticalSpacing").IsEnabled,"stacked layouts enable vertical spacing only");Find<ComboBox>(window,"TaskbarStyle").SelectedIndex=5;Require(Find<Slider>(window,"TaskbarHorizontalSpacing").IsEnabled&&!Find<Slider>(window,"TaskbarVerticalSpacing").IsEnabled,"ring layout enables horizontal spacing only");
                Require(Find<ComboBox>(window,"TaskbarStyle").Items.Count==7,"settings offers all seven styles");
                Find<ComboBox>(window,"TaskbarStyle").SelectedIndex=5;
                Find<ComboBox>(window,"TaskbarPrimaryResetStyle").SelectedIndex=5;
                Find<ComboBox>(window,"TaskbarWeeklyResetStyle").SelectedIndex=2;
                var remaining=Find<RadioButton>(window,"QuotaRemaining");var used=Find<RadioButton>(window,"QuotaUsed");
                used.IsChecked=true;Require(used.IsChecked==true&&remaining.IsChecked==false,"segmented switch selects used exclusively");
                remaining.IsChecked=true;Require(used.IsChecked==false&&remaining.IsChecked==true,"segmented switch selects remaining exclusively");used.IsChecked=true;
                Find<TextBox>(window,"TaskbarPrimaryLabel").Text="日额度{mode}";
                Find<TextBox>(window,"TaskbarSecondaryLabel").Text="长期预算";
                Find<ComboBox>(window,"TaskbarTextFont").SelectedIndex=0;Find<ComboBox>(window,"TaskbarNumberFont").SelectedIndex=3;Find<ComboBox>(window,"TaskbarChineseFont").SelectedIndex=2;
                Find<Slider>(window,"TaskbarFontSize").Value=13;Find<Slider>(window,"TaskbarPercentageFontSize").Value=16;
                Find<RadioButton>(window,"TaskbarPreviewLight").IsChecked=true;
                Find<ComboBox>(window,"TaskbarPalette").SelectedIndex=3;
                Find<TextBox>(window,"TaskbarColor0").Text="#1234AB";Find<TextBox>(window,"TaskbarColor4").Text="#EBB048";
                Find<Slider>(window,"TaskbarBackgroundOpacity").Value=45;
                Find<TextBox>(window,"TaskbarColor6").Text="#45BCAB";
                Find<ThresholdRange>(window,"TaskbarThresholdRange").Medium=65;Find<ThresholdRange>(window,"TaskbarThresholdRange").Low=25;
                Find<ComboBox>(window,"TaskbarPreviewState").SelectedIndex=2;
                Find<ComboBox>(window,"TaskbarPreviewState").SelectedIndex=2;
                var percent=Find<RadioButton>(window,"TaskbarNumberMode1");var hidden=Find<RadioButton>(window,"TaskbarNumberMode2");
                hidden.IsChecked=true;Require(percent.IsChecked==false,"number modes are exclusive");percent.IsChecked=true;
                Find<Slider>(window,"TaskbarRingThickness").Value=4;
                var primaryRange=Find<ThresholdRange>(window,"TaskbarThresholdRange");var weeklyRange=Find<ThresholdRange>(window,"WeeklyThresholdRange");
                Find<Button>(window,"CopyPrimaryToWeekly").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Require(weeklyRange.Low==primaryRange.Low&&weeklyRange.Medium==primaryRange.Medium&&Find<TextBox>(window,"TaskbarColor7").Text==Find<TextBox>(window,"TaskbarColor4").Text&&Find<TextBox>(window,"TaskbarColor8").Text==Find<TextBox>(window,"TaskbarColor6").Text&&Find<TextBox>(window,"TaskbarColor1").Text==Find<TextBox>(window,"TaskbarColor0").Text,"copy to weekly transfers thresholds and all three colors");
                weeklyRange.Low=10;weeklyRange.Medium=45;Find<TextBox>(window,"TaskbarColor7").Text="#AA4400";Require(primaryRange.Low==25&&primaryRange.Medium==65&&Find<TextBox>(window,"TaskbarColor4").Text=="#EBB048","groups remain independent after one-time copy");
                Find<Button>(window,"CopyWeeklyToPrimary").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Require(primaryRange.Low==10&&primaryRange.Medium==45&&Find<TextBox>(window,"TaskbarColor4").Text=="#AA4400","reverse copy transfers weekly settings to primary");
                primaryRange.Medium=65;primaryRange.Low=25;Find<TextBox>(window,"TaskbarColor0").Text="#1234AB";Find<TextBox>(window,"TaskbarColor1").Text="#526CCC";Find<TextBox>(window,"TaskbarColor4").Text="#EBB048";Find<TextBox>(window,"TaskbarColor6").Text="#45BCAB";
                var apply=Find<Button>(window,"ApplySettings");apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(window.IsVisible&&saved!=null&&saved.TaskbarShowPercentage&&!saved.TaskbarShowPercentSign&&saved.TaskbarNumberFont=="Consolas"&&saved.TaskbarTextFont=="Microsoft YaHei UI"&&saved.TaskbarChineseFont=="SimSun"&&saved.WeeklyWarningPercent==10&&saved.WeeklyMediumPercent==45&&saved.WeeklyWarningColor=="#AA4400"&&saved.WarningPercent==25&&saved.TaskbarMediumPercent==65&&saved.TaskbarRingThickness==4&&saved.TaskbarPercentageFontSize==16,"apply saves display options without closing settings");
                var applied=saved;hidden.IsChecked=true;apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(window.IsVisible&&!saved.TaskbarShowPercentage&&applied.TaskbarShowPercentage,"repeated apply keeps independent saved snapshots");percent.IsChecked=true;
                window.UpdateLayout();
                var positionHeight=Find<TextBox>(window,"TaskbarMeterHeight");
                var positionSide=Find<RadioButton>(window,"TaskbarSide0");var positionWidth=Find<TextBox>(window,"TaskbarMeterWidth");var positionOffset=Find<TextBox>(window,"TaskbarOffset");
                Require(Math.Abs(positionHeight.TranslatePoint(new System.Windows.Point(),window).Y-positionWidth.TranslatePoint(new System.Windows.Point(),window).Y)<.1&&Math.Abs(positionWidth.TranslatePoint(new System.Windows.Point(),window).Y-positionOffset.TranslatePoint(new System.Windows.Point(),window).Y)<.1&&Math.Abs(positionSide.TranslatePoint(new System.Windows.Point(),window).Y-positionWidth.TranslatePoint(new System.Windows.Point(),window).Y)<6,"taskbar side width height and offset share one row");
                var paletteRow=Find<Grid>(window,"TaskbarPaletteRow");Require(paletteRow.ColumnDefinitions.Count==2&&Find<ComboBox>(window,"TaskbarPalette").Items.Count==10,"palette label and ten choices use horizontal layout");
                var scroll=Find<ScrollViewer>(window,"");var previewImage=Find<System.Windows.Controls.Image>(window,"");var previewState=Find<ComboBox>(window,"TaskbarPreviewState");
                var previewTop=previewImage.TranslatePoint(new System.Windows.Point(),window).Y;var stateTop=previewState.TranslatePoint(new System.Windows.Point(),window).Y;
                scroll.ScrollToBottom();window.UpdateLayout();
                Require(scroll.VerticalOffset>0&&Math.Abs(previewImage.TranslatePoint(new System.Windows.Point(),window).Y-previewTop)<.1&&Math.Abs(previewState.TranslatePoint(new System.Windows.Point(),window).Y-stateTop)<.1,"preview and state stay fixed while settings scroll to bottom");
                Require(scroll.ViewportHeight>100&&previewTop<scroll.TranslatePoint(new System.Windows.Point(),window).Y,"fixed preview leaves usable settings viewport");
                if(Environment.GetEnvironmentVariable("WIDGET_CHECK_DETECTION")=="1")
                {
                    var detect=Find<Button>(window,"DetectCodexPath");detect.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    var deadline=DateTime.UtcNow.AddSeconds(60);while(!detect.IsEnabled&&DateTime.UtcNow<deadline)await System.Threading.Tasks.Task.Delay(100);
                    Require(detect.IsEnabled&&File.Exists(Find<TextBox>(window,"CodexPath").Text),"manual auto-detection fills a valid Codex executable path");
                }
                scroll.ScrollToTop();window.UpdateLayout();
                var image=Find<System.Windows.Controls.Image>(window,"");var source=(System.Windows.Media.Imaging.BitmapSource)image.Source;var dpi=System.Windows.Media.VisualTreeHelper.GetDpi(window);
                Require(source.PixelWidth==(int)Math.Round(220*dpi.DpiScaleX)&&Math.Abs(source.DpiX-96*dpi.DpiScaleX)<1,"preview rendered at current screen DPI without 96 DPI enlargement");
                var capture=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Round(window.ActualWidth*dpi.DpiScaleX),(int)Math.Round(window.ActualHeight*dpi.DpiScaleY),96*dpi.DpiScaleX,96*dpi.DpiScaleY,System.Windows.Media.PixelFormats.Pbgra32);capture.Render(window);
                var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(capture));using(var file=File.Create(Path.Combine(folder,"display-label-settings-preview.png")))encoder.Save(file);
                Find<CheckBox>(window,"TaskbarPreviewBounds").IsChecked=true;
                var colorField=Find<Grid>(window,"TaskbarPlacementRow");scroll.ScrollToVerticalOffset(colorField.TranslatePoint(new System.Windows.Point(),scroll).Y+scroll.VerticalOffset);window.UpdateLayout();capture.Clear();capture.Render(window);encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(capture));using(var file=File.Create(Path.Combine(folder,"compact-colors-preview.png")))encoder.Save(file);
                scroll.ScrollToBottom();window.UpdateLayout();capture.Clear();capture.Render(window);encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(capture));using(var file=File.Create(Path.Combine(folder,"compact-parameters-preview.png")))encoder.Save(file);
                Find<ComboBox>(window,"TaskbarPreviewState").SelectedIndex=4;
                SaveButton(window).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(saved!=null&&saved.TaskbarStyle=="Rings"&&saved.TaskbarPrimaryColor=="#1234AB"&&saved.TaskbarSecondaryColor=="#526CCC"&&saved.TaskbarBackgroundOpacity==45&&saved.TaskbarMediumColor=="#45BCAB"&&saved.TaskbarMediumPercent==65&&saved.ShowUsed&&saved.TaskbarPrimaryLabel=="日额度{mode}"&&saved.TaskbarSecondaryLabel=="长期预算"&&saved.TaskbarFontSize==13&&saved.TaskbarPrimaryResetStyle=="Hidden"&&saved.TaskbarWeeklyResetStyle=="Countdown","settings UI saves independent reset styles and horizontal ring selection");
                File.WriteAllText(Path.Combine(folder,"appearance-checks.json"),serializer.Serialize(new { ok=true,checks=checks }));
            }
            catch(Exception e) { File.WriteAllText(Path.Combine(folder,"appearance-error.txt"),e.ToString());Environment.ExitCode=1;window.Close(); }
        };
        app.Run(window);
        Console.WriteLine(string.Join("\n",checks.ToArray()));
    }
}
