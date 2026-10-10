using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WindowsUsageWidget
{
    public class SettingsWindow : Window
    {
        public event Action<Preferences> Saved;
        public SettingsWindow(Preferences original)
        {
            Title="Codex · 参数设置";WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;Width=420;Height=Math.Min(670,SystemParameters.WorkArea.Height-24);WindowStartupLocation=WindowStartupLocation.CenterOwner;FontFamily=new FontFamily("Segoe UI");
            var draft=original.Copy();var root=new Grid();root.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });root.RowDefinitions.Add(new RowDefinition());root.Children.Add(UI.Chrome(this));
            var body=new Grid { Margin=new Thickness(24,12,24,24) };body.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });body.RowDefinitions.Add(new RowDefinition());body.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            var title=UI.Text("参数设置",24);title.FontWeight=FontWeights.SemiBold;title.Margin=new Thickness(0,0,0,18);body.Children.Add(title);
            var panel=new StackPanel();var scroll=new ScrollViewer { Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(0,0,8,0) };Grid.SetRow(scroll,1);body.Children.Add(scroll);
            var saveAppearance=AppearanceControls(panel,draft);
            var material=Choice(new[] { "实色 · 清爽主题","轻透明 · 整窗淡化" },original.Material=="Transparent"?1:0);Add(panel,"窗口样式",material);
            var theme=Choice(new[] { "跟随系统","浅色","深色" },original.Theme=="Light"?1:original.Theme=="Dark"?2:0);Add(panel,"主题",theme);
            var top=new CheckBox { Content="始终置顶",IsChecked=original.AlwaysOnTop,FontSize=13,Margin=new Thickness(0,8,0,18) };panel.Children.Add(top);
            var minimized=new CheckBox { Content="启动时不展开详细窗口",IsChecked=original.StartMinimized,FontSize=13,Margin=new Thickness(0,0,0,18) };panel.Children.Add(minimized);
            var side=Choice(new[] { "任务栏右侧 · 系统托盘左边","任务栏左侧" },original.TaskbarSide=="Left"?1:0);Add(panel,"任务栏嵌入位置",side);
            var meterWidth=Input(original.TaskbarMeterWidth.ToString());Add(panel,"嵌入区域宽度（200–380）",meterWidth);
            var offset=Input(original.TaskbarOffset.ToString());Add(panel,"水平偏移（-1200–1200）",offset);
            var interval=Input(original.RefreshSeconds.ToString());Add(panel,"刷新间隔（15–3600 秒）",interval);
            var threshold=Input(original.WarningPercent.ToString());Add(panel,"低余量阈值（0–100%）",threshold);
            var width=Input(original.Width.ToString());Add(panel,"主窗口宽度（320–520）",width);
            var height=Input(original.Height.ToString());Add(panel,"主窗口高度（390–650）",height);
            var opacity=new Slider { Minimum=50,Maximum=100,Value=original.OpacityPercent,TickFrequency=5,IsSnapToTickEnabled=true,Height=30,IsEnabled=original.Material=="Transparent" };Add(panel,"整窗不透明度（仅透明模式）",opacity);
            var path=Input(original.CodexPath);Add(panel,"Codex 路径（留空自动检测）",path);
            material.SelectionChanged+=delegate { opacity.IsEnabled=material.SelectedIndex==1; };
            var footer=new StackPanel { Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,16,0,0) };
            var cancel=new Button { Content="取消",Width=88,Height=34,Margin=new Thickness(0,0,10,0) };
            var saveText=UI.Text("保存",13);saveText.Foreground=UI.Brush(239,246,248);
            var save=new Button { Content=saveText,Width=88,Height=34,Background=UI.Brush(0,104,168),Foreground=UI.Brush(239,246,248),BorderThickness=new Thickness(0) };
            footer.Children.Add(cancel);footer.Children.Add(save);Grid.SetRow(footer,2);body.Children.Add(footer);Grid.SetRow(body,1);root.Children.Add(body);Content=root;
            cancel.Click+=delegate { Close(); };
            save.Click+=delegate
            {
                int seconds,warn,w,h,mw,off;
                if(!int.TryParse(interval.Text,out seconds)||!int.TryParse(threshold.Text,out warn)||!int.TryParse(width.Text,out w)||!int.TryParse(height.Text,out h)||!int.TryParse(meterWidth.Text,out mw)||!int.TryParse(offset.Text,out off)) { MessageBox.Show(this,"请输入有效的整数参数。","参数设置");return; }
                if(!saveAppearance()) { MessageBox.Show(this,"颜色请填写 #RRGGBB，例如 #68DBC5；或填写 Auto 跟随系统。","颜色格式");return; }
                if(draft.TaskbarMediumPercent<Math.Max(0,Math.Min(100,warn))) { MessageBox.Show(this,"中余量阈值不能小于低余量阈值。","余量阈值");return; }
                draft.Material=material.SelectedIndex==1?"Transparent":"Solid";
                draft.Theme=theme.SelectedIndex==1?"Light":theme.SelectedIndex==2?"Dark":"System";
                draft.RefreshSeconds=seconds;draft.WarningPercent=warn;draft.Width=w;draft.Height=h;draft.OpacityPercent=(int)opacity.Value;draft.AlwaysOnTop=top.IsChecked==true;draft.CodexPath=path.Text.Trim();draft.Normalize();
                draft.StartMinimized=minimized.IsChecked==true;draft.TaskbarSide=side.SelectedIndex==1?"Left":"Right";draft.TaskbarMeterWidth=mw;draft.TaskbarOffset=off;draft.Normalize();
                var handler=Saved;if(handler!=null)handler(draft);Close();
            };
            SourceInitialized+=delegate { Native.Apply(this,root,original); };
        }
        Func<bool> AppearanceControls(Panel panel,Preferences draft)
        {
            var heading=UI.Text("任务栏外观",16);heading.FontWeight=FontWeights.SemiBold;heading.Margin=new Thickness(0,0,0,12);panel.Children.Add(heading);
            var modeGrid=new Grid();modeGrid.ColumnDefinitions.Add(new ColumnDefinition());modeGrid.ColumnDefinitions.Add(new ColumnDefinition());
            var group="QuotaMode"+Guid.NewGuid().ToString("N");
            var remainingChoice=new RadioButton { Name="QuotaRemaining",Content="剩余",GroupName=group,IsChecked=!draft.ShowUsed,Style=(Style)FindResource("SegmentOption") };
            var used=new RadioButton { Name="QuotaUsed",Content="已用",GroupName=group,IsChecked=draft.ShowUsed,Style=(Style)FindResource("SegmentOption") };
            modeGrid.Children.Add(remainingChoice);Grid.SetColumn(used,1);modeGrid.Children.Add(used);
            var switchFrame=new Border { Child=modeGrid,CornerRadius=new CornerRadius(9),Padding=new Thickness(3),BorderThickness=new Thickness(1) };switchFrame.SetResourceReference(Border.BackgroundProperty,"FormFill");switchFrame.SetResourceReference(Border.BorderBrushProperty,"FormStroke");Add(panel,"显示方式",switchFrame);
            var styleIds=new[] { "Rounded","Segments","Cards","Dots","Gradient","Rings","Badges" };
            var style=Choice(new[] { "圆角进度条","分段进度条","双行卡片","点阵进度","柔和渐变","环形仪表","数字胶囊" },Math.Max(0,Array.IndexOf(styleIds,draft.TaskbarStyle)));style.Name="TaskbarStyle";Add(panel,"样式",style);
            var labels=new Grid { Margin=new Thickness(0,0,0,6) };labels.ColumnDefinitions.Add(new ColumnDefinition());labels.ColumnDefinitions.Add(new ColumnDefinition());
            var firstLabel=Input(draft.TaskbarPrimaryLabel);firstLabel.Name="TaskbarPrimaryLabel";
            var secondLabel=Input(draft.TaskbarSecondaryLabel);secondLabel.Name="TaskbarSecondaryLabel";
            for(var column=0;column<2;column++) { var stack=new StackPanel { Margin=column==0?new Thickness(0,0,7,0):new Thickness(7,0,0,0) };Add(stack,column==0?"5 小时前缀":"每周前缀",column==0?firstLabel:secondLabel);Grid.SetColumn(stack,column);labels.Children.Add(stack); }panel.Children.Add(labels);
            var labelNote=UI.Text("留空隐藏；{mode} 自动切换为“余 / 用”。",11);labelNote.Opacity=.65;labelNote.Margin=new Thickness(0,0,0,14);panel.Children.Add(labelNote);
            var fontSize=new Slider { Name="TaskbarFontSize",Minimum=10,Maximum=14,Value=draft.TaskbarFontSize,TickFrequency=1,IsSnapToTickEnabled=true,Height=26 };
            var fontLabel=UI.Text("",12);fontLabel.Margin=new Thickness(0,0,0,6);panel.Children.Add(fontLabel);fontSize.Margin=new Thickness(0,0,0,14);panel.Children.Add(fontSize);
            var image=new System.Windows.Controls.Image { Height=42,Stretch=Stretch.None,HorizontalAlignment=HorizontalAlignment.Center };
            var frame=new Border { Child=image,CornerRadius=new CornerRadius(10),Padding=new Thickness(8,10,8,10),Margin=new Thickness(0,0,0,8) };panel.Children.Add(frame);
            var darkPreview=new CheckBox { Name="TaskbarPreviewDark",Content="深色任务栏预览 · 示例用量",IsChecked=UI.Dark(draft),FontSize=11,Margin=new Thickness(0,0,0,18) };panel.Children.Add(darkPreview);
            var previewState=Choice(new[] { "示例用量","中余量","低余量","数据过期","未连接" },0);previewState.Name="TaskbarPreviewState";Add(panel,"预览状态",previewState);
            var palette=Choice(new[] { "自定义","跟随系统","薄荷与靛蓝","海蓝","暮紫","暖橙" },0);palette.Name="TaskbarPalette";Add(panel,"配色预设",palette);
            var fields=new System.Collections.Generic.List<TextBox>();
            var values=new[] { draft.TaskbarPrimaryColor,draft.TaskbarSecondaryColor,draft.TaskbarTextColor,draft.TaskbarBackgroundColor,draft.TaskbarWarningColor,draft.TaskbarStaleColor,draft.TaskbarMediumColor };
            var names=new[] { "5 小时颜色","每周颜色","文字颜色","背景颜色","低余量颜色","过期数据颜色","中余量颜色" };
            var medium=new Slider { Name="TaskbarMediumThreshold",Minimum=0,Maximum=100,Value=draft.TaskbarMediumPercent,TickFrequency=5,IsSnapToTickEnabled=true,Height=34 };
            var mediumLabel=UI.Text("中余量阈值 · "+draft.TaskbarMediumPercent+"%",12);mediumLabel.Margin=new Thickness(0,0,0,6);
            for(var row=0;row<4;row++)
            {
                var grid=new Grid { Margin=new Thickness(0,0,0,12) };grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition());
                for(var col=0;col<2;col++)
                {
                    var index=row*2+col;var stack=new StackPanel { Margin=col==0?new Thickness(0,0,7,0):new Thickness(7,0,0,0) };
                    if(index==7) { stack.Children.Add(mediumLabel);stack.Children.Add(medium);Grid.SetColumn(stack,col);grid.Children.Add(stack);continue; }
                    var label=UI.Text(names[index],12);label.Margin=new Thickness(0,0,0,6);stack.Children.Add(label);
                    var input=Input(values[index]);input.Name="TaskbarColor"+index;input.FontSize=12;input.Padding=new Thickness(8,0,4,0);fields.Add(input);
                    var pick=UI.Icon("\uE790","选择"+names[index]);pick.Width=30;pick.Height=34;pick.Margin=new Thickness(4,0,0,0);
                    var swatch=new Border { Width=8,Height=18,CornerRadius=new CornerRadius(3),Margin=new Thickness(5,0,0,0) };
                    var right=new StackPanel { Orientation=Orientation.Horizontal };right.Children.Add(swatch);right.Children.Add(pick);stack.Children.Add(UI.TwoColumns(input,right));
                    Action paintSwatch=delegate { var color=MeterRenderer.ColorValue(input.Text,System.Drawing.Color.FromArgb(128,145,156));swatch.Background=UI.Brush(color.R,color.G,color.B); };paintSwatch();input.TextChanged+=delegate { paintSwatch(); };
                    pick.Click+=delegate
                    {
                        using(var dialog=new System.Windows.Forms.ColorDialog { FullOpen=true,Color=MeterRenderer.ColorValue(input.Text,System.Drawing.Color.FromArgb(104,219,197)) })
                            if(dialog.ShowDialog(new DialogOwner(new System.Windows.Interop.WindowInteropHelper(this).Handle))==System.Windows.Forms.DialogResult.OK)
                                input.Text="#"+dialog.Color.R.ToString("X2")+dialog.Color.G.ToString("X2")+dialog.Color.B.ToString("X2");
                    };
                    Grid.SetColumn(stack,col);grid.Children.Add(stack);
                }
                panel.Children.Add(grid);
            }
            var note=UI.Text("Auto 跟随系统；也可输入 #RRGGBB 或点右侧选色。",11);note.Opacity=.65;note.Margin=new Thickness(0,0,0,14);panel.Children.Add(note);
            var rule=UI.Text("按剩余量分档：低余量 → 中余量 → 正常；过期优先。",11);rule.Opacity=.65;rule.TextWrapping=TextWrapping.Wrap;rule.Margin=new Thickness(0,0,0,14);panel.Children.Add(rule);
            var opacity=new Slider { Minimum=0,Maximum=100,Value=draft.TaskbarBackgroundOpacity,TickFrequency=5,IsSnapToTickEnabled=true,Height=26 };opacity.Name="TaskbarBackgroundOpacity";
            var opacityLabel=UI.Text("",12);opacityLabel.Margin=new Thickness(0,0,0,6);panel.Children.Add(opacityLabel);opacity.Margin=new Thickness(0,0,0,20);panel.Children.Add(opacity);
            Action<Preferences> read=delegate(Preferences p)
            {
                p.TaskbarStyle=styleIds[Math.Max(0,style.SelectedIndex)];
                p.ShowUsed=used.IsChecked==true;p.TaskbarPrimaryLabel=firstLabel.Text;p.TaskbarSecondaryLabel=secondLabel.Text;p.TaskbarFontSize=(int)fontSize.Value;
                p.TaskbarPrimaryColor=fields[0].Text.Trim();p.TaskbarSecondaryColor=fields[1].Text.Trim();p.TaskbarTextColor=fields[2].Text.Trim();p.TaskbarBackgroundColor=fields[3].Text.Trim();p.TaskbarWarningColor=fields[4].Text.Trim();p.TaskbarStaleColor=fields[5].Text.Trim();p.TaskbarBackgroundOpacity=(int)opacity.Value;
                p.TaskbarMediumColor=fields[6].Text.Trim();p.TaskbarMediumPercent=(int)medium.Value;
            };
            Action preview=delegate
            {
                var p=draft.Copy();read(p);p.Normalize();var dark=darkPreview.IsChecked==true;
                frame.Background=dark?UI.Brush(32,35,42):UI.Brush(232,235,241);opacityLabel.Text="背景不透明度 · "+(int)opacity.Value+"%（0% 全透明）";
                mediumLabel.Text="中余量阈值 · "+(int)medium.Value+"%";
                fontLabel.Text="任务栏字号 · "+(int)fontSize.Value+"（跟随屏幕缩放）";
                var now=(long)(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds;
                var sample=new Snapshot { Primary=new Limit { Used=24,Minutes=300,Reset=now+7200 },Secondary=new Limit { Used=48,Minutes=10080,Reset=now+259200 } };
                if(previewState.SelectedIndex==1||previewState.SelectedIndex==2) { var remaining=previewState.SelectedIndex==1?(p.WarningPercent+p.TaskbarMediumPercent)/2.0:p.WarningPercent/2.0;sample.Primary.Used=sample.Secondary.Used=100-remaining; }
                if(previewState.SelectedIndex==4)sample=null;
                var dpi=VisualTreeHelper.GetDpi(this);var rasterScale=dpi.DpiScaleX;var logicalWidth=Math.Min(320,p.TaskbarMeterWidth);image.Width=logicalWidth;
                using(var bitmap=new System.Drawing.Bitmap((int)Math.Round(logicalWidth*rasterScale),(int)Math.Round(42*rasterScale),System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                using(var graphics=System.Drawing.Graphics.FromImage(bitmap))
                using(var stream=new System.IO.MemoryStream())
                {
                    bitmap.SetResolution((float)(96*rasterScale),(float)(96*rasterScale));graphics.Clear(System.Drawing.Color.Transparent);MeterRenderer.Draw(graphics,bitmap.Width,bitmap.Height,rasterScale,sample,p,previewState.SelectedIndex==3,dark);bitmap.Save(stream,System.Drawing.Imaging.ImageFormat.Png);stream.Position=0;
                    var source=new System.Windows.Media.Imaging.BitmapImage();source.BeginInit();source.CacheOption=System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;source.StreamSource=stream;source.EndInit();source.Freeze();image.Source=source;
                }
            };
            style.SelectionChanged+=delegate { preview(); };darkPreview.Checked+=delegate { preview(); };darkPreview.Unchecked+=delegate { preview(); };opacity.ValueChanged+=delegate { preview(); };
            previewState.SelectionChanged+=delegate { preview(); };medium.ValueChanged+=delegate { preview(); };
            remainingChoice.Checked+=delegate { preview(); };used.Checked+=delegate { preview(); };firstLabel.TextChanged+=delegate { preview(); };secondLabel.TextChanged+=delegate { preview(); };fontSize.ValueChanged+=delegate { preview(); };SourceInitialized+=delegate { preview(); };DpiChanged+=delegate { preview(); };
            foreach(var input in fields)input.TextChanged+=delegate { preview(); };
            palette.SelectionChanged+=delegate
            {
                var dark=darkPreview.IsChecked==true;
                var colors=palette.SelectedIndex==1?new[] { "Auto","Auto" }:palette.SelectedIndex==2?(dark?new[] { "#68DBC5","#A2B5FF" }:new[] { "#00806B","#5868CB" }):palette.SelectedIndex==3?(dark?new[] { "#63CFFF","#86A8FF" }:new[] { "#0077B5","#526CCC" }):palette.SelectedIndex==4?(dark?new[] { "#C8A0FF","#F09AD3" }:new[] { "#8752B9","#B14383" }):palette.SelectedIndex==5?(dark?new[] { "#F3BD70","#F39580" }:new[] { "#A66512","#B45B43" }):null;
                if(colors!=null) { fields[0].Text=colors[0];fields[1].Text=colors[1];fields[2].Text="Auto";fields[3].Text="Auto";fields[4].Text="#EBB048";fields[5].Text="#E15C5C";fields[6].Text="Auto"; }
            };
            preview();
            var divider=new Border { Height=1,Background=UI.Brush(128,145,156),Opacity=.2,Margin=new Thickness(0,0,0,18) };panel.Children.Add(divider);
            return delegate { foreach(var field in fields)if(!Preferences.IsColor(field.Text.Trim()))return false;read(draft);return true; };
        }
        sealed class DialogOwner : System.Windows.Forms.IWin32Window { public IntPtr Handle { get; private set; }public DialogOwner(IntPtr handle) { Handle=handle; } }
        static TextBox Input(string text) { return new TextBox { Text=text,Height=34,FontSize=13,Padding=new Thickness(10,0,10,0),VerticalContentAlignment=VerticalAlignment.Center }; }
        static ComboBox Choice(string[] items,int index) { return new ComboBox { ItemsSource=items,SelectedIndex=index,Height=34,FontSize=13,Padding=new Thickness(8,0,8,0),VerticalContentAlignment=VerticalAlignment.Center }; }
        static void Add(Panel parent,string text,FrameworkElement input) { var label=UI.Text(text,13);label.Margin=new Thickness(0,0,0,8);parent.Children.Add(label);input.Margin=new Thickness(0,0,0,18);parent.Children.Add(input); }
    }
}
