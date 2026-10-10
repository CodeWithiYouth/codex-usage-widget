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
            if(p.TaskbarStyle!="Cards"&&alpha>0)FillRound(g,new RectangleF(1,1,width-2,height-2),(float)(8*scale),Color.FromArgb(alpha,background));
            var physicalFont=(float)Math.Min(p.TaskbarFontSize*scale,Math.Max(7,(height/2.0-1)*.9));
            using(var font=new Font("Segoe UI",physicalFont,FontStyle.Regular,GraphicsUnit.Pixel))
            {
                DrawRow(g,font,width,p.TaskbarLabel(false),snapshot==null?null:snapshot.Primary,0,height/2,scale,p,stale,text,background,alpha,dark,false);
                DrawRow(g,font,width,p.TaskbarLabel(true),snapshot==null?null:snapshot.Secondary,height/2,height-height/2,scale,p,stale,text,background,alpha,dark,true);
            }
        }
        static void DrawRow(Graphics g,Font font,int totalWidth,string name,Limit value,int y,int height,double scale,Preferences p,bool stale,Color text,Color background,int alpha,bool dark,bool weekly)
        {
            int pad=(int)Math.Round(6*scale),gap=(int)Math.Round(3*scale);
            using(var resetFont=new Font("Segoe UI",(float)Math.Max(7,font.Size-scale),FontStyle.Regular,GraphicsUnit.Pixel))
            {
            int number=(int)Math.Ceiling(Math.Max(g.MeasureString("100%",font).Width,g.MeasureString("99.9%",font).Width)+2*scale);
            int resetWidth=(int)Math.Ceiling(g.MeasureString("↻ 周四 23:59",resetFont).Width+2*scale);
            var labelLimit=Math.Max(0,totalWidth-number-resetWidth-pad*2-gap*3-(int)(14*scale));
            var first=p.TaskbarLabel(false);var second=p.TaskbarLabel(true);
            int label=first.Length==0&&second.Length==0?0:Math.Min(labelLimit,(int)Math.Ceiling(Math.Max(g.MeasureString(first,font).Width,g.MeasureString(second,font).Width)+scale));
            var accent=ColorValue(weekly?p.TaskbarSecondaryColor:p.TaskbarPrimaryColor,weekly?(dark?Color.FromArgb(162,181,255):Color.FromArgb(88,104,203)):(dark?Color.FromArgb(104,219,197):Color.FromArgb(0,128,107)));
            var color=stale?ColorValue(p.TaskbarStaleColor,Color.FromArgb(225,92,92)):value!=null&&value.Remaining<=p.WarningPercent?ColorValue(p.TaskbarWarningColor,Color.FromArgb(235,176,72)):value!=null&&value.Remaining<=p.TaskbarMediumPercent?ColorValue(p.TaskbarMediumColor,dark?Color.FromArgb(230,207,135):Color.FromArgb(138,107,25)):accent;
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
            else if(p.TaskbarStyle=="Rings")
            {
                var diameter=Math.Min(height-6,(float)(13*scale));var ring=new RectangleF(x+(width-diameter)/2,y+(height-diameter)/2,diameter,diameter);
                using(var pen=new Pen(track,(float)(2*scale)))g.DrawEllipse(pen,ring);
                if(value!=null&&amount>0)using(var pen=new Pen(color,(float)(2*scale)) { StartCap=LineCap.Round,EndCap=LineCap.Round })g.DrawArc(pen,ring,-90,(float)(359.9*amount/100));
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
            using(var brush=new SolidBrush(Color.FromArgb(195,text)))
            using(var format=new StringFormat { Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap })
                g.DrawString(value==null?"↻ —":value.CompactReset(weekly),resetFont,brush,new RectangleF(totalWidth-resetWidth-pad,y,resetWidth,height),format);
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
