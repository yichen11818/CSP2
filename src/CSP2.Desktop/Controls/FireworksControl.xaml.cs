using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace CSP2.Desktop.Controls;

/// <summary>
/// 烟花动画控件
/// </summary>
public partial class FireworksControl : UserControl
{
    private readonly Random _random = new();
    
    public FireworksControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 播放烟花动画
    /// </summary>
    public void Play()
    {
        // 清空之前的粒子
        FireworksCanvas.Children.Clear();
        
        // 获取画布中心
        var centerX = ActualWidth / 2;
        var centerY = ActualHeight / 2;
        
        // 创建多个烟花粒子
        var particleCount = 80; // 粒子数量
        var colors = new[] 
        { 
            Colors.Gold, 
            Colors.Orange, 
            Colors.Yellow, 
            Colors.Red, 
            Colors.DeepPink, 
            Colors.HotPink,
            Colors.Cyan,
            Colors.LightBlue,
            Colors.LightGreen,
            Colors.Violet
        };

        for (int i = 0; i < particleCount; i++)
        {
            // 计算粒子的飞行方向（360度均匀分布）
            var angle = (360.0 / particleCount) * i;
            var radians = angle * Math.PI / 180.0;
            
            // 飞行距离（随机变化）
            var distance = 150 + _random.Next(100);
            
            // 目标位置
            var targetX = centerX + Math.Cos(radians) * distance;
            var targetY = centerY + Math.Sin(radians) * distance;
            
            // 随机选择颜色
            var color = colors[_random.Next(colors.Length)];
            
            // 创建粒子
            CreateParticle(centerX, centerY, targetX, targetY, color, i * 5); // 逐渐延迟
        }
        
        // 创建中心爆炸光效
        CreateCenterFlash(centerX, centerY);
    }

    /// <summary>
    /// 创建单个粒子
    /// </summary>
    private void CreateParticle(double startX, double startY, double endX, double endY, Color color, int delayMs)
    {
        // 创建椭圆作为粒子
        var particle = new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = new SolidColorBrush(color),
            Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 3 }
        };
        
        // 设置初始位置
        Canvas.SetLeft(particle, startX - particle.Width / 2);
        Canvas.SetTop(particle, startY - particle.Height / 2);
        
        FireworksCanvas.Children.Add(particle);
        
        // 创建位置动画
        var storyboard = new Storyboard();
        
        // X轴移动动画
        var moveX = new DoubleAnimation
        {
            From = startX - particle.Width / 2,
            To = endX - particle.Width / 2,
            Duration = TimeSpan.FromMilliseconds(800),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            BeginTime = TimeSpan.FromMilliseconds(delayMs)
        };
        Storyboard.SetTarget(moveX, particle);
        Storyboard.SetTargetProperty(moveX, new PropertyPath("(Canvas.Left)"));
        storyboard.Children.Add(moveX);
        
        // Y轴移动动画
        var moveY = new DoubleAnimation
        {
            From = startY - particle.Height / 2,
            To = endY - particle.Height / 2,
            Duration = TimeSpan.FromMilliseconds(800),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            BeginTime = TimeSpan.FromMilliseconds(delayMs)
        };
        Storyboard.SetTarget(moveY, particle);
        Storyboard.SetTargetProperty(moveY, new PropertyPath("(Canvas.Top)"));
        storyboard.Children.Add(moveY);
        
        // 透明度动画（渐隐）
        var fadeOut = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(600),
            BeginTime = TimeSpan.FromMilliseconds(delayMs + 200) // 延迟后开始渐隐
        };
        Storyboard.SetTarget(fadeOut, particle);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
        storyboard.Children.Add(fadeOut);
        
        // 大小缩放动画
        var scaleTransform = new ScaleTransform(1.0, 1.0, particle.Width / 2, particle.Height / 2);
        particle.RenderTransform = scaleTransform;
        
        var scaleAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.3,
            Duration = TimeSpan.FromMilliseconds(800),
            BeginTime = TimeSpan.FromMilliseconds(delayMs)
        };
        Storyboard.SetTarget(scaleAnimation, particle);
        Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath("RenderTransform.ScaleX"));
        storyboard.Children.Add(scaleAnimation);
        
        var scaleAnimationY = new DoubleAnimation
        {
            From = 1.0,
            To = 0.3,
            Duration = TimeSpan.FromMilliseconds(800),
            BeginTime = TimeSpan.FromMilliseconds(delayMs)
        };
        Storyboard.SetTarget(scaleAnimationY, particle);
        Storyboard.SetTargetProperty(scaleAnimationY, new PropertyPath("RenderTransform.ScaleY"));
        storyboard.Children.Add(scaleAnimationY);
        
        // 动画完成后移除粒子
        storyboard.Completed += (s, e) => FireworksCanvas.Children.Remove(particle);
        
        // 开始动画
        storyboard.Begin();
    }

    /// <summary>
    /// 创建中心爆炸光效
    /// </summary>
    private void CreateCenterFlash(double centerX, double centerY)
    {
        // 创建中心光球
        var flash = new Ellipse
        {
            Width = 30,
            Height = 30,
            Fill = new RadialGradientBrush
            {
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Colors.White, 0.0),
                    new GradientStop(Color.FromArgb(200, 255, 215, 0), 0.5),
                    new GradientStop(Colors.Transparent, 1.0)
                }
            },
            Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 15 }
        };
        
        Canvas.SetLeft(flash, centerX - flash.Width / 2);
        Canvas.SetTop(flash, centerY - flash.Height / 2);
        
        FireworksCanvas.Children.Add(flash);
        
        // 创建爆炸动画
        var storyboard = new Storyboard();
        
        // 缩放动画
        var scaleTransform = new ScaleTransform(1.0, 1.0, flash.Width / 2, flash.Height / 2);
        flash.RenderTransform = scaleTransform;
        
        var scaleAnimation = new DoubleAnimation
        {
            From = 0.5,
            To = 8.0,
            Duration = TimeSpan.FromMilliseconds(600),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(scaleAnimation, flash);
        Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath("RenderTransform.ScaleX"));
        storyboard.Children.Add(scaleAnimation);
        
        var scaleAnimationY = new DoubleAnimation
        {
            From = 0.5,
            To = 8.0,
            Duration = TimeSpan.FromMilliseconds(600),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(scaleAnimationY, flash);
        Storyboard.SetTargetProperty(scaleAnimationY, new PropertyPath("RenderTransform.ScaleY"));
        storyboard.Children.Add(scaleAnimationY);
        
        // 透明度动画
        var fadeOut = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(600)
        };
        Storyboard.SetTarget(fadeOut, flash);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
        storyboard.Children.Add(fadeOut);
        
        // 动画完成后移除元素
        storyboard.Completed += (s, e) => FireworksCanvas.Children.Remove(flash);
        
        // 开始动画
        storyboard.Begin();
    }
}


