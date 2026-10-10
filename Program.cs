using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Web.Script.Serialization;
using System.Reflection;
using System.Runtime.Versioning;

[assembly: TargetFramework(".NETFramework,Version=v4.8")]
[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyTitle("Codex Usage Widget · Windows")]

namespace WindowsUsageWidget
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if(args.Length>0&&args[0]=="--check") { Check(Array.IndexOf(args,"--live")>=0,Array.IndexOf(args,"--store-only")>=0).GetAwaiter().GetResult();return; }
            bool first;using(var single=new Mutex(true,"Local\\CodexUsageWidget.Desktop",out first))
            {
                if(!first)return;
                var app=new Application();app.Resources.MergedDictionaries.Add(Styles.Create());app.DispatcherUnhandledException+=delegate(object sender,System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) { try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"windows-startup-error.txt"),e.Exception.ToString()); }catch{} };
                var main=new MainWindow();app.MainWindow=main;app.ShutdownMode=ShutdownMode.OnMainWindowClose;app.SessionEnding+=delegate { main.ExitApplication(); };main.Start();app.Run();
            }
        }
        static async Task Check(bool live,bool storeOnly)
        {
            var results=new List<string>();
            var s=Snapshot.Parse(Json.Parse("{\"rateLimitsByLimitId\":{\"codex\":{\"primary\":{\"usedPercent\":40,\"windowDurationMins\":300,\"resetsAt\":1},\"secondary\":null,\"planType\":\"plus\"}}}"));
            Require(s.Primary.Remaining==60,"remaining percentage",results);Require(s.Secondary==null,"missing quota",results);Require(s.Primary.Countdown()=="等待服务端更新","no invented reset",results);
            var taskbarPreferences=new Preferences();
            var taskbar=TaskbarDisplay.From(s,taskbarPreferences,false,false);
            Require(Math.Abs(taskbar.Value-.6)<.00001&&taskbar.Badge=="60%","taskbar remaining amount",results);
            taskbarPreferences.ShowUsed=true;taskbar=TaskbarDisplay.From(s,taskbarPreferences,false,false);
            Require(Math.Abs(taskbar.Value-.4)<.00001&&taskbar.Badge=="40%","taskbar used amount",results);
            taskbarPreferences.TaskbarQuota="Secondary";taskbarPreferences.ShowUsed=false;
            Require(TaskbarDisplay.From(s,taskbarPreferences,false,false).State==System.Windows.Shell.TaskbarItemProgressState.None,"missing taskbar quota is unavailable",results);
            s.Secondary=new Limit { Used=90,Minutes=10080,Reset=1 };taskbar=TaskbarDisplay.From(s,taskbarPreferences,false,false);
            Require(Math.Abs(taskbar.Value-.1)<.00001&&taskbar.State==System.Windows.Shell.TaskbarItemProgressState.Paused,"weekly quota and low warning",results);
            Require(TaskbarDisplay.From(s,taskbarPreferences,true,false).State==System.Windows.Shell.TaskbarItemProgressState.Error,"stale taskbar quota",results);
            Require(new JavaScriptSerializer().Deserialize<Preferences>("{}").StartMinimized,"legacy settings start in taskbar",results);
            var embedded=new Preferences { TaskbarSide="invalid",TaskbarOffset=2000,TaskbarMeterWidth=1 };embedded.Normalize();
            Require(embedded.TaskbarSide=="Right"&&embedded.TaskbarOffset==1200&&embedded.TaskbarMeterWidth==200,"embedded placement boundaries",results);
            var absolute=new DateTime(2026,10,15,14,27,0,DateTimeKind.Local);
            var reset=new Limit { Reset=(long)(absolute.ToUniversalTime()-new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalSeconds };
            Require(reset.CompactReset(false)=="↻ 14:27"&&reset.CompactReset(true)=="↻ 周四 14:27","compact local reset clock and weekday",results);
            Require(new Limit().CompactReset(true)=="↻ —","unknown reset is unavailable",results);
            var legacy=new Preferences { Material="Acrylic" };legacy.Normalize();Require(legacy.Material=="Solid","legacy acrylic becomes solid",results);
            var p=new Preferences { RefreshSeconds=0,Width=1000,Height=0,AcrylicTintPercent=200 };p.Normalize();Require(p.RefreshSeconds==15&&p.Width==520&&p.Height==390&&p.AcrylicTintPercent==100,"parameter boundaries",results);
            var serializer=new JavaScriptSerializer();Require(serializer.Deserialize<Preferences>(serializer.Serialize(p)).Width==520,"settings serialization",results);
            var assemblies=new List<string>();foreach(var name in Assembly.GetExecutingAssembly().GetReferencedAssemblies())assemblies.Add(name.Name);
            Require(!assemblies.Contains("Microsoft.WindowsAppSDK")&&!assemblies.Contains("Microsoft.WinUI")&&!assemblies.Contains("System.Text.Json"),"system framework only",results);
            string selected="";
            if(live) { using(var c=new Client()) { using(var token=new CancellationTokenSource(60000)) { var path=storeOnly?CliLocator.ResolveStore():"";if(storeOnly) { Require(path.IndexOf("WindowsApps",StringComparison.OrdinalIgnoreCase)>=0,"store detection without npm, PATH or desktop cache",results);Require(CliLocator.Resolve(path)==path,"manual store path precedence",results); }await c.Connect(path,token.Token);selected=c.ExecutablePath;var current=Snapshot.Parse(await c.ReadLimits(token.Token));Require(current.Primary!=null,storeOnly?"store-only live account query":"live account query",results); }c.Dispose();Require(!c.Connected,"owned process exits",results); } }
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,storeOnly?"store-cli-checks.json":"windows-checks.json"),serializer.Serialize(new { ok=true,checks=results,references=assemblies,selectedExecutable=selected,storeStatus=CliLocator.LastStoreStatus,storePackages=CliLocator.LastStorePackages,frameworkAssembly=typeof(Window).Assembly.Location }));
        }
        static void Require(bool condition,string name,List<string> results) { if(!condition)throw new Exception(name);results.Add("PASS "+name); }
    }
}
