using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleVideoPlayer.Extensions
{
    #region 防抖器类

    public class Debouncer
    {
        private readonly int _delayMilliseconds;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly object _lock = new object();

        public Debouncer(int delayMilliseconds = 500)
        {
            _delayMilliseconds = delayMilliseconds;
        }

        public void Debounce(Action action)
        {
            lock (_lock)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
            }

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_delayMilliseconds, _cancellationTokenSource.Token);
                    
                    if (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        action?.Invoke();
                    }
                }
                catch (TaskCanceledException)
                {
                }
            }, _cancellationTokenSource.Token);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
        }
    }

    #endregion

    #region 扩展方法

    public static class DebounceExtensions
    {
        private static readonly DebounceCache _cache = new DebounceCache();

        public static void Debounce(this Action action, int delayMilliseconds = 500, string key = "default")
        {
            _cache.Debounce(action, delayMilliseconds, key);
        }

        public static void Debounce<T>(this Action<T> action, T value, int delayMilliseconds = 500, string key = "default")
        {
            _cache.Debounce(() => action?.Invoke(value), delayMilliseconds, key);
        }
    }

    #endregion

    #region 防抖缓存

    internal class DebounceCache : IDisposable
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Debouncer> _debouncers;

        public DebounceCache()
        {
            _debouncers = new System.Collections.Concurrent.ConcurrentDictionary<string, Debouncer>();
        }

        public void Debounce(Action action, int delayMilliseconds, string key)
        {
            var debouncer = _debouncers.GetOrAdd(key, _ => new Debouncer(delayMilliseconds));
            debouncer.Debounce(action);
        }

        public void Dispose()
        {
            foreach (var debouncer in _debouncers.Values)
            {
                debouncer.Dispose();
            }
            _debouncers.Clear();
        }
    }

    #endregion
}
