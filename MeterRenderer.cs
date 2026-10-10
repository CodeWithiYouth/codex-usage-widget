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
            var text=ColorValue(p.TaskbarTextColor,dark?Color.FromArgb(239,246,248):Color.FromArgb(20,32,36));
            var background=ColorValue(p.TaskbarBackgroundColor,dark?Color.FromArgb(17,25,35):Color.White);
            var alpha=(int)(255*p.TaskbarBackgroundOpacity/100.0);
            if(p.TaskbarStyle!="Cards"&&p.TaskbarStyle!="Rings"&&alpha>0)FillRound(g,new RectangleF(1,1,width-2,height-2),(float)(8*scale),Color.FromArgb(alpha,background));
            if(p.TaskbarStyle=="Rings")
            {
                var primary=snapshot==null?null:snapshot.Primary;var secondary=snapshot==null?null:snapshot.Secondary;
                var gap=(int)Math.Round(6*scale);var maximum=(width-gap)/2;
                var first=Math.Min(maximum,MeasureRingPanel(g,height,scale,primary,p,false));
                var second=Math.Min(maximum,MeasureRingPanel(g,height,scale,secondary,p,true));
                var total=first+second+gap;var start=(width-total)/2;
                if(alpha>0)FillRound(g,new RectangleF(start,1,total,height-2),(float)(8*scale),Color.FromArgb(alpha,background));
                DrawRingPanel(g,start,first,height,scale,primary,p,stale,text,dark,false);
                DrawRingPanel(g,start+first+gap,second,height,scale,secondary,p,stale,text,dark,true);
                var separator=start+first+gap/2;
                using(var pen=new Pen(Color.FromArgb(35,text),1))g.DrawLine(pen,separator,(float)(6*scale),separator,height-(float)(6*scale));
                return;
            }
            var physicalFont=(float)Math.Min(p.TaskbarFontSize*scale,Math.Max(7,(height/2.0-1)*.9));
            using(var font=new Font("Segoe UI",physicalFont,FontStyle.Regular,GraphicsUnit.Pixel))
            {
                var resets=new[] { ResetText(snapshot==null?null:snapshot.Primary,p,false,true),ResetText(snapshot==null?null:snapshot.Secondary,p,true,true) };
                DrawRow(g,font,width,p.TaskbarLabel(false),snapshot==null?null:snapshot.Primary,0,height/2,scale,p,stale,text,background,alpha,dark,false,resets);
                DrawRow(g,font,width,p.TaskbarLabel(true),snapshot==null?null:snapshot.Secondary,height/2,height-height/2,scale,p,stale,text,background,alpha,dark,true,resets);
            }
        }
        static void DrawRow(Graphics g,Font font,int totalWidth,string name,Limit value,int y,int height,double scale,Preferences p,bool stale,Color text,Color background,int alpha,bool dark,bool weekly,string[] resets)
        {
            int pad=(int)Math.Round(6*scale),gap=(int)Math.Round(3*scale);
            using(var resetFont=new Font("Segoe UI",(float)Math.Max(7,font.Size-scale),FontStyle.Regular,GraphicsUnit.Pixel))
            {
            int number=(int)Math.Ceiling(Math.Max(g.MeasureString("100%",font).Width,g.MeasureString("99.9%",font).Width)+2*scale);
            var resetCaption=ResetText(value,p,weekly,true);
            int resetWidth=resets[0].Length==0&&resets[1].Length==0?0:Math.Min((int)(totalWidth*.42),(int)Math.Ceiling(Math.Max(g.MeasureString(resets[0],resetFont).Width,g.MeasureString(resets[1],resetFont).Width)+2*scale));
            var labelLimit=Math.Max(0,totalWidth-number-resetWidth-pad*2-gap*3-(int)(14*scale));
            var first=p.TaskbarLabel(false);var second=p.TaskbarLabel(true);
            int label=first.Length==0&&second.Length==0?0:Math.Min(labelLimit,(int)Math.Ceiling(Math.Max(g.MeasureString(first,font).Width,g.MeasureString(second,font).Width)+scale));
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
            if(p.TaskbarStyle=="Badges")
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
            var caption=value==null?"—":amount.ToString("0.#")+"%";
            using(var brush=new SolidBrush(value==null?text:color))using(var format=new StringFormat { Alignment=p.TaskbarStyle=="Badges"?StringAlignment.Center:StringAlignment.Far,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap })g.DrawString(caption,font,brush,p.TaskbarStyle=="Badges"?new RectangleF(x,y,width+number+gap,height):new RectangleF(x+width+gap,y,number,height),format);
            using(var brush=new SolidBrush(text))
            using(var format=new StringFormat { Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap })
                if(resetWidth>0)g.DrawString(resetCaption,resetFont,brush,new RectangleF(totalWidth-resetWidth-pad,y,resetWidth,height),format);
            }
        }
        static Color Accent(Preferences p,bool dark,bool weekly) { return ColorValue(weekly?p.TaskbarSecondaryColor:p.TaskbarPrimaryColor,weekly?(dark?Color.FromArgb(162,181,255):Color.FromArgb(88,104,203)):(dark?Color.FromArgb(104,219,197):Color.FromArgb(0,128,107))); }
        static Color QuotaColor(Limit value,Preferences p,bool stale,bool dark,bool weekly)
        {
            return stale?ColorValue(p.TaskbarStaleColor,Color.FromArgb(225,92,92)):value!=null&&value.Remaining<=p.WarningPercent?ColorValue(p.TaskbarWarningColor,Color.FromArgb(235,176,72)):value!=null&&value.Remaining<=p.TaskbarMediumPercent?ColorValue(p.TaskbarMediumColor,dark?Color.FromArgb(230,207,135):Color.FromArgb(138,107,25)):Accent(p,dark,weekly);
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
                var pad=(float)(4*scale);var diameter=Math.Max(8,Math.Min((float)(34*scale),height-(float)(6*scale)));
                var circle=new RectangleF(left+pad,(height-diameter)/2,diameter,diameter);
                using(var pen=new Pen(Color.FromArgb(dark?55:40,text),(float)(2.5*scale)))g.DrawEllipse(pen,circle);
                if(value!=null&&amount>0)using(var pen=new Pen(color,(float)(2.5*scale)) { StartCap=LineCap.Round,EndCap=LineCap.Round })g.DrawArc(pen,circle,-90,(float)(359.9*amount/100));
                var caption=value==null?"—":amount.ToString("0.#")+"%";
                var numberSize=(float)Math.Min(p.TaskbarFontSize*scale,diameter*.36);
                using(var probe=new Font("Segoe UI",numberSize,FontStyle.Bold,GraphicsUnit.Pixel))
                {
                    var measured=g.MeasureString(caption,probe).Width;var available=diameter-(float)(6*scale);
                    if(measured>available)numberSize*=available/measured;
                }
                using(var numberFont=new Font("Segoe UI",Math.Max(6,numberSize),FontStyle.Bold,GraphicsUnit.Pixel))
                using(var brush=new SolidBrush(value==null?text:color))
                using(var format=new StringFormat { Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap })g.DrawString(caption,numberFont,brush,circle,format);
                var reset=ResetText(value,p,weekly,false);var size=(float)Math.Min((p.TaskbarFontSize-1)*scale,Math.Max(7,(height/2.0-2)*.85));
                var x=circle.Right+(float)(5*scale);var textWidth=Math.Max(1,left+width-pad-x);
                var resetLines=reset.Length==0?new string[0]:new[] { reset };
                using(var probe=new Font("Segoe UI",size,FontStyle.Regular,GraphicsUnit.Pixel))
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
                using(var labelFont=new Font("Segoe UI",size,FontStyle.Regular,GraphicsUnit.Pixel))
                using(var resetFont=new Font("Segoe UI",size,FontStyle.Regular,GraphicsUnit.Pixel))
                using(var format=new StringFormat { Alignment=StringAlignment.Near,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap })
                {
                    var line=rows>2?height/(float)rows:(float)(16*scale);var top=(height-line*rows)/2;
                    using(var brush=new SolidBrush(text))g.DrawString(p.TaskbarLabel(weekly),labelFont,brush,new RectangleF(x,top,textWidth,line),format);
                    using(var brush=new SolidBrush(Color.FromArgb(225,text)))for(var i=0;i<resetLines.Length;i++)g.DrawString(resetLines[i],resetFont,brush,new RectangleF(x,top+line*(i+1),textWidth,line),format);
                }
            }
            finally { g.Restore(state); }
        }
        static int MeasureRingPanel(Graphics g,int height,double scale,Limit value,Preferences p,bool weekly)
        {
            var size=(float)Math.Min((p.TaskbarFontSize-1)*scale,Math.Max(7,(height/2.0-2)*.85));
            using(var font=new Font("Segoe UI",size,FontStyle.Regular,GraphicsUnit.Pixel))
            {
                var textWidth=g.MeasureString(p.TaskbarLabel(weekly),font).Width;
                var reset=ResetText(value,p,weekly,false);
                if(reset.Length>0)textWidth=Math.Max(textWidth,g.MeasureString(reset,font).Width);
                var diameter=Math.Max(8,Math.Min((float)(34*scale),height-(float)(6*scale)));
                return (int)Math.Ceiling(diameter+14*scale+textWidth);
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
