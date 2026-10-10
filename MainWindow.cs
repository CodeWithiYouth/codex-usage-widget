using System;
using System.IO;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Shell;
using System.Web.Script.Serialization;
using System.Windows.Interop;

namespace WindowsUsageWidget
{
    public class MainWindow : Window
    {
        Preferences preferences=Preferences.Load();
        Grid root=new Grid();
        TextBlock plan=UI.Text("账号共享额度",12),status=UI.Text("正在连接",11),error=UI.Text("",11);
        Gauge primary=new Gauge("5 小时额度"),secondary=new Gauge("每周额度");
        Button refresh=UI.Icon("\uE72C","刷新用量");
        Client client;
        Snapshot snapshot;
        SemaphoreSlim mutex=new SemaphoreSlim(1,1);
        CancellationTokenSource lifetime=new CancellationTokenSource();
        DispatcherTimer timer=new DispatcherTimer();
        DateTime next=DateTime.MinValue;
        string state="正在连接";
        int failures;
        bool closing;
        bool exitRequested;
        SettingsWindow settingsWindow;
        string taskbarIconKey="";
        bool taskbarProofWritten;
        EmbeddedMeter meter;
        Task cleanupTask;
        public MainWindow()
        {
            Title="Codex 用量 · "+AppVersion.Display;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;FontFamily=new FontFamily(UI.TextFontFamily);Language=System.Windows.Markup.XmlLanguage.GetLanguage("zh-CN");
            Width=preferences.Width;Height=preferences.Height;Topmost=preferences.AlwaysOnTop;
            ShowInTaskbar=false;
            TaskbarItemInfo=new TaskbarItemInfo();
            var taskbarRefresh=new ThumbButtonInfo { Description="刷新用量",ImageSource=new TaskbarDisplay { Badge="↻",State=TaskbarItemProgressState.Normal }.MakeIcon() };
            taskbarRefresh.Click+=async delegate { await Refresh(); };
            TaskbarItemInfo.ThumbButtonInfos.Add(taskbarRefresh);
            var taskbarSettings=new ThumbButtonInfo { Description="参数设置",ImageSource=new TaskbarDisplay { Badge="⚙",State=TaskbarItemProgressState.Normal }.MakeIcon() };
            taskbarSettings.Click+=delegate { WindowState=WindowState.Normal;ShowSettings(); };
            TaskbarItemInfo.ThumbButtonInfos.Add(taskbarSettings);
            if(preferences.X.HasValue&&preferences.Y.HasValue) { var desk=System.Windows.Forms.SystemInformation.VirtualScreen;Left=Math.Max(desk.Left,Math.Min(preferences.X.Value,desk.Right-Width));Top=Math.Max(desk.Top,Math.Min(preferences.Y.Value,desk.Bottom-Height)); }
            root.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });root.RowDefinitions.Add(new RowDefinition());
            var chrome=UI.Chrome(this);root.Children.Add(chrome);
            var body=new StackPanel { Margin=new Thickness(12,6,12,12) };
            var name=new StackPanel();var heading=UI.Text("Codex",20);heading.FontWeight=FontWeights.SemiBold;name.Children.Add(heading);plan.Opacity=.75;plan.Margin=new Thickness(0,2,0,0);name.Children.Add(plan);
            var settings=UI.Icon("\uE713","调整参数");settings.Click+=delegate { QueueSettings(); };var header=UI.TwoColumns(name,settings);header.Margin=new Thickness(0,0,0,14);body.Children.Add(header);
            body.Children.Add(primary.View);body.Children.Add(secondary.View);error.TextWrapping=TextWrapping.Wrap;error.Visibility=Visibility.Collapsed;error.Margin=new Thickness(0,0,0,8);body.Children.Add(error);
            status.Opacity=.75;body.Children.Add(UI.TwoColumns(status,refresh));refresh.Click+=async delegate { await Refresh(); };
            Grid.SetRow(body,1);root.Children.Add(body);Content=root;
            SourceInitialized+=delegate { HwndSource.FromHwnd(new WindowInteropHelper(this).Handle).AddHook(LifecycleMessage);Apply();EnsureMeter();Dispatcher.BeginInvoke(new Action(async delegate { await Refresh(); })); };
            timer.Interval=TimeSpan.FromSeconds(1);timer.Tick+=async delegate { Paint();if(!closing&&DateTime.Now>=next&&mutex.CurrentCount>0)await Refresh(); };timer.Start();
            Loaded+=delegate { if(Array.IndexOf(Environment.GetCommandLineArgs(),"--settings")>=0)QueueSettings(); };
            StateChanged+=delegate { if(!closing&&WindowState==WindowState.Minimized) { RememberPosition();WindowState=WindowState.Normal;Hide(); } };
            Closing+=Close;
        }
        public void Start()
        {
            var arguments=Environment.GetCommandLineArgs();
            if(preferences.StartMinimized&&Array.IndexOf(arguments,"--settings")<0&&Array.IndexOf(arguments,"--window")<0)
                new WindowInteropHelper(this).EnsureHandle();
            else Show();
        }
        void Apply() { preferences.Normalize();Width=preferences.Width;Height=preferences.Height;Topmost=preferences.AlwaysOnTop;Native.Apply(this,root,preferences);Paint(); }
        void Paint()
        {
            if(closing)return;
            if(snapshot!=null&&!string.IsNullOrWhiteSpace(snapshot.Plan))plan.Text=char.ToUpper(snapshot.Plan[0])+snapshot.Plan.Substring(1)+" · 账号共享额度";
            primary.Update(snapshot==null?null:snapshot.Primary,preferences);secondary.Update(snapshot==null?null:snapshot.Secondary,preferences);
            status.Text=snapshot==null?state:((DateTime.Now-snapshot.Received).TotalSeconds>Math.Max(120,preferences.RefreshSeconds*2)&&state=="已连接"?"数据已过期":state)+" · "+snapshot.Received.ToString("HH:mm:ss");
            var stale=failures>0||(snapshot!=null&&(DateTime.Now-snapshot.Received).TotalSeconds>Math.Max(120,preferences.RefreshSeconds*2));
            var display=TaskbarDisplay.From(snapshot,preferences,stale,snapshot==null&&failures==0);
            EnsureMeter();meter.Update(snapshot,preferences,stale);
            TaskbarItemInfo.ProgressValue=display.Value;TaskbarItemInfo.ProgressState=display.State;TaskbarItemInfo.Description=display.Description;
            var key=display.Badge+display.State.ToString();if(key!=taskbarIconKey) { Icon=display.MakeIcon();taskbarIconKey=key; }
            if(!taskbarProofWritten&&snapshot!=null&&Array.IndexOf(Environment.GetCommandLineArgs(),"--taskbar-check")>=0)
            {
                taskbarProofWritten=true;
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"taskbar-checks.json"),new JavaScriptSerializer().Serialize(new { minimized=WindowState==WindowState.Minimized,progress=TaskbarItemInfo.ProgressValue,state=TaskbarItemInfo.ProgressState.ToString(),description=TaskbarItemInfo.Description,iconReady=Icon!=null,frameworkOnly=true }));
            }
            if(snapshot!=null&&meter.Attached&&Array.IndexOf(Environment.GetCommandLineArgs(),"--embedded-check")>=0)
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"embedded-checks.json"),new JavaScriptSerializer().Serialize(new { attached=meter.Attached,alphaRendered=meter.AlphaRendered,alphaError=meter.AlphaError,dpiMatched=meter.DpiMatched,renderDpi=meter.RenderDpi,pixelWidth=meter.ClientSize.Width,pixelHeight=meter.ClientSize.Height,mainHidden=!IsVisible,primary=snapshot.Primary==null?-1:snapshot.Primary.Remaining,secondary=snapshot.Secondary==null?-1:snapshot.Secondary.Remaining,side=preferences.TaskbarSide,width=preferences.TaskbarMeterWidth,updatedAt=snapshot.Received.ToString("O") }));
        }
        void EnsureMeter()
        {
            if(meter!=null&&!meter.IsDisposed)return;
            meter=new EmbeddedMeter(preferences);
            System.Windows.Forms.Integration.WindowsFormsHost.EnableWindowsFormsInterop();
            meter.OpenDetails+=delegate { Dispatcher.BeginInvoke(new Action(delegate { if(!closing) { WindowState=WindowState.Normal;Show();Activate(); } }),DispatcherPriority.ContextIdle); };
            meter.OpenSettings+=delegate { QueueSettings(); };
            meter.RefreshRequested+=async delegate { await Refresh(); };
            meter.ExitRequested+=delegate { ExitApplication(); };
            meter.SideChanged+=delegate(string side) { preferences.TaskbarSide=side;preferences.TaskbarOffset=0;try { preferences.Save(); }catch{}Apply(); };
            meter.DockToTaskbar();
        }
        async Task Refresh()
        {
            if(closing||!await mutex.WaitAsync(0))return;refresh.IsEnabled=false;
            try
            {
                bool disconnectOnError=false;
                try
                {
                state="正在刷新";Paint();
                if(client==null||!client.Connected) { await DisconnectClient();client=new Client();client.Updated+=delegate { if(!closing)Dispatcher.BeginInvoke(new Action(delegate { next=DateTime.Now; })); };var connecting=client;var path=preferences.CodexPath;await Task.Run(async delegate { await connecting.Connect(path,lifetime.Token); }); }
                snapshot=Snapshot.Parse(await client.ReadLimits(lifetime.Token));if(string.IsNullOrWhiteSpace(snapshot.Plan))snapshot.Plan=client.Plan;
                failures=0;error.Visibility=Visibility.Collapsed;state="已连接";next=DateTime.Now.AddSeconds(preferences.RefreshSeconds);
                }
            catch(OperationCanceledException) { if(!closing)state="查询已取消"; }
            catch(Exception e) { if(!closing) { state=snapshot==null?"连接失败":"数据已过期";error.Text=e.Message;error.Visibility=Visibility.Visible;failures++;next=DateTime.Now.AddSeconds(Math.Min(300,15*Math.Pow(2,Math.Min(failures-1,4))));disconnectOnError=true; } }
                if(disconnectOnError)await DisconnectClient();
            }
            finally { if(!closing) { refresh.IsEnabled=true;Paint(); }mutex.Release(); }
        }
        void ShowSettings()
        {
            if(closing)return;
            WindowState=WindowState.Normal;if(!IsVisible)Show();Activate();
            if(settingsWindow!=null) { settingsWindow.Topmost=Topmost;settingsWindow.WindowState=WindowState.Normal;settingsWindow.Show();settingsWindow.Activate();settingsWindow.Focus();return; }
            try
            {
            settingsWindow=new SettingsWindow(preferences);settingsWindow.Owner=this;
            settingsWindow.Topmost=Topmost;
            settingsWindow.Saved+=async delegate(Preferences draft)
            {
                var changed=preferences.CodexPath!=draft.CodexPath;preferences=draft;
                try { preferences.Save(); }catch { error.Text="保存失败，请将 EXE 放在可写目录。";error.Visibility=Visibility.Visible; }
                if(changed) { await mutex.WaitAsync();try { await DisconnectClient();snapshot=null; }finally{mutex.Release();} }
                Apply();next=DateTime.Now;
            };
            settingsWindow.Closed+=delegate { settingsWindow=null; };settingsWindow.Show();settingsWindow.Activate();settingsWindow.Focus();
            }
            catch(Exception ex) { settingsWindow=null;error.Text="无法打开设置："+ex.Message;error.Visibility=Visibility.Visible;try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"windows-settings-error.txt"),ex.ToString()); }catch{} }
        }
        void QueueSettings() { if(!closing)Dispatcher.BeginInvoke(new Action(ShowSettings),DispatcherPriority.ContextIdle); }
        async Task DisconnectClient() { var old=client;client=null;if(old!=null) { cleanupTask=Task.Run(delegate { old.Dispose(); });await cleanupTask; } }
        void Close(object sender,CancelEventArgs args)
        {
            if(!exitRequested)
            {
                args.Cancel=true;RememberPosition();if(settingsWindow!=null)settingsWindow.Hide();Hide();return;
            }
            closing=true;timer.Stop();lifetime.Cancel();if(settingsWindow!=null)settingsWindow.Close();
            if(meter!=null)meter.Close();
            RememberPosition();
            if(client!=null)client.Dispose();
            if(cleanupTask!=null)try { cleanupTask.Wait(3000); }catch{}
        }
        void RememberPosition()
        {
            var position=WindowState==WindowState.Normal?new Rect(Left,Top,Width,Height):RestoreBounds;
            if(!position.IsEmpty) { preferences.X=(int)position.Left;preferences.Y=(int)position.Top; }try { preferences.Save(); }catch{}
        }
        public void ExitApplication() { exitRequested=true;Close(); }
        IntPtr LifecycleMessage(IntPtr window,int message,IntPtr wparam,IntPtr lparam,ref bool handled)
        {
            if(message==0x8023) { handled=true;ExitApplication(); }
            if(message==0x8024) { handled=true;Dispatcher.BeginInvoke(new Action(async delegate { await Refresh(); })); }
            if(message==0x8025) { handled=true;QueueSettings(); }
            return IntPtr.Zero;
        }
    }
}
