using System.Diagnostics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Rake.Core.Downloading;

/// <summary>
/// A <see cref="IProgress{T}"/> implementation that reports progress updates
/// to the captured <see cref="SynchronizationContext"/> one by one.
/// </summary>
[PublicAPI]
public class BufferedProgress<T> : IProgress<T>
{
    private readonly List<Entry> _buffer = [];
    private readonly int _boundedCapacity = 1;
    private readonly Action<T> _handler;
    private readonly TaskScheduler _scheduler;

    private record struct Entry(T Value, TaskCompletionSource? Tcs);

    public BufferedProgress(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = handler;
        _scheduler = SynchronizationContext.Current is not null
            ? TaskScheduler.FromCurrentSynchronizationContext()
            : TaskScheduler.Default;
    }

    public int BoundedCapacity
    {
        get => _boundedCapacity;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(BoundedCapacity));
            _boundedCapacity = value;
        }
    }

    public void Report(T value)
    {
        TaskCompletionSource? discardedTcs = null;
        var startNewTask = false;
        lock (_buffer)
        {
            if (_buffer.Count > _boundedCapacity)
            {
                // The maximum size of the buffer has been reached.
                if (_boundedCapacity == 0)
                    return;
                Debug.Assert(_buffer.Count >= 2);
                // Discard the oldest inert entry in the buffer, located in index 1.
                // The _buffer[0] is the currently running entry.
                // The currently running entry removes itself when it completes.
                discardedTcs = _buffer[1].Tcs;
                _buffer.RemoveAt(1);
            }
            _buffer.Add(new Entry(value, null));
            if (_buffer.Count == 1)
                startNewTask = true;
        }
        discardedTcs?.SetCanceled(); // Notify any waiter of the discarded value.
        if (startNewTask)
            StartNewTask(value);
    }

    private void StartNewTask(T? value)
    {
        // The starting of the Task is offloaded to the ThreadPool. This allows the
        // UI thread to take a break, so that the UI remains responsive.

        // The Post method is async void, so it never throws synchronously.
        // The async/await below could be omitted, because the Task will always
        // complete successfully.
        ThreadPool.QueueUserWorkItem(
            state =>
                Task.Factory.StartNew(
                    Post,
                    state,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    _scheduler
                ),
            value
        );
    }

#pragma warning disable CS1998
    // Since this method is async void, and is executed by a scheduler connected
    // to the captured SynchronizationContext, any error thrown by the _handler
    // is rethrown on the captured SynchronizationContext.
    private async void Post(object? state)
#pragma warning restore CS1998
    {
        try
        {
            if (state is null)
                return;
            _handler((T)state);
        }
        finally
        {
            TaskCompletionSource? finishedTcs;
            (T Value, bool HasValue) next = default;
            lock (_buffer)
            {
                // Remove the finished value from the buffer, and start the next value.
                Debug.Assert(_buffer.Count > 0);
                Debug.Assert(Equals(_buffer[0].Value, state));
                finishedTcs = _buffer[0].Tcs;
                _buffer.RemoveAt(0);
                if (_buffer.Count > 0)
                    next = (_buffer[0].Value, true);
            }
            finishedTcs?.SetResult(); // Notify any waiter of the finished value.
            if (next.HasValue)
                StartNewTask(next.Value);
        }
    }

    /// <summary>
    /// Returns a Task that will complete successfully when the last value
    /// added in the buffer is processed by the captured SynchronizationContext.
    /// In case more values are reported, causing that value to be discarded because
    /// of lack of empty space in the buffer, the Task will complete as canceled.
    /// </summary>
    public Task WaitToFinish()
    {
        lock (_buffer)
        {
            if (_buffer.Count == 0)
                return Task.CompletedTask;
            var span = CollectionsMarshal.AsSpan(_buffer);
            // ^1 is the last index in the buffer.
            return (
                span[^1].Tcs ??= new TaskCompletionSource(
                    TaskCreationOptions.RunContinuationsAsynchronously
                )
            ).Task;
        }
    }
}
