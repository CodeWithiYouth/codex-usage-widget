using System.Windows;
using System.Windows.Markup;

namespace WindowsUsageWidget
{
    public static class Styles
    {
        public static ResourceDictionary Create()
        {
            return (ResourceDictionary)XamlReader.Parse(@"
<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <SolidColorBrush x:Key='FormFill' Color='#AAF5F9FA'/>
  <SolidColorBrush x:Key='FormText' Color='#142024'/>
  <SolidColorBrush x:Key='FormStroke' Color='#30808080'/>
  <Style x:Key='SegmentOption' TargetType='{x:Type RadioButton}'>
    <Setter Property='Foreground' Value='{DynamicResource FormText}'/>
    <Setter Property='Height' Value='32'/><Setter Property='FontSize' Value='13'/><Setter Property='Margin' Value='1'/>
    <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type RadioButton}'>
      <Border x:Name='Segment' CornerRadius='6' Background='Transparent'>
        <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center' TextElement.Foreground='{TemplateBinding Foreground}'/>
      </Border>
      <ControlTemplate.Triggers>
        <Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Segment' Property='Background' Value='#22808080'/></Trigger>
        <Trigger Property='IsChecked' Value='True'><Setter TargetName='Segment' Property='Background' Value='#0068A8'/><Setter Property='Foreground' Value='White'/></Trigger>
        <Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='Segment' Property='BorderBrush' Value='#63CFFF'/><Setter TargetName='Segment' Property='BorderThickness' Value='1'/></Trigger>
      </ControlTemplate.Triggers>
    </ControlTemplate></Setter.Value></Setter>
  </Style>
  <Style TargetType='{x:Type TextBox}'>
    <Setter Property='Foreground' Value='{DynamicResource FormText}'/>
    <Setter Property='Background' Value='{DynamicResource FormFill}'/>
    <Setter Property='BorderBrush' Value='{DynamicResource FormStroke}'/>
    <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type TextBox}'>
      <Border CornerRadius='6' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='1'>
        <ScrollViewer x:Name='PART_ContentHost' Margin='{TemplateBinding Padding}'/>
      </Border>
    </ControlTemplate></Setter.Value></Setter>
  </Style>
  <Style TargetType='{x:Type ComboBox}'>
    <Setter Property='Foreground' Value='{DynamicResource FormText}'/>
    <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type ComboBox}'>
      <Grid>
        <ToggleButton Focusable='False' IsChecked='{Binding IsDropDownOpen, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}'>
          <ToggleButton.Template><ControlTemplate TargetType='{x:Type ToggleButton}'><Border CornerRadius='6' Background='{DynamicResource FormFill}' BorderBrush='{DynamicResource FormStroke}' BorderThickness='1'/></ControlTemplate></ToggleButton.Template>
        </ToggleButton>
        <DockPanel Margin='10,0' IsHitTestVisible='False'>
          <TextBlock Text='&#xE70D;' FontFamily='Segoe MDL2 Assets' FontSize='10' DockPanel.Dock='Right' VerticalAlignment='Center'/>
          <ContentPresenter Content='{TemplateBinding SelectionBoxItem}' ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}' VerticalAlignment='Center'/>
        </DockPanel>
        <Popup x:Name='PART_Popup' Placement='Bottom' AllowsTransparency='True' IsOpen='{TemplateBinding IsDropDownOpen}' PopupAnimation='Fade'>
          <Border CornerRadius='6' Background='{DynamicResource FormFill}' BorderBrush='{DynamicResource FormStroke}' BorderThickness='1' MinWidth='{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}' Padding='4'>
            <ScrollViewer><ItemsPresenter/></ScrollViewer>
          </Border>
        </Popup>
      </Grid>
    </ControlTemplate></Setter.Value></Setter>
  </Style>
  <Style TargetType='{x:Type ComboBoxItem}'><Setter Property='Foreground' Value='{DynamicResource FormText}'/><Setter Property='Padding' Value='8,6'/></Style>
  <Style TargetType='{x:Type Button}'>
    <Setter Property='Background' Value='{DynamicResource FormFill}'/>
    <Setter Property='Foreground' Value='{DynamicResource FormText}'/>
    <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type Button}'>
      <Border x:Name='Face' CornerRadius='6' Background='{TemplateBinding Background}' BorderBrush='{DynamicResource FormStroke}' BorderThickness='{TemplateBinding BorderThickness}'>
        <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center' TextElement.Foreground='{TemplateBinding Foreground}'/>
      </Border>
      <ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Face' Property='Opacity' Value='0.8'/></Trigger></ControlTemplate.Triggers>
    </ControlTemplate></Setter.Value></Setter>
  </Style>
  <Style TargetType='{x:Type CheckBox}'>
    <Setter Property='Foreground' Value='{DynamicResource FormText}'/>
    <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type CheckBox}'>
      <DockPanel>
        <Border x:Name='Track' Width='34' Height='18' CornerRadius='9' Background='#66808080' DockPanel.Dock='Left' Margin='0,0,10,0'>
          <Ellipse x:Name='Dot' Width='12' Height='12' Fill='White' Margin='3' HorizontalAlignment='Left'/>
        </Border>
        <ContentPresenter VerticalAlignment='Center'/>
      </DockPanel>
      <ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='Track' Property='Background' Value='#0068A8'/><Setter TargetName='Dot' Property='HorizontalAlignment' Value='Right'/></Trigger></ControlTemplate.Triggers>
    </ControlTemplate></Setter.Value></Setter>
  </Style>
  <Style TargetType='{x:Type Slider}'>
    <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type Slider}'>
      <Grid VerticalAlignment='Center' Height='20'>
        <Border Background='#40808080' Height='4' CornerRadius='2'/>
        <Track x:Name='PART_Track' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' Value='{TemplateBinding Value}'>
          <Track.DecreaseRepeatButton><RepeatButton Command='{x:Static Slider.DecreaseLarge}' Focusable='False'><RepeatButton.Template><ControlTemplate><Border Background='#0068A8' Height='4' CornerRadius='2'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton>
          <Track.Thumb><Thumb Width='16' Height='16'><Thumb.Template><ControlTemplate><Ellipse Fill='#0068A8' Stroke='White' StrokeThickness='2'/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb>
          <Track.IncreaseRepeatButton><RepeatButton Command='{x:Static Slider.IncreaseLarge}' Focusable='False'><RepeatButton.Template><ControlTemplate><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton>
        </Track>
      </Grid>
    </ControlTemplate></Setter.Value></Setter>
  </Style>
  <Style TargetType='{x:Type ScrollBar}'>
    <Setter Property='Width' Value='10'/>
    <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type ScrollBar}'>
      <Grid Background='Transparent'>
        <Track x:Name='PART_Track' Orientation='{TemplateBinding Orientation}' IsDirectionReversed='True' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' Value='{TemplateBinding Value}' ViewportSize='{TemplateBinding ViewportSize}'>
          <Track.DecreaseRepeatButton><RepeatButton Command='{x:Static ScrollBar.PageUpCommand}' Focusable='False'><RepeatButton.Template><ControlTemplate><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton>
          <Track.Thumb><Thumb><Thumb.Template><ControlTemplate><Border Background='#50808080' CornerRadius='3' Width='6'/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb>
          <Track.IncreaseRepeatButton><RepeatButton Command='{x:Static ScrollBar.PageDownCommand}' Focusable='False'><RepeatButton.Template><ControlTemplate><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton>
        </Track>
      </Grid>
    </ControlTemplate></Setter.Value></Setter>
  </Style>
</ResourceDictionary>");
        }
    }
}
