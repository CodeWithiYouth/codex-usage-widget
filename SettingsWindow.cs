using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;

namespace WindowsUsageWidget
{
    public class SettingsWindow : Window
    {
        public event Action<Preferences> Saved;
        public SettingsWindow(Preferences original)
        {
            Title="Codex · 参数设置";WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;Width=420;Height=Math.Min(670,SystemParameters.WorkArea.Height-24);WindowStartupLocation=WindowStartupLocation.CenterOwner;FontFamily=new FontFamily(UI.TextFontFamily);Language=System.Windows.Markup.XmlLanguage.GetLanguage("zh-CN");
            var draft=original.Copy();var root=new Grid();root.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });root.RowDefinitions.Add(new RowDefinition());root.Children.Add(UI.Chrome(this));
            var body=new Grid { Margin=new Thickness(12,6,4,12) };body.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });body.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });body.RowDefinitions.Add(new RowDefinition());body.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            var title=UI.Text("参数设置",24);title.FontWeight=FontWeights.SemiBold;title.Margin=new Thickness(0,0,8,10);body.Children.Add(title);
            var previewPanel=new StackPanel { Margin=new Thickness(0,0,8,8) };Grid.SetRow(previewPanel,1);body.Children.Add(previewPanel);
            var panel=new StackPanel { Margin=new Thickness(0,0,6,0) };var scroll=new ScrollViewer { Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(0) };Grid.SetRow(scroll,2);body.Children.Add(scroll);

            RadioButton[] sideOptions;var side=Segments(new[] { "左侧","右侧" },original.TaskbarSide=="Left"?0:1,"TaskbarSide",out sideOptions);
            var meterWidth=Input(original.TaskbarMeterWidth.ToString());meterWidth.Name="TaskbarMeterWidth";meterWidth.ToolTip="200–380 像素";
            var meterHeight=Input(original.TaskbarMeterHeight.ToString());meterHeight.Name="TaskbarMeterHeight";meterHeight.ToolTip="24–64 像素；实际高度受任务栏可用高度限制";
            var offset=Input(original.TaskbarOffset.ToString());offset.Name="TaskbarOffset";offset.ToolTip="−1200 至 1200 像素";
            var placement=new Grid { Name="TaskbarPlacementRow" };for(var i=0;i<4;i++)placement.ColumnDefinitions.Add(new ColumnDefinition());
            var placementLabels=new[] { "任务栏位置","区域宽度","区域高度","水平偏移" };var placementInputs=new FrameworkElement[] { side,meterWidth,meterHeight,offset };
            for(var i=0;i<4;i++) { var stack=new StackPanel { Margin=new Thickness(i==0?0:3,0,i==3?0:3,0) };Add(stack,placementLabels[i],placementInputs[i]);Grid.SetColumn(stack,i);placement.Children.Add(stack); }
            var saveAppearance=AppearanceControls(panel,previewPanel,draft,placement,meterWidth,meterHeight);
            Section(panel,"窗口设置");
            var top=new CheckBox { Content="始终置顶",IsChecked=original.AlwaysOnTop,FontSize=13,Margin=new Thickness(0,4,0,10) };panel.Children.Add(top);
            var minimized=new CheckBox { Content="启动时不展开详细窗口",IsChecked=original.StartMinimized,FontSize=13,Margin=new Thickness(0,0,0,10) };panel.Children.Add(minimized);
            RadioButton[] materialOptions,themeOptions;
            var material=Segments(new[] { "实色","透明" },original.Material=="Transparent"?1:0,"WindowMaterial",out materialOptions);
            var theme=Segments(new[] { "系统","浅色","深色" },original.Theme=="Light"?1:original.Theme=="Dark"?2:0,"WindowTheme",out themeOptions);AddPair(panel,"窗口样式",material,"窗口主题",theme);
            var width=Input(original.Width.ToString());
            var height=Input(original.Height.ToString());AddPair(panel,"窗口宽度（320–520）",width,"窗口高度（390–650）",height);
            var opacity=new Slider { Minimum=50,Maximum=100,Value=original.OpacityPercent,TickFrequency=5,IsSnapToTickEnabled=true,Height=30,IsEnabled=original.Material=="Transparent" };Add(panel,"整窗不透明度（仅透明模式）",opacity);
            Section(panel,"数据源");var interval=Input(original.RefreshSeconds.ToString());interval.Width=88;interval.HorizontalAlignment=HorizontalAlignment.Right;interval.ToolTip="15–3600 秒";var intervalLabel=UI.Text("刷新间隔（秒）",13);var intervalRow=UI.TwoColumns(intervalLabel,interval);intervalRow.Margin=new Thickness(0,0,0,10);panel.Children.Add(intervalRow);
            var path=Input(original.CodexPath);path.Name="CodexPath";path.ToolTip=original.CodexPath;path.Padding=new Thickness(8,4,8,4);path.Margin=new Thickness(0,0,0,4);
            var detect=new Button { Name="DetectCodexPath",Content="自动检测",Width=88,Height=28,FontSize=12,Background=UI.Brush(0,104,168),Foreground=Brushes.White,BorderThickness=new Thickness(0) };var pathHeader=UI.TwoColumns(UI.Text("Codex 路径",13),detect);pathHeader.Margin=new Thickness(0,0,0,6);panel.Children.Add(pathHeader);panel.Children.Add(path);
            var pathStatus=UI.Text("留空自动检测，也可点击按钮填入路径。",11);pathStatus.TextWrapping=TextWrapping.Wrap;pathStatus.Opacity=.7;pathStatus.Margin=new Thickness(0,0,0,8);panel.Children.Add(pathStatus);
            bool settingsClosed=false;Closed+=delegate { settingsClosed=true; };
            detect.Click+=async delegate
            {
                detect.IsEnabled=false;detect.Content="检测中…";pathStatus.Text="正在查找可用的 Codex 后台程序…";
                try { var found=await Task.Run(delegate { return CliLocator.Resolve(""); });if(!settingsClosed) { path.Text=found;path.ToolTip=found;path.ScrollToHome();pathStatus.Text="检测成功，点击应用或保存后生效。"; } }
                catch(Exception e) { if(!settingsClosed)pathStatus.Text="检测失败："+e.Message; }
                finally { if(!settingsClosed) { detect.IsEnabled=true;detect.Content="自动检测"; } }
            };
            foreach(var option in materialOptions)option.Checked+=delegate { opacity.IsEnabled=materialOptions[1].IsChecked==true; };
            var footer=new StackPanel { Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,10,8,0) };
            var cancel=new Button { Content="取消",Width=88,Height=34,Margin=new Thickness(0,0,10,0) };
            var saveText=UI.Text("保存",13);saveText.Foreground=UI.Brush(239,246,248);
            var save=new Button { Content=saveText,Width=88,Height=34,Background=UI.Brush(0,104,168),Foreground=UI.Brush(239,246,248),BorderThickness=new Thickness(0) };
            var apply=new Button { Name="ApplySettings",Content="应用",Width=88,Height=34,Margin=new Thickness(0,0,10,0) };
            footer.Children.Add(cancel);footer.Children.Add(apply);footer.Children.Add(save);Grid.SetRow(footer,3);body.Children.Add(footer);Grid.SetRow(body,1);root.Children.Add(body);Content=root;
            cancel.Click+=delegate { Close(); };
            Action<bool> commit=delegate(bool closeAfterSave)
            {
                int seconds,w,h,mw,mh,off;
                if(!int.TryParse(interval.Text,out seconds)||!int.TryParse(width.Text,out w)||!int.TryParse(height.Text,out h)||!int.TryParse(meterWidth.Text,out mw)||!int.TryParse(meterHeight.Text,out mh)||!int.TryParse(offset.Text,out off)) { MessageBox.Show(this,"请输入有效的整数参数。","参数设置");return; }
                if(!saveAppearance()) { MessageBox.Show(this,"颜色请填写 #RRGGBB，例如 #68DBC5；或填写 Auto 跟随系统。","颜色格式");return; }
                if(draft.TaskbarMediumPercent<draft.WarningPercent) { MessageBox.Show(this,"中余量阈值不能小于低余量阈值。","余量阈值");return; }
                draft.Material=materialOptions[1].IsChecked==true?"Transparent":"Solid";
                draft.Theme=themeOptions[1].IsChecked==true?"Light":themeOptions[2].IsChecked==true?"Dark":"System";
                draft.RefreshSeconds=seconds;draft.Width=w;draft.Height=h;draft.OpacityPercent=(int)opacity.Value;draft.AlwaysOnTop=top.IsChecked==true;draft.CodexPath=path.Text.Trim();draft.Normalize();
                draft.StartMinimized=minimized.IsChecked==true;draft.TaskbarSide=sideOptions[0].IsChecked==true?"Left":"Right";draft.TaskbarMeterWidth=mw;draft.TaskbarMeterHeight=mh;draft.TaskbarOffset=off;draft.Normalize();
                var handler=Saved;if(handler!=null)handler(draft.Copy());Native.Apply(this,root,draft);if(closeAfterSave)Close();
            };
            save.Click+=delegate { commit(true); };apply.Click+=delegate { commit(false); };
            SourceInitialized+=delegate { Native.Apply(this,root,original); };
        }
        Func<bool> AppearanceControls(Panel panel,Panel previewPanel,Preferences draft,FrameworkElement placement,TextBox meterWidth,TextBox meterHeight)
        {
            var heading=UI.Text("任务栏设置",16);heading.FontWeight=FontWeights.SemiBold;heading.Margin=new Thickness(0,0,0,12);panel.Children.Add(heading);
            var modeGrid=new Grid();modeGrid.ColumnDefinitions.Add(new ColumnDefinition());modeGrid.ColumnDefinitions.Add(new ColumnDefinition());
            var group="QuotaMode"+Guid.NewGuid().ToString("N");
            var remainingChoice=new RadioButton { Name="QuotaRemaining",Content="剩余",GroupName=group,IsChecked=!draft.ShowUsed,Style=(Style)FindResource("SegmentOption") };
            var used=new RadioButton { Name="QuotaUsed",Content="已用",GroupName=group,IsChecked=draft.ShowUsed,Style=(Style)FindResource("SegmentOption") };
            modeGrid.Children.Add(remainingChoice);Grid.SetColumn(used,1);modeGrid.Children.Add(used);
            var switchFrame=new Border { Child=modeGrid,CornerRadius=new CornerRadius(9),Padding=new Thickness(3),BorderThickness=new Thickness(1) };switchFrame.SetResourceReference(Border.BackgroundProperty,"FormFill");switchFrame.SetResourceReference(Border.BorderBrushProperty,"FormStroke");switchFrame.Height=34;
            var styleIds=new[] { "Rounded","Segments","Cards","Dots","Gradient","Rings","Badges" };
            var style=Choice(new[] { "圆角进度条","分段进度条","双行卡片","点阵进度","柔和渐变","环形仪表","数字胶囊" },Math.Max(0,Array.IndexOf(styleIds,draft.TaskbarStyle)));style.Name="TaskbarStyle";AddPair(panel,"显示方式",switchFrame,"显示样式",style);
            RadioButton[] numberModes;var numberOptions=Segments(new[] { "显示比例","显示数值","全部隐藏" },!draft.TaskbarShowPercentage?2:draft.TaskbarShowPercentSign?0:1,"TaskbarNumberMode",out numberModes);Add(panel,"数值显示",numberOptions);
            var ringThickness=new Slider { Name="TaskbarRingThickness",Minimum=1,Maximum=6,Value=draft.TaskbarRingThickness,TickFrequency=1,IsSnapToTickEnabled=true,Height=26,IsEnabled=draft.TaskbarStyle=="Rings" };Add(panel,"圆环粗细（1–6）",ringThickness);
            var labels=new Grid { Margin=new Thickness(0,0,0,6) };labels.ColumnDefinitions.Add(new ColumnDefinition());labels.ColumnDefinitions.Add(new ColumnDefinition());
            var firstLabel=Input(draft.TaskbarPrimaryLabel);firstLabel.Name="TaskbarPrimaryLabel";
            var secondLabel=Input(draft.TaskbarSecondaryLabel);secondLabel.Name="TaskbarSecondaryLabel";
            for(var column=0;column<2;column++) { var stack=new StackPanel { Margin=column==0?new Thickness(0,0,7,0):new Thickness(7,0,0,0) };Add(stack,column==0?"5 小时前缀":"每周前缀",column==0?firstLabel:secondLabel);Grid.SetColumn(stack,column);labels.Children.Add(stack); }panel.Children.Add(labels);
            var labelNote=UI.Text("留空隐藏；{mode} 自动切换为“余 / 用”。",11);labelNote.Opacity=.65;labelNote.Margin=new Thickness(0,0,0,14);panel.Children.Add(labelNote);
            var textFont=Choice(new[] { "微软雅黑 UI","Segoe UI","Arial","Consolas","Bahnschrift" },Math.Max(0,Array.IndexOf(Preferences.FontChoices,draft.TaskbarTextFont)));textFont.Name="TaskbarTextFont";
            var numberFont=Choice(new[] { "微软雅黑 UI","Segoe UI","Arial","Consolas","Bahnschrift" },Math.Max(0,Array.IndexOf(Preferences.FontChoices,draft.TaskbarNumberFont)));numberFont.Name="TaskbarNumberFont";
            var chineseFont=Choice(new[] { "微软雅黑 UI","微软雅黑","宋体","黑体","楷体","仿宋" },Math.Max(0,Array.IndexOf(Preferences.ChineseFontChoices,draft.TaskbarChineseFont)));chineseFont.Name="TaskbarChineseFont";
            var fonts=new Grid();for(var i=0;i<3;i++)fonts.ColumnDefinitions.Add(new ColumnDefinition());
            var fontNames=new[] { "文字字体","数字字体","中文字体" };var fontInputs=new FrameworkElement[] { textFont,numberFont,chineseFont };
            for(var i=0;i<3;i++) { var stack=new StackPanel { Margin=new Thickness(i==0?0:4,0,i==2?0:4,0) };fontInputs[i].ToolTip=i==2?Preferences.ChineseFontChoices[chineseFont.SelectedIndex]:fontInputs[i]==textFont?draft.TaskbarTextFont:draft.TaskbarNumberFont;Add(stack,fontNames[i],fontInputs[i]);Grid.SetColumn(stack,i);fonts.Children.Add(stack); }panel.Children.Add(fonts);
            var fontSize=new Slider { Name="TaskbarFontSize",Minimum=10,Maximum=14,Value=draft.TaskbarFontSize,TickFrequency=1,IsSnapToTickEnabled=true,Height=26 };
            var fontLabel=UI.Text("",12);fontLabel.Margin=new Thickness(0,0,0,6);var percentageFont=new Slider { Name="TaskbarPercentageFontSize",Minimum=8,Maximum=20,Value=draft.TaskbarPercentageFontSize,TickFrequency=1,IsSnapToTickEnabled=true,Height=26 };
            var percentageLabel=UI.Text("",12);percentageLabel.Margin=new Thickness(0,0,0,6);var fontPair=new Grid();fontPair.ColumnDefinitions.Add(new ColumnDefinition());fontPair.ColumnDefinitions.Add(new ColumnDefinition());
            var textFontPanel=new StackPanel { Margin=new Thickness(0,0,6,10) };textFontPanel.Children.Add(fontLabel);textFontPanel.Children.Add(fontSize);
            var numberFontPanel=new StackPanel { Margin=new Thickness(6,0,0,10) };numberFontPanel.Children.Add(percentageLabel);numberFontPanel.Children.Add(percentageFont);fontPair.Children.Add(textFontPanel);Grid.SetColumn(numberFontPanel,1);fontPair.Children.Add(numberFontPanel);panel.Children.Add(fontPair);
            var resetIds=new[] { "Weekday","Clock","Countdown","DateTime","Date","Hidden" };
            var resetNames=new[] { "星期＋时刻","时刻","倒计时","日期＋时刻","日期","隐藏" };
            var primaryReset=Choice(resetNames,Math.Max(0,Array.IndexOf(resetIds,draft.TaskbarPrimaryResetStyle)));primaryReset.Name="TaskbarPrimaryResetStyle";
            var weeklyReset=Choice(resetNames,Math.Max(0,Array.IndexOf(resetIds,draft.TaskbarWeeklyResetStyle)));weeklyReset.Name="TaskbarWeeklyResetStyle";
            var resetGrid=new Grid();resetGrid.ColumnDefinitions.Add(new ColumnDefinition());resetGrid.ColumnDefinitions.Add(new ColumnDefinition());
            for(var column=0;column<2;column++) { var stack=new StackPanel { Margin=column==0?new Thickness(0,0,7,0):new Thickness(7,0,0,0) };Add(stack,column==0?"5 小时重置时间":"每周重置时间",column==0?primaryReset:weeklyReset);Grid.SetColumn(stack,column);resetGrid.Children.Add(stack); }panel.Children.Add(resetGrid);
            var horizontalSpacing=new Slider { Name="TaskbarHorizontalSpacing",Minimum=0,Maximum=32,Value=draft.TaskbarHorizontalSpacing,TickFrequency=1,IsSnapToTickEnabled=true,Height=26 };
            var verticalSpacing=new Slider { Name="TaskbarVerticalSpacing",Minimum=0,Maximum=8,Value=draft.TaskbarVerticalSpacing,TickFrequency=1,IsSnapToTickEnabled=true,Height=26 };
            var horizontalLabel=UI.Text("",12);var verticalLabel=UI.Text("",12);
            var spacingGrid=new Grid { Margin=new Thickness(0,0,0,10) };spacingGrid.ColumnDefinitions.Add(new ColumnDefinition());spacingGrid.ColumnDefinitions.Add(new ColumnDefinition());
            var horizontalPanel=new StackPanel { Margin=new Thickness(0,0,6,0) };horizontalPanel.Children.Add(horizontalLabel);horizontalPanel.Children.Add(horizontalSpacing);
            var verticalPanel=new StackPanel { Margin=new Thickness(6,0,0,0) };verticalPanel.Children.Add(verticalLabel);verticalPanel.Children.Add(verticalSpacing);Grid.SetColumn(verticalPanel,1);spacingGrid.Children.Add(horizontalPanel);spacingGrid.Children.Add(verticalPanel);
            var image=new System.Windows.Controls.Image { Height=42,Stretch=Stretch.None,HorizontalAlignment=HorizontalAlignment.Center };
            var frame=new Border { Child=image,CornerRadius=new CornerRadius(10),Padding=new Thickness(8,0,8,0),Margin=new Thickness(0,0,0,8) };previewPanel.Children.Add(frame);
            var showBounds=new CheckBox { Name="TaskbarPreviewBounds",Content="显示边框",FontSize=12,VerticalAlignment=VerticalAlignment.Center,ToolTip="蓝色为区域外框，橙色虚线为两组控件的范围；仅在预览中显示。" };
            var previewSize=UI.Text("",11);previewSize.Opacity=.7;var guideRow=UI.TwoColumns(previewSize,showBounds);guideRow.Margin=new Thickness(0,0,0,6);previewPanel.Children.Add(guideRow);
            var previewMode=new Grid();previewMode.ColumnDefinitions.Add(new ColumnDefinition());previewMode.ColumnDefinitions.Add(new ColumnDefinition());
            var previewGroup="PreviewTheme"+Guid.NewGuid().ToString("N");
            var lightPreview=new RadioButton { Name="TaskbarPreviewLight",Content="浅色",GroupName=previewGroup,IsChecked=!UI.TaskbarDark(),Style=(Style)FindResource("SegmentOption") };
            var darkPreview=new RadioButton { Name="TaskbarPreviewDark",Content="深色",GroupName=previewGroup,IsChecked=UI.TaskbarDark(),Style=(Style)FindResource("SegmentOption") };
            previewMode.Children.Add(lightPreview);Grid.SetColumn(darkPreview,1);previewMode.Children.Add(darkPreview);
            var previewSwitch=new Border { Child=previewMode,Height=34,CornerRadius=new CornerRadius(9),Padding=new Thickness(3),BorderThickness=new Thickness(1) };previewSwitch.SetResourceReference(Border.BackgroundProperty,"FormFill");previewSwitch.SetResourceReference(Border.BorderBrushProperty,"FormStroke");
            var previewState=Choice(new[] { "示例用量","高余量","中余量","低余量","数据过期","未连接" },0);previewState.Name="TaskbarPreviewState";
            AddPair(previewPanel,"任务栏预览",previewSwitch,"预览状态",previewState);
            panel.Children.Add(placement);panel.Children.Add(spacingGrid);Section(panel,"颜色显示");
            var palette=Choice(PaletteNames,0);palette.Name="TaskbarPalette";
            var paletteLabel=UI.Text("配色预设",13);paletteLabel.VerticalAlignment=VerticalAlignment.Center;
            var paletteRow=new Grid { Name="TaskbarPaletteRow",Margin=new Thickness(0,0,0,12) };paletteRow.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(94) });paletteRow.ColumnDefinitions.Add(new ColumnDefinition());paletteRow.Children.Add(paletteLabel);Grid.SetColumn(palette,1);paletteRow.Children.Add(palette);panel.Children.Add(paletteRow);
            var fields=new System.Collections.Generic.List<TextBox>();var swatches=new System.Collections.Generic.List<Border>();var colorPanels=new System.Collections.Generic.List<StackPanel>();
            var values=new[] { draft.TaskbarPrimaryColor,draft.TaskbarSecondaryColor,draft.TaskbarTextColor,draft.TaskbarBackgroundColor,draft.TaskbarWarningColor,draft.TaskbarStaleColor,draft.TaskbarMediumColor,draft.LowColor(true),draft.MediumColor(true) };
            var names=new[] { "正常颜色","正常颜色","文字颜色","背景颜色","低余量颜色","过期颜色","中余量颜色","低余量颜色","中余量颜色" };
            for(var index=0;index<values.Length;index++)
            {
                var stack=new StackPanel();var label=UI.Text(names[index],11);label.Margin=new Thickness(0,0,0,5);stack.Children.Add(label);
                var input=Input(values[index]);input.Name="TaskbarColor"+index;input.FontSize=11;input.Height=30;input.Padding=new Thickness(3,0,2,0);fields.Add(input);
                var swatch=new Border { Width=20,Height=20,CornerRadius=new CornerRadius(4),BorderThickness=new Thickness(1),BorderBrush=UI.Brush(128,145,156) };
                swatches.Add(swatch);var pick=new Button { Name="TaskbarColorPicker"+index,Content=swatch,Width=28,Height=30,Margin=new Thickness(3,0,0,0),Padding=new Thickness(3),Background=Brushes.Transparent,BorderThickness=new Thickness(0),ToolTip="选择"+names[index] };
                stack.Children.Add(UI.TwoColumns(input,pick));colorPanels.Add(stack);
                Action paintSwatch=delegate { var color=MeterRenderer.ColorValue(input.Text,System.Drawing.Color.FromArgb(128,145,156));swatch.Background=UI.Brush(color.R,color.G,color.B); };paintSwatch();input.TextChanged+=delegate { paintSwatch(); };
                pick.Click+=delegate
                {
                    using(var dialog=new System.Windows.Forms.ColorDialog { FullOpen=true,Color=System.Drawing.Color.FromArgb(((SolidColorBrush)swatch.Background).Color.R,((SolidColorBrush)swatch.Background).Color.G,((SolidColorBrush)swatch.Background).Color.B) })
                        if(dialog.ShowDialog(new DialogOwner(new System.Windows.Interop.WindowInteropHelper(this).Handle))==System.Windows.Forms.DialogResult.OK)
                            input.Text="#"+dialog.Color.R.ToString("X2")+dialog.Color.G.ToString("X2")+dialog.Color.B.ToString("X2");
                };
            }
            var thresholds=new ThresholdRange { Name="TaskbarThresholdRange",Medium=draft.MediumThreshold(false),Low=draft.LowThreshold(false) };
            var weeklyThresholds=new ThresholdRange { Name="WeeklyThresholdRange",Medium=draft.MediumThreshold(true),Low=draft.LowThreshold(true) };
            var quotaLabels=new[] { UI.Text("",12),UI.Text("",12) };
            var copyToWeekly=new Button { Name="CopyPrimaryToWeekly",Content="复制到每周",Height=28,Width=96,FontSize=12 };
            var copyToPrimary=new Button { Name="CopyWeeklyToPrimary",Content="复制到5小时",Height=28,Width=96,FontSize=12 };
            var ranges=new[] { thresholds,weeklyThresholds };var buttons=new[] { copyToWeekly,copyToPrimary };var orders=new[] { new[] { 4,6,0 },new[] { 7,8,1 } };
            for(var groupIndex=0;groupIndex<2;groupIndex++)
            {
                var headingRow=UI.TwoColumns(UI.Text(groupIndex==0?"5 小时额度":"每周额度",14),buttons[groupIndex]);headingRow.Margin=new Thickness(0,groupIndex==0?0:14,0,6);panel.Children.Add(headingRow);
                panel.Children.Add(quotaLabels[groupIndex]);panel.Children.Add(ranges[groupIndex]);
                var colorGrid=new Grid();for(var column=0;column<3;column++) { colorGrid.ColumnDefinitions.Add(new ColumnDefinition());var stack=colorPanels[orders[groupIndex][column]];stack.Margin=new Thickness(column==0?0:4,0,column==2?0:4,0);Grid.SetColumn(stack,column);colorGrid.Children.Add(stack); }panel.Children.Add(colorGrid);
            }
            var sharedColors=new Grid { Margin=new Thickness(0,14,0,12) };for(var column=0;column<3;column++) { sharedColors.ColumnDefinitions.Add(new ColumnDefinition());var stack=colorPanels[new[] { 2,3,5 }[column]];stack.Margin=new Thickness(column==0?0:4,0,column==2?0:4,0);Grid.SetColumn(stack,column);sharedColors.Children.Add(stack); }panel.Children.Add(sharedColors);
            var note=UI.Text("点击色块选色；Auto 跟随系统；阈值按剩余量判定。",11);note.Opacity=.65;note.TextWrapping=TextWrapping.Wrap;note.Margin=new Thickness(0,0,0,14);panel.Children.Add(note);
            var opacity=new Slider { Minimum=0,Maximum=100,Value=draft.TaskbarBackgroundOpacity,TickFrequency=5,IsSnapToTickEnabled=true,Height=26 };opacity.Name="TaskbarBackgroundOpacity";
            var opacityLabel=UI.Text("",12);opacityLabel.Margin=new Thickness(0,0,0,6);panel.Children.Add(opacityLabel);opacity.Margin=new Thickness(0,0,0,20);panel.Children.Add(opacity);
            Action<Preferences> read=delegate(Preferences p)
            {
                int previewWidth;if(int.TryParse(meterWidth.Text,out previewWidth))p.TaskbarMeterWidth=previewWidth;int previewHeight;if(int.TryParse(meterHeight.Text,out previewHeight))p.TaskbarMeterHeight=previewHeight;p.TaskbarHorizontalSpacing=(int)horizontalSpacing.Value;p.TaskbarVerticalSpacing=(int)verticalSpacing.Value;
                p.TaskbarStyle=styleIds[Math.Max(0,style.SelectedIndex)];p.TaskbarShowPercentage=numberModes[2].IsChecked!=true;p.TaskbarShowPercentSign=numberModes[0].IsChecked==true;p.TaskbarRingThickness=(int)ringThickness.Value;
                p.ShowUsed=used.IsChecked==true;p.TaskbarPrimaryLabel=firstLabel.Text;p.TaskbarSecondaryLabel=secondLabel.Text;p.TaskbarFontSize=(int)fontSize.Value;p.TaskbarPercentageFontSize=(int)percentageFont.Value;p.TaskbarTextFont=Preferences.FontChoices[Math.Max(0,textFont.SelectedIndex)];p.TaskbarNumberFont=Preferences.FontChoices[Math.Max(0,numberFont.SelectedIndex)];p.TaskbarChineseFont=Preferences.ChineseFontChoices[Math.Max(0,chineseFont.SelectedIndex)];
                p.TaskbarPrimaryResetStyle=resetIds[Math.Max(0,primaryReset.SelectedIndex)];p.TaskbarWeeklyResetStyle=resetIds[Math.Max(0,weeklyReset.SelectedIndex)];
                p.TaskbarPrimaryColor=fields[0].Text.Trim();p.TaskbarSecondaryColor=fields[1].Text.Trim();p.TaskbarTextColor=fields[2].Text.Trim();p.TaskbarBackgroundColor=fields[3].Text.Trim();p.TaskbarWarningColor=fields[4].Text.Trim();p.TaskbarStaleColor=fields[5].Text.Trim();p.TaskbarBackgroundOpacity=(int)opacity.Value;
                p.WarningPercent=thresholds.Low;p.TaskbarMediumColor=fields[6].Text.Trim();p.TaskbarMediumPercent=thresholds.Medium;p.WeeklyWarningPercent=weeklyThresholds.Low;p.WeeklyMediumPercent=weeklyThresholds.Medium;p.WeeklyWarningColor=fields[7].Text.Trim();p.WeeklyMediumColor=fields[8].Text.Trim();
            };
            int lastPreviewTaskbarHeight=0;
            Action preview=delegate
            {
                ringThickness.IsEnabled=style.SelectedIndex==5;horizontalSpacing.IsEnabled=style.SelectedIndex==5;verticalSpacing.IsEnabled=style.SelectedIndex!=5;
                horizontalLabel.Text="横向间距 · "+(int)horizontalSpacing.Value+" px";verticalLabel.Text="纵向间距 · "+(int)verticalSpacing.Value+" px";
                horizontalSpacing.ToolTip="圆环左右两组之间的间距（0–32 像素）";verticalSpacing.ToolTip="上下两行之间的间距（0–8 像素），在任务栏高度内调整";
                var p=draft.Copy();read(p);p.Normalize();var dark=darkPreview.IsChecked==true;
                frame.Background=dark?UI.Brush(32,35,42):UI.Brush(232,235,241);opacityLabel.Text="背景不透明度 · "+(int)opacity.Value+"%（0% 全透明）";
                for(var groupIndex=0;groupIndex<2;groupIndex++) { var order=orders[groupIndex];var range=ranges[groupIndex];range.LowColor=ToWpf(MeterRenderer.SettingColor(p,dark,order[0]));range.MediumColor=ToWpf(MeterRenderer.SettingColor(p,dark,order[1]));range.NormalColor=ToWpf(MeterRenderer.SettingColor(p,dark,order[2]));range.InvalidateVisual();quotaLabels[groupIndex].Text="低 ≤ "+range.Low+"%　中 ≤ "+range.Medium+"%　正常 > "+range.Medium+"%"; }
                for(var colorIndex=0;colorIndex<swatches.Count;colorIndex++)swatches[colorIndex].Background=new SolidColorBrush(ToWpf(MeterRenderer.SettingColor(p,dark,colorIndex)));
                fontLabel.Text="文字字号 · "+(int)fontSize.Value;percentageLabel.Text="百分比字号 · "+(int)percentageFont.Value;percentageFont.IsEnabled=numberModes[2].IsChecked!=true;
                var sample=PreviewSnapshot(previewState.SelectedIndex);
                var dpi=VisualTreeHelper.GetDpi(this);var rasterScale=dpi.DpiScaleX;var logicalWidth=p.TaskbarMeterWidth;var taskbarPixelHeight=EmbeddedMeter.PreviewTaskbarPixelHeight(rasterScale);lastPreviewTaskbarHeight=taskbarPixelHeight;var taskbarLogicalHeight=taskbarPixelHeight/rasterScale;var pixelHeight=Math.Min(taskbarPixelHeight,EmbeddedMeter.PreviewPixelHeight(p,rasterScale));var logicalHeight=pixelHeight/rasterScale;image.Width=logicalWidth;image.Height=taskbarLogicalHeight;frame.Height=taskbarLogicalHeight;var heightLimited=logicalHeight<p.TaskbarMeterHeight-.5;previewSize.Text="任务栏 "+taskbarLogicalHeight.ToString("0.#")+" · 区域 "+logicalWidth+" × "+(heightLimited?logicalHeight:p.TaskbarMeterHeight).ToString("0.#")+" px"+(heightLimited?" · 高度受限":"");previewSize.ToolTip="设置高度 "+p.TaskbarMeterHeight+" px；预览外层高度固定为当前系统任务栏高度，内部区域居中显示，实际高度受任务栏可用高度限制。";
                using(var bitmap=new System.Drawing.Bitmap((int)Math.Round(logicalWidth*rasterScale),taskbarPixelHeight,System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                using(var graphics=System.Drawing.Graphics.FromImage(bitmap))
                using(var stream=new System.IO.MemoryStream())
                {
                    bitmap.SetResolution((float)(96*rasterScale),(float)(96*rasterScale));graphics.Clear(System.Drawing.Color.Transparent);
                    var state=graphics.Save();graphics.TranslateTransform(0,(taskbarPixelHeight-pixelHeight)/2);
                    MeterRenderer.Draw(graphics,bitmap.Width,pixelHeight,rasterScale,sample,p,previewState.SelectedIndex==4,dark);if(showBounds.IsChecked==true)MeterRenderer.DrawPreviewBounds(graphics,bitmap.Width,pixelHeight,rasterScale,p);graphics.Restore(state);bitmap.Save(stream,System.Drawing.Imaging.ImageFormat.Png);stream.Position=0;
                    var source=new System.Windows.Media.Imaging.BitmapImage();source.BeginInit();source.CacheOption=System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;source.StreamSource=stream;source.EndInit();source.Freeze();image.Source=source;
                }
            };
            horizontalSpacing.ValueChanged+=delegate { preview(); };verticalSpacing.ValueChanged+=delegate { preview(); };meterWidth.TextChanged+=delegate { preview(); };meterHeight.TextChanged+=delegate { preview(); };showBounds.Checked+=delegate { preview(); };showBounds.Unchecked+=delegate { preview(); };
            chineseFont.SelectionChanged+=delegate { preview(); };textFont.SelectionChanged+=delegate { preview(); };numberFont.SelectionChanged+=delegate { preview(); };style.SelectionChanged+=delegate { preview(); };darkPreview.Checked+=delegate { preview(); };lightPreview.Checked+=delegate { preview(); };opacity.ValueChanged+=delegate { preview(); };
            foreach(var mode in numberModes)mode.Checked+=delegate { preview(); };ringThickness.ValueChanged+=delegate { preview(); };
            previewState.SelectionChanged+=delegate { preview(); };thresholds.Changed+=delegate { preview(); };weeklyThresholds.Changed+=delegate { preview(); };
            remainingChoice.Checked+=delegate { preview(); };used.Checked+=delegate { preview(); };firstLabel.TextChanged+=delegate { preview(); };secondLabel.TextChanged+=delegate { preview(); };fontSize.ValueChanged+=delegate { preview(); };percentageFont.ValueChanged+=delegate { preview(); };SourceInitialized+=delegate { preview(); };DpiChanged+=delegate { preview(); };
            primaryReset.SelectionChanged+=delegate { preview(); };weeklyReset.SelectionChanged+=delegate { preview(); };
            foreach(var input in fields)input.TextChanged+=delegate { preview(); };
            palette.SelectionChanged+=delegate
            {
                var dark=darkPreview.IsChecked==true;
                var colors=PresetColors(palette.SelectedIndex,dark);if(colors!=null) { for(var colorIndex=0;colorIndex<colors.Length;colorIndex++)fields[colorIndex].Text=colors[colorIndex];preview(); }
            };
            copyToWeekly.Click+=delegate { CopyQuota(thresholds,weeklyThresholds,fields,orders[0],orders[1]);preview(); };copyToPrimary.Click+=delegate { CopyQuota(weeklyThresholds,thresholds,fields,orders[1],orders[0]);preview(); };
            var taskbarTimer=new System.Windows.Threading.DispatcherTimer { Interval=TimeSpan.FromSeconds(2) };
            taskbarTimer.Tick+=delegate { if(EmbeddedMeter.PreviewTaskbarPixelHeight(VisualTreeHelper.GetDpi(this).DpiScaleX)!=lastPreviewTaskbarHeight)preview(); };
            Loaded+=delegate { taskbarTimer.Start(); };Closed+=delegate { taskbarTimer.Stop(); };
            preview();

            return delegate { foreach(var field in fields)if(!Preferences.IsColor(field.Text.Trim()))return false;read(draft);return true; };
        }
        public static Snapshot PreviewSnapshot(int state)
        {
            if(state==5)return null;
            var now=(long)(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds;
            var primary=state==1?82:state==2?51:state==3?13:76;var weekly=state==1?81:state==2?49:state==3?11:52;
            return new Snapshot { Primary=new Limit { Used=100-primary,Minutes=300,Reset=now+7200 },Secondary=new Limit { Used=100-weekly,Minutes=10080,Reset=now+259200 } };
        }
        static void CopyQuota(ThresholdRange source,ThresholdRange target,System.Collections.Generic.List<TextBox> fields,int[] sourceFields,int[] targetFields) { target.Low=0;target.Medium=source.Medium;target.Low=source.Low;for(var i=0;i<3;i++)fields[targetFields[i]].Text=fields[sourceFields[i]].Text; }
        sealed class DialogOwner : System.Windows.Forms.IWin32Window { public IntPtr Handle { get; private set; }public DialogOwner(IntPtr handle) { Handle=handle; } }
        static Color ToWpf(System.Drawing.Color color) { return Color.FromRgb(color.R,color.G,color.B); }
        public static readonly string[] PaletteNames={ "自定义","跟随系统","薄荷与靛蓝","海蓝","暮紫","暖橙","森林与金砂","玫瑰与烟紫","冰川与湖蓝","石墨与银灰" };
        public static string[] PresetColors(int preset,bool dark)
        {
            if(preset==0)return null;
            if(preset==1)return new[] { "Auto","Auto","Auto","Auto","Auto","Auto","Auto","Auto","Auto" };
            if(preset<0||preset>=PaletteNames.Length)throw new ArgumentOutOfRangeException("preset");
            if(preset>=6)
            {
                var light=new[] {
                    new[] { "#297B50","#917124","Auto","Auto","#C96540","#D95858","#6E873E","#BE674D","#9C834B" },
                    new[] { "#B4466D","#76639B","Auto","Auto","#C56C42","#D95858","#AD796A","#BF6559","#988099" },
                    new[] { "#168485","#356FAC","Auto","Auto","#C87643","#D95858","#668F84","#C06D55","#6D87A3" },
                    new[] { "#505E6C","#74808D","Auto","Auto","#B86442","#C94F56","#93834F","#AC6A53","#928B70" }
                };
                var darkColors=new[] {
                    new[] { "#8ADBA2","#E6C576","Auto","Auto","#F29A75","#F17C85","#BDD294","#F0A48A","#D8C79B" },
                    new[] { "#F3A0BC","#C5B3EC","Auto","Auto","#F2A078","#F17C85","#DAB6A4","#F0A496","#D2BACF" },
                    new[] { "#80DFD8","#96C7FF","Auto","Auto","#F2B07D","#F17C85","#AED2BF","#EFA791","#B5CCE2" },
                    new[] { "#BECAD6","#E0E6ED","Auto","Auto","#EBA27F","#F17C85","#D3C396","#E8AD93","#D8D1B8" }
                };
                return (string[])(dark?darkColors[preset-6]:light[preset-6]).Clone();
            }
            var primary=preset==2?(dark?"#68DBC5":"#00806B"):preset==3?(dark?"#63CFFF":"#0077B5"):preset==4?(dark?"#C8A0FF":"#8752B9"):(dark?"#F3BD70":"#A66512");
            var weekly=preset==2?(dark?"#A2B5FF":"#5868CB"):preset==3?(dark?"#86A8FF":"#526CCC"):preset==4?(dark?"#F09AD3":"#B14383"):(dark?"#F39580":"#B45B43");
            var low=preset==2?"#DCA34B":preset==3?"#D99C4A":preset==4?"#DB9271":"#DD7760";
            var medium=preset==2?(dark?"#8FC8A9":"#538B69"):preset==3?(dark?"#79B5CB":"#477F98"):preset==4?(dark?"#BDA0C7":"#84628E"):(dark?"#D2AB77":"#9B7845");
            var weeklyLow=preset==2?"#D4A06D":preset==3?"#CE9B70":preset==4?"#D99288":"#D97770";
            var weeklyMedium=preset==2?(dark?"#ADB0D4":"#777CA9"):preset==3?(dark?"#99ACD1":"#657BA5"):preset==4?(dark?"#D0A0B9":"#9A6884"):(dark?"#CF9D91":"#A57566");
            return new[] { primary,weekly,"Auto","Auto",low,"#E15C5C",medium,weeklyLow,weeklyMedium };
        }
        static Color ColorFrom(string value,byte r,byte g,byte b) { var color=MeterRenderer.ColorValue(value,System.Drawing.Color.FromArgb(r,g,b));return Color.FromRgb(color.R,color.G,color.B); }
        Border Segments(string[] labels,int selected,string name,out RadioButton[] options)
        {
            var grid=new Grid();options=new RadioButton[labels.Length];var group=name+Guid.NewGuid().ToString("N");
            for(var i=0;i<labels.Length;i++) { grid.ColumnDefinitions.Add(new ColumnDefinition());options[i]=new RadioButton { Name=name+i,Content=labels[i],GroupName=group,IsChecked=i==selected,Style=(Style)FindResource("SegmentOption") };Grid.SetColumn(options[i],i);grid.Children.Add(options[i]); }
            var frame=new Border { Child=grid,Height=34,CornerRadius=new CornerRadius(9),Padding=new Thickness(3),BorderThickness=new Thickness(1) };frame.SetResourceReference(Border.BackgroundProperty,"FormFill");frame.SetResourceReference(Border.BorderBrushProperty,"FormStroke");return frame;
        }
        static void Section(Panel panel,string title) { var divider=new Border { Height=1,Background=UI.Brush(128,145,156),Opacity=.2,Margin=new Thickness(0,8,0,12) };panel.Children.Add(divider);var heading=UI.Text(title,16);heading.FontWeight=FontWeights.SemiBold;heading.Margin=new Thickness(0,0,0,12);panel.Children.Add(heading); }
        static void AddPair(Panel parent,string leftLabel,FrameworkElement left,string rightLabel,FrameworkElement right)
        {
            var grid=new Grid();grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition());
            var first=new StackPanel { Margin=new Thickness(0,0,6,0) };var second=new StackPanel { Margin=new Thickness(6,0,0,0) };
            Add(first,leftLabel,left);Add(second,rightLabel,right);grid.Children.Add(first);Grid.SetColumn(second,1);grid.Children.Add(second);parent.Children.Add(grid);
        }
        static TextBox Input(string text) { return new TextBox { Text=text,Height=34,FontSize=13,Padding=new Thickness(10,0,10,0),VerticalContentAlignment=VerticalAlignment.Center }; }
        static ComboBox Choice(string[] items,int index) { return new ComboBox { ItemsSource=items,SelectedIndex=index,Height=34,FontSize=13,Padding=new Thickness(8,0,8,0),VerticalContentAlignment=VerticalAlignment.Center }; }
        static void Add(Panel parent,string text,FrameworkElement input) { var label=UI.Text(text,13);label.Margin=new Thickness(0,0,0,5);parent.Children.Add(label);input.Margin=new Thickness(0,0,0,8);parent.Children.Add(input); }
    }
}
