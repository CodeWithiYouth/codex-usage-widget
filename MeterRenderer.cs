using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace WindowsUsageWidget
{
    public static class MeterRenderer
    {
        public static Color ColorValue(string value,Color fallback)
        {
            int rgb;
            if(!Preferences.IsColor(value)||value=="Auto"||!int.TryParse(value.Substring(1),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out rgb))return fallback;
            return Color.FromArgb((rgb>>16)&255,(rgb>>8)&255,rgb&255);
        }
        public static void Draw(Graphics g,int width,int height,double scale,Snapshot snapshot,Preferences p,bool stale,bool dark)
        {
            g.SmoothingMode=SmoothingMode.AntiAlias;
            g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            var text=SettingColor(p,dark,2);
            var background=SettingColor(p,dark,3);
            var alpha=(int)(255*p.TaskbarBackgroundOpacity/100.0);var regions=LayoutRegions(width,height,scale,p);
            if(p.TaskbarStyle!="Cards"&&p.TaskbarStyle!="Rings"&&alpha>0)FillRound(g,new RectangleF(1,1,width-2,height-2),(float)(8*scale),Color.FromArgb(alpha,background));
            if(p.TaskbarStyle=="Rings")
            {
                var primary=snapshot==null?null:snapshot.Primary;var secondary=snapshot==null?null:snapshot.Secondary;
                var first=regions[0];var second=regions[1];var total=width;var start=0;
                if(alpha>0)FillRound(g,new RectangleF(start,1,total,height-2),(float)(8*scale),Color.FromArgb(alpha,background));
                DrawRingPanel(g,first.Left,first.Width,height,scale,primary,p,stale,text,dark,false);
                DrawRingPanel(g,second.Left,second.Width,height,scale,secondary,p,stale,text,dark,true);

                return;
            }
            var physicalFont=(float)Math.Min(p.TaskbarFontSize*scale,Math.Max(7,(Math.Min(regions[0].Height,regions[1].Height)-1)*.9));
            using(var font=new Font(TextFontFor(p,p.TaskbarLabel(false)+p.TaskbarLabel(true)),physicalFont,FontStyle.Regular,GraphicsUnit.Pixel))
            {
                var resets=new[] { ResetText(snapshot==null?null:snapshot.Primary,p,false,true),ResetText(snapshot==null?null:snapshot.Secondary,p,true,true) };
                DrawRow(g,font,width,p.TaskbarLabel(false),snapshot==null?null:snapshot.Primary,regions[0].Top,regions[0].Height,scale,p,stale,text,background,alpha,dark,false,resets);
                DrawRow(g,font,width,p.TaskbarLabel(true),snapshot==null?null:snapshot.Secondary,regions[1].Top,regions[1].Height,scale,p,stale,text,background,alpha,dark,true,resets);
            }
        }
        public static Rectangle[] LayoutRegions(int width,int height,double scale,Preferences p)
        {
            if(p.TaskbarStyle=="Rings")
            {
                var gap=Math.Min(Math.Max(0,width-2),(int)Math.Round(p.TaskbarHorizontalSpacing*scale));var first=(width-gap)/2;
                return new[] { new Rectangle(0,0,first,height),new Rectangle(first+gap,0,width-first-gap,height) };
            }
            var verticalGap=Math.Min(Math.Max(0,height-2),(int)Math.Round(p.TaskbarVerticalSpacing*scale));var row=(height-verticalGap)/2;
            return new[] { new Rectangle(0,0,width,row),new Rectangle(0,row+verticalGap,width,height-row-verticalGap) };
        }
        public static void DrawPreviewBounds(Graphics g,int width,int height,double scale,Preferences p)
        {
            var regions=LayoutRegions(width,height,scale,p);var state=g.Save();
            try
            {
                using(var pen=new Pen(Color.FromArgb(235,0,133,212),(float)scale))g.DrawRectangle(pen,(float)(scale/2),(float)(scale/2),(float)(width-scale),(float)(height-scale));
                using(var pen=new Pen(Color.FromArgb(220,222,123,32),(float)scale) { DashStyle=DashStyle.Dash })
                    foreach(var region in regions)g.DrawRectangle(pen,(float)(region.X+scale),(float)(region.Y+scale),(float)Math.Max(0,region.Width-2*scale),(float)Math.Max(0,region.Height-2*scale));
            }
            finally { g.Restore(state); }
        }
        static void DrawRow(Graphics g,Font font,int totalWidth,string name,Limit value,int y,int height,double scale,Preferences p,bool stale,Color text,Color background,int alpha,bool dark,bool weekly,string[] resets)
        {
            int pad=(int)Math.Round(6*scale),gap=(int)Math.Round(3*scale);
            using(var percentageFont=new Font(p.TaskbarNumberFont,(float)Math.Min(p.TaskbarPercentageFontSize*scale,Math.Max(7,(height-2)*.9)),FontStyle.Regular,GraphicsUnit.Pixel))
            using(var resetFont=new Font(TextFontFor(p,ResetText(value,p,weekly,true)),(float)Math.Max(7,font.Size-scale),FontStyle.Regular,GraphicsUnit.Pixel))
            {
            var suffix=p.TaskbarShowPercentSign?"%":"";
            int number=p.TaskbarShowPercentage?(int)Math.Ceiling(Math.Max(g.MeasureString("100"+suffix,percentageFont).Width,g.MeasureString("99.9"+suffix,percentageFont).Width)+2*scale):0;
            var resetCaption=ResetText(value,p,weekly,true);
            int resetWidth=resets[0].Length==0&&resets[1].Length==0?0:(int)(totalWidth*.36);
            var labelLimit=Math.Max(0,totalWidth-number-resetWidth-pad*2-gap*3-(int)(14*scale));
            var first=p.TaskbarLabel(false);var second=p.TaskbarLabel(true);
            int label=first.Length==0&&second.Length==0?0:Math.Min(labelLimit,(int)(totalWidth*.28));
            var accent=Accent(p,dark,weekly);var color=QuotaColor(value,p,stale,dark,weekly);
            if(p.TaskbarStyle=="Cards")
            {
                var card=new RectangleF(1,y+1,totalWidth-2,height-2);
                if(alpha>0)FillRound(g,card,(float)(5*scale),Color.FromArgb(alpha,background));
                using(var path=Round(card,(float)(5*scale)))using(var pen=new Pen(Color.FromArgb(65,accent),1))g.DrawPath(pen,path);
            }
            using(var brush=new SolidBrush(text))using(var format=new StringFormat { Alignment=StringAlignment.Near,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap })if(label>0)g.DrawString(name,font,brush,new RectangleF(pad,y,label,height),format);
            var amount=value==null?0:Math.Max(0,Math.Min(100,p.ShowUsed?value.Used:value.Remaining));
            var x=pad+label+gap;var width=Math.Max(10,totalWidth-x-number-resetWidth-pad-gap*2);var h=Math.Max(3,(float)(4*scale));var top=y+(height-h)/2;
            var track=Color.FromArgb(dark?65:45,text);
            if(p.TaskbarStyle=="Badges"&&p.TaskbarShowPercentage)
            {
                var badge=new RectangleF(x,y+2,width+number+gap,height-4);
                FillRound(g,badge,(float)(6*scale),Color.FromArgb(28,color));
                using(var path=Round(badge,(float)(6*scale)))using(var pen=new Pen(Color.FromArgb(70,color),1))g.DrawPath(pen,path);
            }
            else if(p.TaskbarStyle=="Dots")
            {
                var count=width<32*scale?4:7;var dot=Math.Min((float)(4*scale),width/(count*1.6f));var step=(width-dot)/(count-1);
                for(var i=0;i<count;i++)
                {
                    var box=new RectangleF(x+i*step,y+(height-dot)/2,dot,dot);
                    using(var brush=new SolidBrush(track))g.FillEllipse(brush,box);
                    var filled=(float)Math.Max(0,Math.Min(1,amount/100*count-i));
                    if(value!=null&&filled>0) { var state=g.Save();g.SetClip(new RectangleF(box.X,box.Y,box.Width*filled,box.Height));using(var brush=new SolidBrush(color))g.FillEllipse(brush,box);g.Restore(state); }
                }
            }
            else if(p.TaskbarStyle=="Segments")
            {
                var count=width<32*scale?5:8;var cellGap=(float)(1.5*scale);var cellWidth=(width-(count-1)*cellGap)/count;
                for(var i=0;i<count;i++)
                {
                    var cell=new RectangleF(x+i*(cellWidth+cellGap),top,cellWidth,h);
                    FillRound(g,cell,(float)scale,track);
                    var filled=(float)Math.Max(0,Math.Min(1,amount/100*count-i));
                    if(value!=null&&filled>0) { var state=g.Save();g.SetClip(new RectangleF(cell.X,cell.Y,cell.Width*filled,cell.Height));FillRound(g,cell,(float)scale,color);g.Restore(state); }
                }
            }
            else
            {
                var bar=new RectangleF(x,top,width,h);FillRound(g,bar,h/2,track);
                if(value!=null&&amount>0)
                {
                    var state=g.Save();g.SetClip(new RectangleF(x,top,(float)(width*amount/100),h));
                    if(p.TaskbarStyle=="Gradient")
                    {
                        var end=Color.FromArgb((color.R+255)/2,(color.G+255)/2,(color.B+255)/2);
                        using(var path=Round(bar,h/2))using(var brush=new LinearGradientBrush(bar,color,end,0f))g.FillPath(brush,path);
                    }
                    else FillRound(g,bar,h/2,color);
                    g.Restore(state);
                }
            }
            var caption=PercentageCaption(value,p);
            if(p.TaskbarShowPercentage)using(var brush=new SolidBrush(value==null?text:color))using(var format=new StringFormat { Alignment=p.TaskbarStyle=="Badges"?StringAlignment.Center:StringAlignment.Far,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap })g.DrawString(caption,percentageFont,brush,p.TaskbarStyle=="Badges"?new RectangleF(x,y,width+number+gap,height):new RectangleF(x+width+gap,y,number,height),format);
            using(var brush=new SolidBrush(text))
            using(var format=new StringFormat { Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap })
                if(resetWidth>0)g.DrawString(resetCaption,resetFont,brush,new RectangleF(totalWidth-resetWidth-pad,y,resetWidth,height),format);
            }
        }
        public static string PercentageCaption(Limit value,Preferences p) { return !p.TaskbarShowPercentage?"":value==null?"—":(p.ShowUsed?value.Used:value.Remaining).ToString("0.#")+(p.TaskbarShowPercentSign?"%":""); }
        public static Color SettingColor(Preferences p,bool dark,int index)
        {
            switch(index)
            {
                case 0:return ColorValue(p.TaskbarPrimaryColor,dark?Color.FromArgb(104,219,197):Color.FromArgb(0,128,107));
                case 1:return ColorValue(p.TaskbarSecondaryColor,dark?Color.FromArgb(162,181,255):Color.FromArgb(88,104,203));
                case 2:return ColorValue(p.TaskbarTextColor,dark?Color.FromArgb(239,246,248):Color.FromArgb(20,32,36));
                case 3:return ColorValue(p.TaskbarBackgroundColor,dark?Color.FromArgb(17,25,35):Color.White);
                case 4:case 7:return ColorValue(p.LowColor(index==7),Color.FromArgb(235,176,72));
                case 5:return ColorValue(p.TaskbarStaleColor,Color.FromArgb(225,92,92));
                case 6:case 8:return ColorValue(p.MediumColor(index==8),dark?Color.FromArgb(230,207,135):Color.FromArgb(138,107,25));
                default:throw new ArgumentOutOfRangeException("index");
            }
        }
        static Color Accent(Preferences p,bool dark,bool weekly) { return SettingColor(p,dark,weekly?1:0); }
        public static Color QuotaColor(Limit value,Preferences p,bool stale,bool dark,bool weekly)
        {
            return stale?SettingColor(p,dark,5):value!=null&&value.Remaining<=p.LowThreshold(weekly)?SettingColor(p,dark,weekly?7:4):value!=null&&value.Remaining<=p.MediumThreshold(weekly)?SettingColor(p,dark,weekly?8:6):Accent(p,dark,weekly);
        }
        static string ResetText(Limit value,Preferences p,bool weekly,bool icon)
        {
            var style=weekly?p.TaskbarWeeklyResetStyle:p.TaskbarPrimaryResetStyle;
            if(style=="Hidden")return "";
            return (icon?"↻ ":"")+(value==null?"—":value.ResetDisplay(style));
        }
        static void DrawRingPanel(Graphics g,int left,int width,int height,double scale,Limit value,Preferences p,bool stale,Color text,bool dark,bool weekly)
        {
            var state=g.Save();g.SetClip(new Rectangle(left,0,width,height));
            try
            {
                var color=QuotaColor(value,p,stale,dark,weekly);var amount=value==null?0:Math.Max(0,Math.Min(100,p.ShowUsed?value.Used:value.Remaining));
                var pad=(float)(5*scale);var diameter=Math.Max(8,Math.Min((float)(34*scale),height-(float)(6*scale)));
                var circle=new RectangleF(left+pad,(height-diameter)/2,diameter,diameter);
                using(var pen=new Pen(Color.FromArgb(dark?55:40,text),(float)(p.TaskbarRingThickness*scale)))g.DrawEllipse(pen,circle);
                if(value!=null&&amount>0)using(var pen=new Pen(color,(float)(p.TaskbarRingThickness*scale)) { StartCap=LineCap.Round,EndCap=LineCap.Round })g.DrawArc(pen,circle,-90,(float)(359.9*amount/100));
                var caption=PercentageCaption(value,p);
                DrawCenteredNumber(g,circle,caption,p.TaskbarNumberFont,(float)(p.TaskbarPercentageFontSize*scale),(float)((p.TaskbarRingThickness+4)*scale),value==null?text:color);
                var reset=ResetText(value,p,weekly,false);var size=(float)Math.Min((p.TaskbarFontSize-1)*scale,Math.Max(7,(height/2.0-2)*.85));
                var x=circle.Right+(float)(5*scale);var textWidth=Math.Max(1,left+width-pad-x);
                var resetLines=reset.Length==0?new string[0]:new[] { reset };
                using(var probe=new Font(TextFontFor(p,p.TaskbarLabel(weekly)+ResetText(value,p,weekly,false)),size,FontStyle.Regular,GraphicsUnit.Pixel))
                {
                    if(reset.Length>0&&g.MeasureString(reset,probe).Width>textWidth)
                    {
                        var split=reset.LastIndexOf(' ');
                        if(split<0) { var day=reset.IndexOf('天');if(day>=0&&day<reset.Length-1)split=day+1; }
                        if(split>0&&split<reset.Length-1)resetLines=new[] { reset.Substring(0,split).Trim(),reset.Substring(split).Trim() };
                    }
                }
                var rows=1+resetLines.Length;
                if(rows>2)size=(float)Math.Min(size,Math.Max(7,(height/(double)rows-.5)*.82));
                using(var labelFont=new Font(TextFontFor(p,p.TaskbarLabel(weekly)+ResetText(value,p,weekly,false)),size,FontStyle.Regular,GraphicsUnit.Pixel))
                using(var resetFont=new Font(TextFontFor(p,p.TaskbarLabel(weekly)+ResetText(value,p,weekly,false)),size,FontStyle.Regular,GraphicsUnit.Pixel))
                using(var format=new StringFormat { Alignment=StringAlignment.Near,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap })
                {
                    var line=rows>2?height/(float)rows:(float)(16*scale);var top=(height-line*rows)/2;
                    DrawAlignedRingText(g,p.TaskbarLabel(weekly),labelFont,new RectangleF(x,top,textWidth,line),text);
                    for(var i=0;i<resetLines.Length;i++)DrawAlignedRingText(g,resetLines[i],resetFont,new RectangleF(x,top+line*(i+1),textWidth,line),Color.FromArgb(225,text));
                }
            }
            finally { g.Restore(state); }
        }
        public static void DrawAlignedRingText(Graphics g,string caption,Font font,RectangleF box,Color color)
        {
            if(string.IsNullOrEmpty(caption)||box.Width<=0)return;
            using(var path=new GraphicsPath(FillMode.Winding))using(var format=(StringFormat)StringFormat.GenericTypographic.Clone())
            {
                var visible=caption;bool trimmed=false;RectangleF bounds;
                while(true)
                {
                    path.Reset();path.AddString(visible+(trimmed?"…":""),font.FontFamily,(int)font.Style,font.Size,new PointF(0,0),format);bounds=path.GetBounds();
                    if(bounds.Width<=box.Width||visible.Length==0)break;
                    trimmed=true;visible=visible.Substring(0,visible.Length-1);
                }
                if(bounds.Width<=0||bounds.Height<=0)return;
                using(var matrix=new Matrix()) { matrix.Translate(box.X-bounds.X,box.Y+(box.Height-bounds.Height)/2-bounds.Y);path.Transform(matrix); }
                var state=g.Save();try { g.SetClip(box,CombineMode.Intersect);using(var brush=new SolidBrush(color))g.FillPath(brush,path); }finally { g.Restore(state); }
            }
        }
        static int MeasureRingPanel(Graphics g,int height,double scale,Limit value,Preferences p,bool weekly)
        {
            var size=(float)Math.Min((p.TaskbarFontSize-1)*scale,Math.Max(7,(height/2.0-2)*.85));
            using(var font=new Font(TextFontFor(p,p.TaskbarLabel(weekly)+ResetText(value,p,weekly,false)),size,FontStyle.Regular,GraphicsUnit.Pixel))
            {
                var textWidth=g.MeasureString(p.TaskbarLabel(weekly),font).Width;
                var reset=ResetText(value,p,weekly,false);
                if(reset.Length>0)textWidth=Math.Max(textWidth,g.MeasureString(reset,font).Width);
                var diameter=Math.Max(8,Math.Min((float)(34*scale),height-(float)(6*scale)));
                return (int)Math.Ceiling(diameter+15*scale+textWidth);
            }
        }
        static string TextFontFor(Preferences p,string value) { foreach(var c in value)if(c>=0x2E80)return p.TaskbarChineseFont;return p.TaskbarTextFont; }
        public static void DrawCenteredNumber(Graphics g,RectangleF circle,string caption,string family,float size,float inset,Color color)
        {
            if(caption.Length==0)return;
            using(var fontFamily=new FontFamily(family))using(var path=new GraphicsPath(FillMode.Winding))using(var format=(StringFormat)StringFormat.GenericTypographic.Clone())
            {
                path.AddString(caption,fontFamily,(int)FontStyle.Bold,size,new PointF(0,0),format);var bounds=path.GetBounds();if(bounds.Width<=0||bounds.Height<=0)return;
                var available=Math.Max(1,circle.Width-inset);var fit=Math.Min(1,Math.Min(available/bounds.Width,available/bounds.Height));
                using(var matrix=new Matrix()) { matrix.Translate(-bounds.X,-bounds.Y);matrix.Scale(fit,fit,MatrixOrder.Append);matrix.Translate(circle.X+(circle.Width-bounds.Width*fit)/2,circle.Y+(circle.Height-bounds.Height*fit)/2,MatrixOrder.Append);path.Transform(matrix); }
                using(var brush=new SolidBrush(color))g.FillPath(brush,path);
            }
        }
        static GraphicsPath Round(RectangleF box,float radius)
        {
            var path=new GraphicsPath();var d=Math.Min(Math.Min(box.Width,box.Height),radius*2);
            if(d<=0)return path;
            path.AddArc(box.Left,box.Top,d,d,180,90);path.AddArc(box.Right-d,box.Top,d,d,270,90);path.AddArc(box.Right-d,box.Bottom-d,d,d,0,90);path.AddArc(box.Left,box.Bottom-d,d,d,90,90);path.CloseFigure();return path;
        }
        static void FillRound(Graphics g,RectangleF box,float radius,Color color) { using(var path=Round(box,radius))using(var brush=new SolidBrush(color))g.FillPath(brush,path); }
    }
}
