using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace grpcFileTransfer.Model.Classes
{
    public class SmLocker
    {
        public object Tag;

        public string Name { get; private set; }

        private SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

        public SmLocker(string name)
        {
            Name = name;
        }

        public int LockTaskId { get; private set; } = -1;

        public bool IsLocked => _locker.CurrentCount == 0;

        public async Task<bool> TryLockAsync(int waitMs, CancellationToken ct)
        {
            var result = await _locker.WaitAsync(waitMs, ct);
            if (result) LockTaskId = Task.CurrentId ?? 0;
            return result;
        }

        public async Task<bool> TryLockAsync(int waitMs)
        {
            var result = await _locker.WaitAsync(waitMs);
            if (result) LockTaskId = Task.CurrentId ?? 0;
            return result;
        }

        public bool TryLock(int waitMs)
        {
            var result = _locker.Wait(waitMs);
            if (result) LockTaskId = Task.CurrentId ?? 0;
            return result;
        }

        public async Task<bool> WaitWhileLockAsync(int waitMs)
        {
            var step = 10;//ms
            var cn = 0;
            while (_locker.CurrentCount == 0 && cn < waitMs)
            {
                await Task.Delay(step);
                cn += step;
            }

            return _locker.CurrentCount > 0;
        }


        public void ReleaseLock()
        {
            LockTaskId = -1;
            if (_locker.CurrentCount == 0)
            {
                _locker.Release();
            }
            else
            {

            }
        }

        public override string ToString() => IsLocked ? $"{Name}:Locked" : $"{Name}:Free";
    }
}
