using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Forms=System.Windows.Forms;

namespace WindowsUsageWidget
{
    public class EmbeddedMeter : Forms.Form
    {
        [StructLayout(LayoutKind.Sequential)] struct Rect { public int Left,Top,Right,Bottom; }
        [StructLayout(LayoutKind.Sequential)] struct Point { public int X,Y; }
        [StructLayout(LayoutKind.Sequential)] struct BitmapSize { public int Width,Height; }
        [StructLayout(LayoutKind.Sequential,Pack=1)] struct Blend { public byte Op,Flags,Alpha,Format; }
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr FindWindow(string name,string title);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string name,string title);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window,out Rect rect);
        [DllImport("user32.dll")] static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr window);
        [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SetParent(IntPtr window,IntPtr parent);
        [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr window,int index);
        [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr window,int index,IntPtr value);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
        [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr window);
        [DllImport("user32.dll")] static extern IntPtr GetWindowDpiAwarenessContext(IntPtr window);
        [DllImport("user32.dll")] static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")] static extern bool AreDpiAwarenessContextsEqual(IntPtr first,IntPtr second);
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr window,IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc,IntPtr bitmap);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr value);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
        [DllImport("user32.dll",SetLastError=true)] static extern bool UpdateLayeredWindow(IntPtr window,IntPtr destinationDC,IntPtr position,ref BitmapSize size,IntPtr sourceDC,ref Point source,uint color,ref Blend blend,uint flags);
        IntPtr taskbar;
        Preferences preferences;
        Snapshot snapshot;
        bool stale;
        double scale=1;
        int lastX=int.MinValue,lastY,lastWidth,lastHeight;
        string lastTip="";
        Forms.ToolTip tooltip=new Forms.ToolTip { AutoPopDelay=15000,InitialDelay=400,ReshowDelay=200 };
        public event Action OpenDetails,OpenSettings,RefreshRequested,ExitRequested;
        public event Action<string> SideChanged;
        public bool Attached { get { return taskbar!=IntPtr.Zero&&IsWindow(taskbar)&&GetParent(Handle)==taskbar; } }
        public bool AlphaRendered { get; private set; }
        public int AlphaError { get; private set; }
        public bool DpiMatched { get; private set; }
        public int RenderDpi { get { return (int)Math.Round(scale*96); } }
        public EmbeddedMeter(Preferences p)
        {
            preferences=p;Text="Codex 任务栏用量";FormBorderStyle=Forms.FormBorderStyle.None;ShowInTaskbar=false;AutoScaleMode=Forms.AutoScaleMode.None;
            BackColor=Color.Black;
            SetStyle(Forms.ControlStyles.UserPaint|Forms.ControlStyles.AllPaintingInWmPaint|Forms.ControlStyles.OptimizedDoubleBuffer,true);
            var menu=new Forms.ContextMenuStrip();
            menu.Items.Add("显示详细用量",null,delegate { if(OpenDetails!=null)OpenDetails(); });
            menu.Items.Add("立即刷新",null,delegate { if(RefreshRequested!=null)RefreshRequested(); });
            menu.Items.Add("参数设置",null,delegate { if(OpenSettings!=null)OpenSettings(); });
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("放在任务栏左侧",null,delegate { if(SideChanged!=null)SideChanged("Left"); });
            menu.Items.Add("放在任务栏右侧",null,delegate { if(SideChanged!=null)SideChanged("Right"); });
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("退出",null,delegate { if(ExitRequested!=null)ExitRequested(); });ContextMenuStrip=menu;
            MouseDoubleClick+=delegate { if(OpenDetails!=null)OpenDetails(); };
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override Forms.CreateParams CreateParams { get { var p=base.CreateParams;p.ExStyle|=0x08000000|0x80|0x80000;return p; } }
        public void Update(Snapshot data,Preferences p,bool expired)
        {
            snapshot=data;preferences=p;stale=expired;
            var description=TaskbarDisplay.From(data,p,expired,data==null).Description;
            var labels=(p.TaskbarLabel(false)+" / "+p.TaskbarLabel(true)).Trim(' ','/');
            if(labels.Length>0)description=labels+Environment.NewLine+description;
            if(description!=lastTip) { tooltip.SetToolTip(this,description);lastTip=description; }
            if(DockToTaskbar())RenderSurface();
        }
        public bool DockToTaskbar()
        {
            var parent=FindWindow("Shell_TrayWnd",null);if(parent==IntPtr.Zero) { Hide();return false; }
            IntPtr previous=IntPtr.Zero;
            try { previous=SetThreadDpiAwarenessContext(GetWindowDpiAwarenessContext(parent));return DockCore(parent); }
            finally { if(previous!=IntPtr.Zero)SetThreadDpiAwarenessContext(previous); }
        }
        bool DockCore(IntPtr parent)
        {
            if(!Visible)Show();
            if(!IsHandleCreated)CreateControl();
            bool changedParent=taskbar!=parent||GetParent(Handle)!=parent;
            DpiMatched=AreDpiAwarenessContextsEqual(GetWindowDpiAwarenessContext(Handle),GetWindowDpiAwarenessContext(parent));
            if(changedParent)
            {
                taskbar=parent;var style=GetWindowLongPtr(Handle,-16).ToInt64();
                SetWindowLongPtr(Handle,-16,new IntPtr((style&~0x80000000L)|0x40000000L));
                SetParent(Handle,parent);
            }
            Rect bar;GetWindowRect(parent,out bar);scale=Math.Max(96,GetDpiForWindow(parent))/96.0;
            int width=(int)Math.Round(preferences.TaskbarMeterWidth*scale),height=Math.Min(bar.Bottom-bar.Top-4,(int)Math.Round(42*scale));
            int x;
            if(preferences.TaskbarSide=="Left")
            {
                bool widgets=Convert.ToInt32(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced","TaskbarDa",1))!=0;
                x=(int)((widgets?150:8)*scale);
            }
            else
            {
                var tray=FindWindowEx(parent,IntPtr.Zero,"TrayNotifyWnd",null);Rect trayRect;
                var edge=tray!=IntPtr.Zero&&GetWindowRect(tray,out trayRect)?trayRect.Left-bar.Left:bar.Right-bar.Left-(int)(300*scale);
                x=edge-width-(int)(8*scale);
            }
            x=Math.Max(0,Math.Min(bar.Right-bar.Left-width,x+(int)(preferences.TaskbarOffset*scale)));
            var y=Math.Max(0,(bar.Bottom-bar.Top-height)/2);
            if(changedParent||lastX!=x||lastY!=y||lastWidth!=width||lastHeight!=height)
            {
                SetWindowPos(Handle,IntPtr.Zero,x,y,width,height,0x10|0x40|(changedParent?0x20u:0u));
                lastX=x;lastY=y;lastWidth=width;lastHeight=height;
            }
            return Attached;
        }
        protected override void OnPaintBackground(Forms.PaintEventArgs e) { }
        protected override void OnPaint(Forms.PaintEventArgs e)
        {
            RenderSurface();
        }
        void RenderSurface()
        {
            if(ClientSize.Width<1||ClientSize.Height<1)return;
            bool dark=Convert.ToInt32(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize","SystemUsesLightTheme",0))==0;
            using(var bitmap=new Bitmap(ClientSize.Width,ClientSize.Height,System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using(var graphics=Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.FromArgb(1,0,0,0));
                MeterRenderer.Draw(graphics,ClientSize.Width,ClientSize.Height,scale,snapshot,preferences,stale,dark);
                var screen=GetDC(IntPtr.Zero);var dc=CreateCompatibleDC(screen);var image=bitmap.GetHbitmap(Color.FromArgb(0));var old=SelectObject(dc,image);
                try { var size=new BitmapSize { Width=bitmap.Width,Height=bitmap.Height };var origin=new Point();var blend=new Blend { Op=0,Flags=0,Alpha=255,Format=1 };AlphaRendered=UpdateLayeredWindow(Handle,screen,IntPtr.Zero,ref size,dc,ref origin,0,ref blend,2);AlphaError=AlphaRendered?0:Marshal.GetLastWin32Error(); }
                finally { SelectObject(dc,old);DeleteObject(image);DeleteDC(dc);ReleaseDC(IntPtr.Zero,screen); }
            }
        }
        protected override void Dispose(bool disposing) { if(disposing)tooltip.Dispose();base.Dispose(disposing); }
    }
}
