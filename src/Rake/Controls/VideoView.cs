// using System;
// using System.Diagnostics;
// using System.Linq;
// using System.Reactive.Disposables;
// using System.Reactive.Linq;
// using System.Reactive.Subjects;
// using System.Runtime.InteropServices;
// using Avalonia;
// using Avalonia.Controls;
// using Avalonia.Data;
// using Avalonia.Input;
// using Avalonia.Layout;
// using Avalonia.LogicalTree;
// using Avalonia.Media;
// using Avalonia.Metadata;
// using Avalonia.Platform;
// using Avalonia.VisualTree;
// using LibVLCSharp.Shared;
// using ReactiveMarbles.ObservableEvents;
//
// namespace Rake.Controls;
//
// /// <summary>
// ///     Avalonia VideoView for Windows, Linux and Mac.
// /// </summary>
// public class VideoView : NativeControlHost
// {
//     public static readonly DirectProperty<VideoView, MediaPlayer?> MediaPlayerProperty =
//         AvaloniaProperty.RegisterDirect<VideoView, MediaPlayer?>(
//             nameof(MediaPlayer),
//             o => o.MediaPlayer,
//             (o, v) => o.MediaPlayer = v,
//             defaultBindingMode: BindingMode.TwoWay
//         );
//
//     public static readonly StyledProperty<object?> ContentProperty =
//         ContentControl.ContentProperty.AddOwner<VideoView>();
//
//     public static readonly StyledProperty<IBrush?> BackgroundProperty =
//         Panel.BackgroundProperty.AddOwner<VideoView>();
//
//     private readonly IDisposable _attacher;
//     private readonly BehaviorSubject<MediaPlayer?> _mediaPlayers = new(null);
//     private readonly BehaviorSubject<IPlatformHandle?> _platformHandles = new(null);
//     private CompositeDisposable? _disposables;
//
//     private Window? _floatingContent;
//     private bool _isAttached;
//     private IDisposable? _isEffectivelyVisible;
//
//     public VideoView()
//     {
//         _attacher = _platformHandles
//             .WithLatestFrom(_mediaPlayers)
//             .Subscribe(x =>
//             {
//                 if (x.First is not null && x.Second is not null)
//                     x.Second.SetHandle(x.First);
//             });
//
//         ContentProperty.Changed.AddClassHandler<VideoView>((s, _) => s.InitializeNativeOverlay());
//         IsVisibleProperty.Changed.AddClassHandler<VideoView>(
//             (s, _) => s.ShowNativeOverlay(s.IsVisible)
//         );
//     }
//
//     public IPlatformHandle? PlatformHandle { get; private set; }
//
//     public MediaPlayer? MediaPlayer
//     {
//         get => _mediaPlayers.Value;
//         set => _mediaPlayers.OnNext(value);
//     }
//
//     [Content]
//     public object? Content
//     {
//         get => GetValue(ContentProperty);
//         set => SetValue(ContentProperty, value);
//     }
//
//     public IBrush? Background
//     {
//         get => GetValue(BackgroundProperty);
//         set => SetValue(BackgroundProperty, value);
//     }
//
//     public void SetContent(object o)
//     {
//         Content = o;
//     }
//
//     private void InitializeNativeOverlay()
//     {
//         if (!this.IsAttachedToVisualTree())
//             return;
//
//         if (_floatingContent == null && Content != null)
//         {
//             _floatingContent = new Window
//             {
//                 SystemDecorations = SystemDecorations.None,
//
//                 TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
//                 Background = Brushes.Transparent,
//
//                 SizeToContent = SizeToContent.WidthAndHeight,
//                 CanResize = false,
//
//                 ShowInTaskbar = false,
//
//                 //Topmost=true,
//                 ZIndex = int.MaxValue,
//
//                 Opacity = 1
//             };
//
//             _floatingContent.PointerEntered += Controls_PointerEnter;
//             _floatingContent.PointerExited += Controls_PointerLeave;
//
//             _disposables = new CompositeDisposable
//             {
//                 _floatingContent.Bind(
//                     ContentControl.ContentProperty,
//                     this.GetObservable(ContentProperty)
//                 ),
//                 this.GetObservable(ContentProperty).Skip(1).Subscribe(_ => UpdateOverlayPosition()),
//                 this.GetObservable(BoundsProperty).Skip(1).Subscribe(_ => UpdateOverlayPosition()),
//                 ((Window)VisualRoot!)
//                     .Events()
//                     .PositionChanged.Subscribe(_ => UpdateOverlayPosition())
//                 // Observable.FromEventPattern(VisualRoot!, nameof(Window.PositionChanged)).Subscribe(_ => UpdateOverlayPosition())
//             };
//         }
//
//         ShowNativeOverlay(IsEffectivelyVisible);
//     }
//
//     public void Controls_PointerEnter(object? sender, PointerEventArgs e)
//     {
//         if (_floatingContent is not null)
//         {
//             Debug.WriteLine("POINTER ENTER");
//             _floatingContent.Opacity = 0.8;
//         }
//     }
//
//     public void Controls_PointerLeave(object? sender, PointerEventArgs e)
//     {
//         if (_floatingContent is not null)
//         {
//             Debug.WriteLine("POINTER LEAVE");
//             _floatingContent.Opacity = 0;
//         }
//     }
//
//     /// <inheritdoc />
//     protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
//     {
//         var handle = base.CreateNativeControlCore(parent);
//         _platformHandles.OnNext(handle);
//         PlatformHandle = handle;
//         return handle;
//     }
//
//     /// <inheritdoc />
//     protected override void DestroyNativeControlCore(IPlatformHandle control)
//     {
//         _attacher.Dispose();
//         base.DestroyNativeControlCore(control);
//         _mediaPlayers.Value?.DisposeHandle();
//     }
//
//     private void ShowNativeOverlay(bool show)
//     {
//         if (_floatingContent == null || _floatingContent.IsVisible == show)
//             return;
//         if (VisualRoot is not (Window window and not null))
//             return;
//         if (show && _isAttached)
//             _floatingContent.Show(window);
//         else
//             _floatingContent.Hide();
//     }
//
//     private void UpdateOverlayPosition()
//     {
//         if (_floatingContent == null)
//             return;
//
//         bool forceSetWidth = false,
//             forceSetHeight = false;
//
//         var topLeft = new Point();
//
//         var child = _floatingContent.Presenter?.Child;
//
//         if (child?.IsArrangeValid == true)
//         {
//             switch (child.HorizontalAlignment)
//             {
//                 case HorizontalAlignment.Right:
//                     topLeft = topLeft.WithX(Bounds.Width - _floatingContent.Bounds.Width);
//                     break;
//
//                 case HorizontalAlignment.Center:
//                     topLeft = topLeft.WithX((Bounds.Width - _floatingContent.Bounds.Width) / 2);
//                     break;
//
//                 case HorizontalAlignment.Stretch:
//                     forceSetWidth = true;
//                     break;
//                 case HorizontalAlignment.Left:
//                     break;
//                 default:
//                     throw new ArgumentOutOfRangeException();
//             }
//
//             switch (child.VerticalAlignment)
//             {
//                 case VerticalAlignment.Bottom:
//                     topLeft = topLeft.WithY(Bounds.Height - _floatingContent.Bounds.Height);
//                     break;
//
//                 case VerticalAlignment.Center:
//                     topLeft = topLeft.WithY((Bounds.Height - _floatingContent.Bounds.Height) / 2);
//                     break;
//
//                 case VerticalAlignment.Stretch:
//                     forceSetHeight = true;
//                     break;
//                 case VerticalAlignment.Top:
//                     break;
//                 default:
//                     throw new ArgumentOutOfRangeException();
//             }
//         }
//
//         if (forceSetWidth && forceSetHeight)
//             _floatingContent.SizeToContent = SizeToContent.Manual;
//         else if (forceSetHeight)
//             _floatingContent.SizeToContent = SizeToContent.Width;
//         else if (forceSetWidth)
//             _floatingContent.SizeToContent = SizeToContent.Height;
//         else
//             _floatingContent.SizeToContent = SizeToContent.Manual;
//
//         _floatingContent.Width = forceSetWidth ? Bounds.Width : double.NaN;
//         _floatingContent.Height = forceSetHeight ? Bounds.Height : double.NaN;
//
//         _floatingContent.MaxWidth = Bounds.Width;
//         _floatingContent.MaxHeight = Bounds.Height;
//
//         var newPosition = this.PointToScreen(topLeft);
//
//         if (newPosition != _floatingContent.Position)
//             _floatingContent.Position = newPosition;
//     }
//
//     protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
//     {
//         base.OnAttachedToVisualTree(e);
//
//         _isAttached = true;
//
//         InitializeNativeOverlay();
//
//         _isEffectivelyVisible = this.GetVisualAncestors()
//             .OfType<Control>()
//             .Select(v => v.GetObservable(IsVisibleProperty))
//             .CombineLatest(v => v.All(o => o))
//             .DistinctUntilChanged()
//             .Subscribe(v => IsVisible = v);
//     }
//
//     protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
//     {
//         base.OnDetachedFromVisualTree(e);
//
//         _isEffectivelyVisible?.Dispose();
//
//         ShowNativeOverlay(false);
//
//         _isAttached = false;
//     }
//
//     protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
//     {
//         base.OnDetachedFromLogicalTree(e);
//
//         _disposables?.Dispose();
//         _disposables = null;
//         _floatingContent?.Close();
//         _floatingContent = null;
//     }
// }
//
// public static class MediaPlayerExtensions
// {
//     public static void DisposeHandle(this MediaPlayer player)
//     {
//         if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//             player.Hwnd = IntPtr.Zero;
//         else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//             player.XWindow = 0;
//         else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
//             player.NsObject = IntPtr.Zero;
//     }
//
//     public static void SetHandle(this MediaPlayer player, IPlatformHandle platformHandle)
//     {
//         if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//             player.Hwnd = platformHandle.Handle;
//         else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//             player.XWindow = (uint)platformHandle.Handle;
//         else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
//             player.NsObject = platformHandle.Handle;
//     }
// }
