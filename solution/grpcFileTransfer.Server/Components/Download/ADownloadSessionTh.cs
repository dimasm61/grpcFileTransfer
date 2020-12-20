using Grpc.Core;
using grpcFileTransfer.Model;
using grpcFileTransfer.Model.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace grpcFileTransfer.Server.Components
{

    /// <summary>
    /// This class control download of one file. There is some buffers, 
    /// file info class, and list of client connections (for multistream downloading)
    /// </summary>
    public class ADownloadSessionTh : SmThread
    {
        public SmLocker Locker = new SmLocker("SessionLck");

        public int SessionKey { get; private set; }

        /// <summary>file info class</summary>
        private AFileInfo _fileReader;

        /// <summary>buffer controller with some buffers</summary>
        private FileBufferController _bufferController;

        /// <summary>settings</summary>
        IFileTransferSettingsServer _sett;

        /// <summary>client connection list</summary>
        private readonly Dictionary<string, ClientCallTh> _clientCallDict = new Dictionary<string, ClientCallTh>();

        public ADownloadSessionTh(
                IFileTransferSettingsServer sett
            , int sessionKey
            , string relativeFilePath
            , IServerStreamWriter<FileDownloadResponse> responseStream
            , ServerCallContext callContext
        ) : base(1)
        {
            if (!Locker.TryLock(20000))
                throw new Exception("Can't get lock");
            try
            {
                SessionKey = sessionKey;

                _sett = sett;

                var filePath = Path.Combine(_sett.RootDownloadPath, relativeFilePath);

                _fileReader = new AFileInfo(filePath);
                _fileReader.OpenFile();

                _bufferController = new FileBufferController();

                uint bufferSize = 100 * 1024; // 100kb

                uint bufferCount = (uint)(_fileReader.LoadedFileInfo.Length / bufferSize);

                // buffer count depend on file size, but value should be between 1 and 10
                bufferCount = (bufferCount < 1) ? 1 : bufferCount;
                bufferCount = (bufferCount > 10) ? 10 : bufferCount;

                // create buffers
                _bufferController.Init(bufferCount, bufferSize, _ct);

                // remember client call context
                var clientCall = new ClientCallTh(sessionKey, callContext, responseStream, _bufferController);
                _clientCallDict.Add(clientCall.Peer, clientCall);

                clientCall.Start();
            }
            finally
            {
                Locker.ReleaseLock();
            }
        }

        public void JoinClientCall(
        int sessionKey
        , ServerCallContext callContext
        , IServerStreamWriter<FileDownloadResponse> responseStream
        )
        {
            if (!Locker.TryLock(20000))
                throw new Exception("Can't get lock");

            try
            {
                var clientCall = new ClientCallTh(sessionKey, callContext, responseStream, _bufferController);

                _clientCallDict.Add(clientCall.Peer, clientCall);

                clientCall.Start();
            }
            finally
            {
                Locker.ReleaseLock();
            }
        }

        protected override async Task DoStepAsync(CancellationToken ct)
        {
            // thread step
            // are needed to check that 'client call' is sending buffers.
            // when detected new (joined) 'client call' then start it
            // when all 'client calls' is finished and buffer is clear and file read
            // have completed then stop self
            
            // if any buffer is free
            //while(_bufferController.HasFreeBuffer)
            //{
            //    // load next file part
            //    await _bufferController.FillNextFreeBufferAsync(_fileReader.AFileStream, ct);
            //}

            //// if file is read and all buffers are sent
            //if(_bufferController.AllBuffersIsSended && _bufferController.FileHasBeenReaded)
            //{
            //    await StopAll();
            //}

        }

        public Task WaitFinish()
        {
            // wait while have not finished:
            // - loading file into buffers
            // - sending buffers to clients
            
            Task.WaitAll(WorkTask);

            return Task.CompletedTask;
        }

        public Task ForceFinishDOwnload()
        {
            // interrupt download - need stop all sending thread and file load threads

            return StopAll();
        }

        private async Task StopAll()
        {
            if (!await Locker.TryLockAsync(20000))
                throw new Exception("Can't get lock");

            try
            {
                foreach (var client in _clientCallDict.Values)
                    await client.StopAsync();
            }
            finally
            {
                Locker.ReleaseLock();
            }

            await this.StopAsync();

            _fileReader.CloseFile();
        }
    }
}
