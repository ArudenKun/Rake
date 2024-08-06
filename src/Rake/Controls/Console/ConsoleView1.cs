// using System.Collections.Generic;
// using System.Threading.Channels;
// using System.Threading.Tasks;
// using Avalonia;
// using Avalonia.Controls;
// using Avalonia.Controls.Primitives;
// using Avalonia.Input;
// using Avalonia.Layout;
// using Avalonia.Media;
//
// namespace Rake.Controls.Console;
//
// public class ConsoleView : ContentControl
// {
//     private readonly Stack<string> _history = new();
//     private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();
//     private readonly ChannelWriter<string> _writer;
//
//     private readonly TextBox _inputTextBox;
//     private readonly TextBlock _outputTextBlock;
//     private readonly ScrollViewer _scrollViewer;
//
//     public ConsoleView()
//     {
//         var panel = new StackPanel
//         {
//             VerticalAlignment = VerticalAlignment.Stretch,
//             HorizontalAlignment = HorizontalAlignment.Stretch
//         };
//
//         _outputTextBlock = new TextBlock
//         {
//             VerticalAlignment = VerticalAlignment.Top,
//             TextWrapping = TextWrapping.Wrap,
//             DataContext = _history,
//             Background = Brushes.Black,
//             Foreground = Brushes.White,
//             FontFamily = new FontFamily("Consolas"),
//             FontSize = 14,
//             Padding = new Thickness(5)
//         };
//
//         _scrollViewer = new ScrollViewer
//         {
//             MaxHeight = 300,
//             VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
//             Content = _outputTextBlock
//         };
//
//         _inputTextBox = new TextBox
//         {
//             VerticalAlignment = VerticalAlignment.Bottom
//         };
//
//         _inputTextBox.KeyDown += OnInputKeyDown;
//
//         panel.Children.Add(_scrollViewer);
//         panel.Children.Add(_inputTextBox);
//
//         Content = panel;
//         StartListening();
//
//         _writer = _channel.Writer;
//     }
//
//     public ConsoleViewWriter ViewWriter => new(_writer);
//
//     public async Task WriteAsync(string message)
//     {
//         await ViewWriter.WriteAsync(message);
//     }
//
//     private void WriteToHistory(string message)
//     {
//         _outputTextBlock.Text += $"\n> {message}";
//         _scrollViewer.ScrollToEnd();
//     }
//
//     private void OnInputKeyDown(object? sender, KeyEventArgs e)
//     {
//         if (e.Key != Key.Enter || string.IsNullOrWhiteSpace(_inputTextBox.Text)) return;
//         WriteToHistory(_inputTextBox.Text);
//         // ExecuteCommand(_inputTextBox.Text);
//         _inputTextBox.Clear();
//     }
//
//     private async void StartListening()
//     {
//         await foreach (var message in _channel.Reader.ReadAllAsync())
//         {
//             WriteToHistory(message);
//         }
//     }
//
//     // private void ExecuteCommand(string command)
//     // {
//     //     foreach (var consoleCommand in _commands)
//     //     {
//     //         if (consoleCommand.CanExecute(command))
//     //         {
//     //             var stringBuilder = new StringBuilder();
//     //             stringBuilder.AppendLine();
//     //             stringBuilder.AppendLine($"Running command: {consoleCommand.CommandName}");
//     //             try
//     //             {
//     //                 var result = consoleCommand.Execute(command);
//     //                 stringBuilder.AppendLine(result);
//     //             }
//     //             catch (Exception e)
//     //             {
//     //                 stringBuilder.AppendLine($"An error occurred: {e.Message}");
//     //             }
//     //             WriteToHistory(stringBuilder.ToString());
//     //         }
//     //     }
//     // }
// }