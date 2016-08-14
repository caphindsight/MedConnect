using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MedConnectMongoLib;

namespace MedConnectBot.Tele {
    public static class GlobalCache {
        public static readonly Cache<Room> CurrentRoomCache =
            new Cache<Room>(TimeSpan.FromMinutes(BotConfig.Data.Caching.CurrentRoomCacheInvalidationTimeMinutes));
    }

    public sealed class Cache<T>
        where T: class
    {
        public Cache(TimeSpan invalidationTime) {
            InvalidationTime = invalidationTime;
        }

        public TimeSpan InvalidationTime { get; private set; }

        private readonly Dictionary<long, CacheItem<T>> Dict_ =
            new Dictionary<long, CacheItem<T>>();

        public T Get(long id) {
            lock (this) {
                if (Dict_.ContainsKey(id)) {
                    CacheItem<T> cacheItem = Dict_[id];
                    return cacheItem.Get();
                } else {
                    return null;
                }
            }
        }

        public void Set(long id, T val) {
            lock (this) {
                if (!Dict_.ContainsKey(id)) {
                    Dict_[id] = new CacheItem<T>(InvalidationTime);
                }

                CacheItem<T> cacheItem = Dict_[id];
                cacheItem.Set(val);
            }
        }

        public async Task<T> GetOrUpdate(long id, Func<long, Task<T>> valF) {
            T val = Get(id);

            if (val == null) {
                val = await valF(id);
                Set(id, val);
            }

            return val;
        }
    }

    public sealed class CacheItem<T>
        where T: class
    {
        public CacheItem(TimeSpan invalidationTime) {
            InvalidationTime = invalidationTime;
            Data_ = null;
        }

        public TimeSpan InvalidationTime { get; private set; }

        private T Data_;
        private DateTime LastAccess_;

        public T Get() {
            if (Data_ == null) {
                return null;
            }

            DateTime now = DateTime.Now;
            TimeSpan lasting = now - LastAccess_;

            if (lasting > InvalidationTime) {
                Data_ = null;
            }

            return Data_;
        }

        public void Set(T data) {
            LastAccess_ = DateTime.Now;
            Data_ = data;
        }
    }
}
