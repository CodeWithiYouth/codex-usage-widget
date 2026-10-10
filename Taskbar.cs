using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;

namespace WindowsUsageWidget
{
    public class TaskbarDisplay
    {
        public double Value;
        public TaskbarItemProgressState State;
        public string Badge,Description;
        public static TaskbarDisplay From(Snapshot snapshot,Preferences p,bool stale,bool querying)
        {
            var chosen=snapshot==null?null:p.TaskbarQuota=="Secondary"?snapshot.Secondary:snapshot.Primary;
            var mode=p.ShowUsed?"已用":"剩余";
            var display=new TaskbarDisplay { Badge="—",Value=0,State=TaskbarItemProgressState.None,Description="Codex 用量 · 等待数据" };
            if(chosen==null)
            {
                display.Badge=stale?"!":"—";
                display.State=stale?TaskbarItemProgressState.Error:querying?TaskbarItemProgressState.Indeterminate:TaskbarItemProgressState.None;
                display.Description=stale?"Codex 用量 · 查询失败，将自动重试":"Codex 用量 · 所选额度暂无数据";
                return display;
            }
            var amount=p.ShowUsed?chosen.Used:chosen.Remaining;
            display.Value=Math.Max(0,Math.Min(1,amount/100));
            display.Badge=amount.ToString("0",CultureInfo.InvariantCulture)+"%";
            display.State=stale?TaskbarItemProgressState.Error:chosen.Remaining<=p.LowThreshold(p.TaskbarQuota=="Secondary")?TaskbarItemProgressState.Paused:TaskbarItemProgressState.Normal;
            display.Description="Codex · "+mode+(stale?" · 数据已过期":"")+"\n";
            if(snapshot.Primary!=null)display.Description+=QuotaDescription(snapshot.Primary,p)+"\n";
            if(snapshot.Secondary!=null)display.Description+=QuotaDescription(snapshot.Secondary,p)+"\n";
            display.Description+="最后更新 "+snapshot.Received.ToString("HH:mm:ss");
            return display;
        }
        static string QuotaDescription(Limit value,Preferences p)
        {
            var amount=p.ShowUsed?value.Used:value.Remaining;var now=(long)(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds;
            var reset=value.Reset<=0?"重置时间未知":value.Reset<=now?"等待服务端更新":value.ResetDisplay("Weekday")+" 重置";
            return value.Label+"："+amount.ToString("0.#")+"% · "+reset;
        }
        public ImageSource MakeIcon()
        {
            var visual=new DrawingVisual();
            var color=State==TaskbarItemProgressState.Error?Color.FromRgb(166,55,55):State==TaskbarItemProgressState.Paused?Color.FromRgb(153,98,18):Color.FromRgb(0,95,83);
            using(var dc=visual.RenderOpen())
            {
                dc.DrawRoundedRectangle(new SolidColorBrush(color),null,new Rect(0,0,32,32),6,6);
                var text=new FormattedText(Badge,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface(new FontFamily(UI.TextFontFamily),FontStyles.Normal,FontWeights.SemiBold,FontStretches.Normal),Badge.Length>3?12:15,Brushes.White,1.0);
                dc.DrawText(text,new Point((32-text.WidthIncludingTrailingWhitespace)/2,(29-text.Height)/2));
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(90,255,255,255)),null,new Rect(3,27,26,2),1,1);
                if(Value>0)dc.DrawRoundedRectangle(Brushes.White,null,new Rect(3,27,26*Value,2),1,1);
            }
            var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(32,32,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);bitmap.Freeze();return bitmap;
        }
    }
}
