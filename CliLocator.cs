using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsUsageWidget
{
    public static class CliLocator
    {
        static readonly object storeGate=new object();
        static List<string> cachedStore=new List<string>();
        static DateTime storeQueried=DateTime.MinValue;
        public static string LastStoreStatus { get; private set; }
        public static string LastStorePackages { get; private set; }
        public static string Resolve(string configured)
        {
            if(!string.IsNullOrWhiteSpace(configured))
            {
                var path=Path.GetFullPath(Environment.ExpandEnvironmentVariables(configured));
                if(File.Exists(path)&&path.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))return path;
                throw new FileNotFoundException("指定的 Codex EXE 不存在，请更新路径或留空使用自动检测。");
            }
            var npm=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"npm","node_modules","@openai","codex");
            var candidates=new List<string> { Path.Combine(npm,"node_modules","@openai","codex-win32-x64","vendor","x86_64-pc-windows-msvc","bin","codex.exe"),Path.Combine(npm,"vendor","x86_64-pc-windows-msvc","bin","codex.exe") };
            foreach(var folder in (Environment.GetEnvironmentVariable("PATH")??"").Split(Path.PathSeparator))
                if(!string.IsNullOrWhiteSpace(folder))try { candidates.Add(Path.Combine(Environment.ExpandEnvironmentVariables(folder.Trim().Trim('"')),"codex.exe")); }catch(ArgumentException){}
            return ResolveAutomatic(candidates);
        }
        public static string ResolveAutomatic(IEnumerable<string> ordinaryCandidates)
        {
            foreach(var candidate in ordinaryCandidates)if(File.Exists(candidate)&&SupportsAppServer(candidate))return candidate;
            var desktop=ResolveDesktopCache(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OpenAI","Codex","bin"));
            if(desktop!=null)return desktop;
            return ResolveStore();
        }
        public static string ResolveDesktopCache(string root)
        {
            foreach(var candidate in DesktopCacheCandidates(root))if(SupportsAppServer(candidate))return candidate;
            return null;
        }
        public static List<string> DesktopCacheCandidates(string root)
        {
            var candidates=new List<KeyValuePair<string,DateTime>>();
            var result=new List<string>();if(string.IsNullOrWhiteSpace(root))return result;
            try
            {
                // Only inspect direct build folders; the folder identifier is not a version.
                foreach(var folder in Directory.GetDirectories(root))
                    try
                    {
                        var path=Path.Combine(folder,"codex.exe");
                        if(File.Exists(path))candidates.Add(new KeyValuePair<string,DateTime>(path,File.GetLastWriteTimeUtc(path)));
                    }
                    catch(IOException){}catch(UnauthorizedAccessException){}catch(System.Security.SecurityException){}
            }
            catch(IOException){}catch(UnauthorizedAccessException){}catch(System.Security.SecurityException){}
            candidates.Sort(delegate(KeyValuePair<string,DateTime> a,KeyValuePair<string,DateTime> b) { var order=b.Value.CompareTo(a.Value);return order!=0?order:StringComparer.OrdinalIgnoreCase.Compare(a.Key,b.Key); });
            foreach(var candidate in candidates)result.Add(candidate.Key);
            return result;
        }
        public static string ResolveStore()
        {
            foreach(var candidate in StoreCandidates())if(SupportsAppServer(candidate))return candidate;
            throw new FileNotFoundException("未找到可调用的 Codex 后台程序。"+(LastStoreStatus??"")+" 若商店包不含 Codex 工作组件，请安装 Codex CLI 或在设置指定路径。");
        }
        public static List<string> PackageCandidates(string root)
        {
            var result=new List<string>();if(string.IsNullOrWhiteSpace(root))return result;
            foreach(var relative in new[] { "app/resources/codex.exe","resources/codex.exe","app/codex.exe","codex.exe","app/resources/bin/codex.exe","resources/bin/codex.exe" })
            {
                var path=Path.Combine(root,relative.Replace('/',Path.DirectorySeparatorChar));if(File.Exists(path))result.Add(path);
            }
            return result;
        }
        static List<string> StoreCandidates()
        {
            lock(storeGate)
            {
                if((DateTime.UtcNow-storeQueried).TotalMinutes<2&&cachedStore.Exists(File.Exists))return new List<string>(cachedStore);
                storeQueried=DateTime.UtcNow;cachedStore.Clear();LastStoreStatus="";LastStorePackages="";
                try
                {
                    var packages=new List<StorePackage>();var errors=new List<string>();
                    foreach(var family in new[] { "OpenAI.Codex_2p2nqsd0c76g0","OpenAI.ChatGPT-Desktop_2p2nqsd0c76g0","OpenAI.ChatGPT_2p2nqsd0c76g0" })
                    {
                        try
                        {
                            foreach(var fullName in NativePackageNames(family))
                                try { packages.Add(ReadNativePackage(fullName)); }
                                catch(Win32Exception e) { errors.Add(fullName+": "+e.NativeErrorCode); }
                        }
                        catch(Win32Exception e) { errors.Add(family+": "+e.NativeErrorCode); }
                    }
                    packages.Sort(delegate(StorePackage a,StorePackage b) { return b.Version.CompareTo(a.Version); });
                    var descriptions=new List<string>();
                    foreach(var package in packages)
                    {
                        descriptions.Add(package.FullName+" => "+package.Root);
                        foreach(var candidate in PackageCandidates(package.Root))if(!cachedStore.Contains(candidate))cachedStore.Add(candidate);
                    }
                    LastStorePackages=string.Join("; ",descriptions.ToArray());
                    LastStoreStatus=cachedStore.Count>0?"原生接口已发现商店后台程序。":packages.Count>0?"已查询商店包，但已知位置的 codex.exe 不存在或不可访问。":"当前用户未注册已知的官方 ChatGPT／Codex 商店包。";
                    if(errors.Count>0)LastStoreStatus+=" 包查询错误码："+string.Join("; ",errors.ToArray());
                }
                catch(Exception e) { LastStoreStatus="原生商店包检测失败（"+e.GetType().Name+"），可在设置中指定包内 codex.exe 路径。"; }
                return new List<string>(cachedStore);
            }
        }
        sealed class StorePackage { public string FullName,Root;public ulong Version; }
        [StructLayout(LayoutKind.Sequential)]
        struct PackageId
        {
            public uint Reserved,Architecture;
            public ulong Version;
            public IntPtr Name,Publisher,ResourceId,PublisherId;
        }
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]
        static extern int GetPackagesByPackageFamily(string family,ref uint count,IntPtr names,ref uint length,IntPtr buffer);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]
        static extern int PackageIdFromFullName(string fullName,uint flags,ref uint length,IntPtr buffer);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]
        static extern int GetPackagePathByFullName(string fullName,ref uint length,IntPtr path);
        static List<string> NativePackageNames(string family)
        {
            var result=new List<string>();
            // Package registration can change during a Store update; retry buffer sizing.
            for(var attempt=0;attempt<3;attempt++)
            {
                uint count=0,length=0;var code=GetPackagesByPackageFamily(family,ref count,IntPtr.Zero,ref length,IntPtr.Zero);
                if(code==0&&count==0)return result;
                if(code!=122&&code!=0)throw new Win32Exception(code);
                var names=Marshal.AllocHGlobal(checked((int)count*IntPtr.Size));
                var buffer=IntPtr.Zero;
                try
                {
                    buffer=Marshal.AllocHGlobal(checked((int)length*2));
                    code=GetPackagesByPackageFamily(family,ref count,names,ref length,buffer);
                    if(code==122)continue;
                    if(code!=0)throw new Win32Exception(code);
                    for(var i=0;i<count;i++)result.Add(Marshal.PtrToStringUni(Marshal.ReadIntPtr(names,i*IntPtr.Size)));
                    return result;
                }
                finally { Marshal.FreeHGlobal(names);if(buffer!=IntPtr.Zero)Marshal.FreeHGlobal(buffer); }
            }
            throw new Win32Exception(122);
        }
        static StorePackage ReadNativePackage(string fullName)
        {
            uint length=0;var code=PackageIdFromFullName(fullName,0,ref length,IntPtr.Zero);
            if(code!=122)throw new Win32Exception(code);
            var buffer=Marshal.AllocHGlobal(checked((int)length));ulong version;
            try
            {
                code=PackageIdFromFullName(fullName,0,ref length,buffer);if(code!=0)throw new Win32Exception(code);
                version=((PackageId)Marshal.PtrToStructure(buffer,typeof(PackageId))).Version;
            }
            finally { Marshal.FreeHGlobal(buffer); }
            length=0;code=GetPackagePathByFullName(fullName,ref length,IntPtr.Zero);
            if(code!=122)throw new Win32Exception(code);
            buffer=Marshal.AllocHGlobal(checked((int)length*2));
            try
            {
                code=GetPackagePathByFullName(fullName,ref length,buffer);if(code!=0)throw new Win32Exception(code);
                return new StorePackage { FullName=fullName,Root=Marshal.PtrToStringUni(buffer),Version=version };
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
        static bool SupportsAppServer(string executable)
        {
            try
            {
                var start=new ProcessStartInfo(executable,"app-server --help") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8,WorkingDirectory=Path.GetDirectoryName(executable) };
                using(var process=Process.Start(start))
                {
                    var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
                    if(!process.WaitForExit(8000)) { process.Kill();process.WaitForExit(2000);return false; }
                    var text=output.GetAwaiter().GetResult()+error.GetAwaiter().GetResult();return process.ExitCode==0&&text.IndexOf("app-server",StringComparison.OrdinalIgnoreCase)>=0;
                }
            }
            catch { return false; }
        }
    }
}
