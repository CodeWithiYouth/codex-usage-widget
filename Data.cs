using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace WindowsUsageWidget
{
    public class Preferences
    {
        public int RefreshSeconds { get; set; }
        public int WarningPercent { get; set; }
        public bool AlwaysOnTop { get; set; }
        public bool ShowUsed { get; set; }
        public bool StartMinimized { get; set; }
        public string TaskbarQuota { get; set; }
        public string TaskbarSide { get; set; }
        public int TaskbarOffset { get; set; }
        public int TaskbarMeterWidth { get; set; }
        public string TaskbarPrimaryLabel { get; set; }
        public string TaskbarSecondaryLabel { get; set; }
        public int TaskbarFontSize { get; set; }
        public string TaskbarStyle { get; set; }
        public string TaskbarPrimaryColor { get; set; }
        public string TaskbarSecondaryColor { get; set; }
        public string TaskbarTextColor { get; set; }
        public string TaskbarBackgroundColor { get; set; }
        public string TaskbarWarningColor { get; set; }
        public string TaskbarMediumColor { get; set; }
        public int TaskbarMediumPercent { get; set; }
        public string TaskbarStaleColor { get; set; }
        public int TaskbarBackgroundOpacity { get; set; }
        public string Theme { get; set; }
        public string Material { get; set; }
        public int AcrylicTintPercent { get; set; }
        public int OpacityPercent { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string CodexPath { get; set; }
        public int? X { get; set; }
        public int? Y { get; set; }
        public Preferences() { RefreshSeconds=60; WarningPercent=20; AlwaysOnTop=true; StartMinimized=true; TaskbarQuota="Primary"; TaskbarSide="Right"; TaskbarMeterWidth=220; TaskbarPrimaryLabel="5小时{mode}";TaskbarSecondaryLabel="每周{mode}";TaskbarFontSize=12;TaskbarStyle="Rounded";TaskbarPrimaryColor="Auto";TaskbarSecondaryColor="Auto";TaskbarTextColor="Auto";TaskbarBackgroundColor="Auto";TaskbarWarningColor="#EBB048";TaskbarStaleColor="#E15C5C";TaskbarMediumColor="Auto";TaskbarMediumPercent=50;Theme="System"; Material="Solid"; AcrylicTintPercent=18; OpacityPercent=100; Width=360; Height=390; CodexPath=""; }
        public static string FilePath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json"); } }
        public void Normalize() { RefreshSeconds=Clamp(RefreshSeconds,15,3600); WarningPercent=Clamp(WarningPercent,0,100); AcrylicTintPercent=Clamp(AcrylicTintPercent,0,100); OpacityPercent=Clamp(OpacityPercent,50,100); Width=Clamp(Width,320,520); Height=Clamp(Height,390,650); TaskbarMeterWidth=Clamp(TaskbarMeterWidth,200,380);TaskbarOffset=Clamp(TaskbarOffset,-1200,1200);if(TaskbarSide!="Left")TaskbarSide="Right"; if(TaskbarQuota!="Secondary")TaskbarQuota="Primary"; if(Theme!="Dark"&&Theme!="Light")Theme="System"; if(Material!="Transparent")Material="Solid";if(Array.IndexOf(new[] { "Rounded","Segments","Cards","Dots","Gradient","Rings","Badges" },TaskbarStyle)<0)TaskbarStyle="Rounded";TaskbarBackgroundOpacity=Clamp(TaskbarBackgroundOpacity,0,100);TaskbarPrimaryColor=NormalizeColor(TaskbarPrimaryColor,"Auto");TaskbarSecondaryColor=NormalizeColor(TaskbarSecondaryColor,"Auto");TaskbarTextColor=NormalizeColor(TaskbarTextColor,"Auto");TaskbarBackgroundColor=NormalizeColor(TaskbarBackgroundColor,"Auto");TaskbarWarningColor=NormalizeColor(TaskbarWarningColor,"#EBB048");TaskbarStaleColor=NormalizeColor(TaskbarStaleColor,"#E15C5C");TaskbarMediumColor=NormalizeColor(TaskbarMediumColor,"Auto");TaskbarMediumPercent=Clamp(TaskbarMediumPercent,WarningPercent,100);TaskbarFontSize=Clamp(TaskbarFontSize,10,14);TaskbarPrimaryLabel=NormalizeLabel(TaskbarPrimaryLabel,"5小时{mode}");TaskbarSecondaryLabel=NormalizeLabel(TaskbarSecondaryLabel,"每周{mode}"); }
        static string NormalizeLabel(string value,string fallback)
        {
            if(value==null)return fallback;
            value=value.Replace("\r"," ").Replace("\n"," ").Replace("\t"," ").Trim();
            var elements=System.Globalization.StringInfo.ParseCombiningCharacters(value);
            return elements.Length>24?value.Substring(0,elements[24]):value;
        }
        public string TaskbarLabel(bool weekly) { return (weekly?TaskbarSecondaryLabel:TaskbarPrimaryLabel).Replace("{mode}",ShowUsed?"用":"余"); }
        public static bool IsColor(string value)
        {
            if(string.Equals(value,"Auto",StringComparison.OrdinalIgnoreCase))return true;
            int rgb;return value!=null&&value.Length==7&&value[0]=='#'&&int.TryParse(value.Substring(1),System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture,out rgb)&&rgb>=0&&rgb<=0xFFFFFF;
        }
        static string NormalizeColor(string value,string fallback) { return IsColor(value)?string.Equals(value,"Auto",StringComparison.OrdinalIgnoreCase)?"Auto":value.ToUpperInvariant():fallback; }
        static int Clamp(int n,int lo,int hi) { return Math.Min(hi,Math.Max(lo,n)); }
        public static Preferences Load() { try { var p=new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(FilePath)); p.Normalize();return p; } catch { return new Preferences(); } }
        public void Save() { Normalize();var temp=FilePath+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(this),new UTF8Encoding(false));if(File.Exists(FilePath))File.Replace(temp,FilePath,null);else File.Move(temp,FilePath); }
        public Preferences Copy() { var serializer=new JavaScriptSerializer();return serializer.Deserialize<Preferences>(serializer.Serialize(this)); }
    }
    public static class Json
    {
        public static object Get(IDictionary<string,object> value,string key) { object result;return value!=null&&value.TryGetValue(key,out result)?result:null; }
        public static IDictionary<string,object> Map(object value) { return value as IDictionary<string,object>; }
        public static string Text(IDictionary<string,object> value,string key) { return Get(value,key) as string??""; }
        public static double Number(IDictionary<string,object> value,string key,double fallback) { var v=Get(value,key);if(v==null)return fallback;try{return Convert.ToDouble(v);}catch{return fallback;} }
        public static IDictionary<string,object> Parse(string text) { return new JavaScriptSerializer { MaxJsonLength=4*1024*1024 }.Deserialize<Dictionary<string,object>>(text); }
    }
    public class Limit
    {
        public double Used;
        public int Minutes;
        public long Reset;
        public double Remaining { get { return Math.Max(0,Math.Min(100,100-Used)); } }
        public static Limit Parse(IDictionary<string,object> bucket,string key)
        {
            var v=Json.Map(Json.Get(bucket,key));var n=Json.Number(v,"usedPercent",double.NaN);
            if(double.IsNaN(n)||double.IsInfinity(n))return null;
            return new Limit { Used=Math.Max(0,Math.Min(100,n)),Minutes=(int)Json.Number(v,"windowDurationMins",0),Reset=(long)Json.Number(v,"resetsAt",0) };
        }
        public string Label { get { return Minutes==10080?"每周额度":Minutes>0&&Minutes%60==0?(Minutes/60)+" 小时额度":Minutes>0?Minutes+" 分钟额度":"额度窗口"; } }
        public string Countdown()
        {
            if(Reset<=0)return "重置时间未知";
            var left=Reset-(long)(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds;
            if(left<=0)return "等待服务端更新";
            var t=TimeSpan.FromSeconds(left);
            var relative=t.TotalDays>=1?((int)t.TotalDays)+"天 "+t.Hours+"小时后重置":((int)t.TotalHours)+"小时 "+t.Minutes+"分后重置";
            var absolute=new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddSeconds(Reset).ToLocalTime();
            var format=absolute.Date==DateTime.Today?"今天 HH:mm":absolute.Year==DateTime.Today.Year?"MM-dd HH:mm":"yyyy-MM-dd HH:mm";
            return relative+"（"+absolute.ToString(format)+"）";
        }
        public string CompactReset(bool weekly)
        {
            if(Reset<=0)return "↻ —";
            var absolute=new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddSeconds(Reset).ToLocalTime();
            var time=absolute.ToString("HH:mm",System.Globalization.CultureInfo.InvariantCulture);
            return "↻ "+(weekly?"周"+"日一二三四五六"[(int)absolute.DayOfWeek]+" ":"")+time;
        }
    }
    public class Snapshot
    {
        public Limit Primary,Secondary;
        public string Plan;
        public DateTime Received;
        public static Snapshot Parse(IDictionary<string,object> result)
        {
            var buckets=Json.Map(Json.Get(result,"rateLimitsByLimitId"));
            var bucket=Json.Map(Json.Get(buckets,"codex"))??Json.Map(Json.Get(result,"rateLimits"));
            if(bucket==null)throw new InvalidDataException("服务端未返回 Codex 额度。");
            return new Snapshot { Primary=Limit.Parse(bucket,"primary"),Secondary=Limit.Parse(bucket,"secondary"),Plan=Json.Text(bucket,"planType"),Received=DateTime.Now };
        }
    }
    public class Client : IDisposable
    {
        Process process;
        readonly object processGate=new object();
        bool disposed;
        Task readerTask,errorTask;
        int sequence;
        readonly SemaphoreSlim writes=new SemaphoreSlim(1,1);
        readonly ConcurrentDictionary<int,TaskCompletionSource<IDictionary<string,object>>> pending=new ConcurrentDictionary<int,TaskCompletionSource<IDictionary<string,object>>>();
        public event Action Updated;
        public string Plan="";
        public string ExecutablePath { get; private set; }
        public bool Connected { get { var owned=process;if(owned==null)return false;try { return !owned.HasExited; }catch(InvalidOperationException) { return false; } } }
        public static string Resolve(string path)
        {
            return CliLocator.Resolve(path);
        }
        public async Task Connect(string path,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();var executable=Resolve(path);token.ThrowIfCancellationRequested();
            ExecutablePath=executable;
            var start=new ProcessStartInfo(executable,"app-server") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8,WorkingDirectory=AppDomain.CurrentDomain.BaseDirectory };
            Process owned;lock(processGate) { if(disposed)throw new ObjectDisposedException("Client");token.ThrowIfCancellationRequested();process=Process.Start(start);owned=process; }
            readerTask=Task.Run(async delegate { await Read(owned); });
            errorTask=Task.Run(async delegate { try { while(await owned.StandardError.ReadLineAsync()!=null){} } catch{} });
            await Request("initialize",new { clientInfo=new { name="codex_usage_widget",title="Codex Usage Widget",version="2.0.0" } },token);
            await Send(new { method="initialized",@params=new {} },token);
            var result=await Request("account/read",new { refreshToken=false },token);
            var account=Json.Map(Json.Get(result,"account"));
            if(account==null)throw new InvalidOperationException("已找到 Codex 后台程序，但尚未登录。请先在商店应用的 Codex 工作模式中登录，或使用 Codex CLI 登录。");
            if(Json.Text(account,"type")=="apiKey")throw new InvalidOperationException("套餐用量需要 ChatGPT 账号登录。");
            Plan=Json.Text(account,"planType");
        }
        public Task<IDictionary<string,object>> ReadLimits(CancellationToken token) { return Request("account/rateLimits/read",new {},token); }
        async Task<IDictionary<string,object>> Request(string method,object parameters,CancellationToken token)
        {
            var id=Interlocked.Increment(ref sequence);var source=new TaskCompletionSource<IDictionary<string,object>>(TaskCreationOptions.RunContinuationsAsynchronously);pending[id]=source;
            try { await Send(new { id=id,method=method,@params=parameters },token);var deadline=Task.Delay(25000,token);if(await Task.WhenAny(source.Task,deadline)!=source.Task) { token.ThrowIfCancellationRequested();throw new TimeoutException("请求超时，将自动重试。"); }return await source.Task; }
            finally { TaskCompletionSource<IDictionary<string,object>> removed;pending.TryRemove(id,out removed); }
        }
        async Task Send(object message,CancellationToken token)
        {
            await writes.WaitAsync(token);
            try { if(!Connected)throw new IOException("后台连接已断开。");var text=new JavaScriptSerializer().Serialize(message)+"\n";var bytes=new UTF8Encoding(false).GetBytes(text);await process.StandardInput.BaseStream.WriteAsync(bytes,0,bytes.Length,token);await process.StandardInput.BaseStream.FlushAsync(token); }
            finally { writes.Release(); }
        }
        async Task Read(Process owned)
        {
            try
            {
                string line;
                while((line=await owned.StandardOutput.ReadLineAsync())!=null)
                {
                    IDictionary<string,object> data;try { data=Json.Parse(line); }catch{continue;}
                    var id=(int)Json.Number(data,"id",-1);TaskCompletionSource<IDictionary<string,object>> source;
                    if(pending.TryGetValue(id,out source)) { if(Json.Get(data,"error")!=null)source.TrySetException(new IOException("用量查询失败，请检查网络、账号或 CLI 版本。"));else source.TrySetResult(Json.Map(Json.Get(data,"result"))); }
                    else if(Json.Text(data,"method")=="account/rateLimits/updated") { var handler=Updated;if(handler!=null)handler(); }
                }
            }
            catch(IOException){}catch(ObjectDisposedException){}
            finally { foreach(var source in pending.Values)source.TrySetException(new IOException("后台连接已断开。")); }
        }
        public void Dispose()
        {
            Process owned;lock(processGate) { disposed=true;owned=process;process=null; }if(owned==null)return;
            try { if(!owned.HasExited) { owned.StandardInput.Close();if(!owned.WaitForExit(2000)) { owned.Kill();owned.WaitForExit(2000); } } }catch{}finally{owned.Dispose();}
            if(readerTask!=null&&readerTask.IsFaulted)GC.KeepAlive(readerTask.Exception);
            if(errorTask!=null&&errorTask.IsFaulted)GC.KeepAlive(errorTask.Exception);
        }
    }
}
