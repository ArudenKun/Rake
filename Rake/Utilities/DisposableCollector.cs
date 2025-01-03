using System;
using System.Collections.Generic;
using System.Threading;
using Rake.Extensions;

namespace Rake.Utilities;

public class DisposableCollector : IDisposable
{
    private readonly Lock _lock = new();
    private readonly List<IDisposable> _items = [];

    public void Add(IDisposable item)
    {
        lock (_lock)
        {
            _items.Add(item);
        }
    }

    public void AddRange(IEnumerable<IDisposable> items)
    {
        lock (_lock)
        {
            _items.AddRange(items);
        }
    }

    public void AddRange(params IDisposable[] items)
    {
        lock (_lock)
        {
            _items.AddRange(items);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _items.DisposeAll();
            _items.Clear();
        }
    }
}
