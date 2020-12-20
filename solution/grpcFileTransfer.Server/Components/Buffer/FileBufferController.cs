using grpcFileTransfer.Model.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace grpcFileTransfer.Server.Components
{
    public class FileBufferController
    {
        private uint _bufferCount = 1;
        private uint _bufferSizeByte = 1;
        private bool _isStartLoadCompleted = false;

        public SmLocker Locker = new SmLocker("BuffLck");

        private SmLocker _loadLocker = new SmLocker("LoadLck");

        CancellationToken _ct;

        public bool FileHasBeenReaded { get; private set; }

        public long StreamLoadPosition { get; private set; }

        private readonly Dictionary<uint, FileBufferItem> _bufferDict = new Dictionary<uint, FileBufferItem>();

        /// <summary>list of states when you can to use the buffer with a new part of the file</summary>
        private static BuffStateEnum[] _freeBuffStates = new[] {
            BuffStateEnum.Created,
            BuffStateEnum.Sended
        };

        public int FreeBuffersCount
        {
            get
            {
                if (!Locker.TryLock(5000))
                    throw new Exception("Can't get lock");
                try
                {
                    return _bufferDict.Values.Count(c => _freeBuffStates.Contains(c.BuffState));
                }
                finally
                {
                    Locker.ReleaseLock();
                }
            }
        }

        public int NotSendedBuffersCount
        {
            get
            {
                if (!Locker.TryLock(5000))
                    throw new Exception("Can't get lock");
                try
                {
                    return _bufferDict.Values.Count(c => c.BuffState != BuffStateEnum.Sended);
                }
                finally
                {
                    Locker.ReleaseLock();
                }
            }
        }


        public void Init(uint bufferCount, uint bufferSizeByte, CancellationToken ct)
        {
            if (!Locker.TryLock(5000))
                throw new Exception("Can't get lock");
            try
            {
                _bufferCount = bufferCount;
                _bufferSizeByte = bufferSizeByte;
                _ct = ct;

                for (uint i = 0; i < _bufferCount; i++)
                {
                    var bufferItem = new FileBufferItem()
                    {
                        PartNum = i,
                        Data = new byte[_bufferSizeByte]

                    };
                    _bufferDict.Add(i, bufferItem);
                }
            }
            finally
            {
                Locker.ReleaseLock();
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileStream"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task LoadBuffersAfterInitAsync(int lockTimeoutMsec, Stream fileStream, CancellationToken ct)
        {
            if (_isStartLoadCompleted)
                throw new Exception($"method {nameof(LoadBuffersAfterInitAsync)} has already been called");

            if (!await Locker.TryLockAsync(lockTimeoutMsec))
                throw new Exception("Can't get lock");

            try
            {
                foreach (var bufferItem in _bufferDict.Values)
                {
                    await LoadBufferItemFfromStreamAsync(lockTimeoutMsec, bufferItem, fileStream, bufferItem.PartNum, ct, false);
                }

                _isStartLoadCompleted = true;
            }
            finally
            {
                Locker.ReleaseLock();
            }
        }

        public async Task<FileBufferItem> LoadNextFreeBufferAsync(int lockTimeoutMsec, Stream fileStream, CancellationToken ct)
        {
            FileBufferItem bufferItem = null;
            uint newPartNum = 0;

            if (!await Locker.TryLockAsync(5000, _ct))
                return bufferItem;

            try
            {
                // try get free buffer
                bufferItem = await GetNextBufferForAsync(
                    getStateList: _freeBuffStates
                    , newState: BuffStateEnum.Loading
                    , lockTimeoutMsec: 2000
                    , getTimeoutMsec: 2000
                    , ct: ct
                    , isNeedLock: false);

                if (bufferItem == null)
                    return null;

                // if free buffer exists

                var lastPartNum = _bufferDict.Values.Max(c => c.PartNum);

                newPartNum = lastPartNum + 1;

                // reinsert with new PartNum
                _bufferDict.Remove(bufferItem.PartNum);

                _bufferDict.Add(newPartNum, bufferItem);
            }
            finally
            {
                Locker.ReleaseLock();
            }

            // fill next file part
            await LoadBufferItemFfromStreamAsync(lockTimeoutMsec, bufferItem, fileStream, newPartNum, ct, true);

            return bufferItem;
        }

        private async Task LoadBufferItemFfromStreamAsync(
            int lockTimeoutMsec
            , FileBufferItem bufferItem
            , Stream fileStream
            , uint newPartNum
            , CancellationToken ct
            , bool isNeedLock)
        {
            if (!await _loadLocker.TryLockAsync(5000, _ct))
                return;

            try
            {

                using var md5 = MD5.Create();

                var fileOffset = (int)(_bufferSizeByte * newPartNum);

                fileStream.Seek(fileOffset, SeekOrigin.Begin);

                // read part of file to buffer
                await fileStream.ReadAsync(bufferItem.Data, 0, bufferItem.Data.Length, ct);

                // calc checksum
                bufferItem.Checksum = md5.ComputeHash(bufferItem.Data, 0, bufferItem.Data.Length);

                // update partNum
                bufferItem.PartNum = newPartNum;

                //bufferItem.BuffState = BuffStateEnum.Loaded;

                // check for end of file
                FileHasBeenReaded = (fileOffset + _bufferSizeByte) >= fileStream.Length;

                var lastSourceBuffPos = fileOffset + _bufferSizeByte;

                if (lastSourceBuffPos > StreamLoadPosition) StreamLoadPosition = lastSourceBuffPos;
            }
            finally
            {
                _loadLocker.ReleaseLock();
            }

            await SetBufferStateAsync(lockTimeoutMsec, bufferItem, BuffStateEnum.Loaded, false);
        }

        public Task<FileBufferItem> GetNextBufferForAsync(
              BuffStateEnum[] getStateList
            , BuffStateEnum newState
            , int lockTimeoutMsec
            , int getTimeoutMsec
            , CancellationToken ct)
        {
            return GetNextBufferForAsync(getStateList, newState, lockTimeoutMsec, getTimeoutMsec, ct, true);
        }

        private async Task<FileBufferItem> GetNextBufferForAsync(
              BuffStateEnum[] getStateList
            , BuffStateEnum newState
            , int lockTimeoutMsec
            , int getTimeoutMsec
            , CancellationToken ct
            , bool isNeedLock)
        {
            FileBufferItem result = null;

            var sw = Stopwatch.StartNew();

            while (result == null
                && sw.ElapsedMilliseconds < getTimeoutMsec
                && !ct.IsCancellationRequested
            )
            {
                // get lock
                if (isNeedLock)
                    if (!await Locker.TryLockAsync(lockTimeoutMsec, ct))
                        continue;
                try
                {
                    // try find buffer
                    result = _bufferDict.Values.FirstOrDefault(c => getStateList.Contains(c.BuffState));

                    // if did it
                    if (result != null)
                    {
                        // tag the buffer with new state
                        result.BuffState = newState;
                    }

                }
                finally
                {
                    if (isNeedLock)
                        Locker.ReleaseLock();
                }

                // if can't find - sleep and try again
                if (result == null)
                    await Task.Delay(1, ct);

            }// while not get timeout

            return result;
        }

        public Task SetBufferStateToSendedAsync(int lockTimeoutMsec, FileBufferItem bufferItem)
        {
            return SetBufferStateAsync(lockTimeoutMsec, bufferItem, BuffStateEnum.Sended, true);

            //if (bufferItem.BuffState != BuffStateEnum.Sending)
            //    throw new Exception("Tag 'Sended' be able to only 'Sending' buffer");

            //if (!await Locker.TryLockAsync(lockTimeoutMsec, _ct))
            //    return;
            //try
            //{
            //    bufferItem.BuffState = BuffStateEnum.Sended;
            //}
            //finally
            //{
            //    Locker.ReleaseLock();
            //}
        }

        private async Task SetBufferStateAsync(int lockTimeoutMsec, FileBufferItem buff, BuffStateEnum newState, bool isNeedLock)
        {
            if (isNeedLock)
                if (!await Locker.TryLockAsync(lockTimeoutMsec, _ct))
                    throw new Exception("Can't get lock");
            try
            {
                buff.BuffState = newState;
            }
            finally
            {
                if (isNeedLock)
                    Locker.ReleaseLock();
            }
        }


    }
}
