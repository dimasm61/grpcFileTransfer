using Google.Protobuf;
using Grpc.Core;
using grpcFileTransfer.Model;
using grpcFileTransfer.Model.Classes;
using System.Threading;
using System.Threading.Tasks;

namespace grpcFileTransfer.Server.Components
{
    /// <summary>
    /// The class should store client connnection context and writing
    /// data from buffer class to response stream.
    /// </summary>
    internal class ClientCallTh : SmThread
    {
        private int _sessionKey;

        private FileBufferController _bufferController;

        private IServerStreamWriter<FileDownloadResponse> _responseStream;

        private ServerCallContext _callContext;

        public string Peer { get; private set; }

        public ClientCallTh(
              int sessionKey
              , ServerCallContext callContext
              , IServerStreamWriter<FileDownloadResponse> responseStream
              , FileBufferController bufferController
            )
        {
            _sessionKey = sessionKey;
            Peer = _callContext.Peer;
            _responseStream = responseStream;
            _callContext = callContext;
            _bufferController = bufferController;
        }

        private FileBufferItem _currentFileBufferItem;

        protected override async Task DoStepAsync(CancellationToken ct)
        {
            // 1. get unreaded buffer
            // 2. tag as reading
            // 3. write to client response stream
            // 4. tag buffer as downloaded

            // if this is first step
            if (_currentFileBufferItem == null
             // or buffer already sended
             || _currentFileBufferItem?.BuffState == BuffStateEnum.Sended)
            {
                // get next buffer
                _currentFileBufferItem = await _bufferController.GetNextBufferForAsync(
                     getStateList: new[] { BuffStateEnum.Loaded }
                    , newState: BuffStateEnum.Sending
                    , lockTimeoutMsec: 2000
                    , getTimeoutMsec: 2000
                    , ct);
            }//

            // if "get next buffer" is failed - return and try on next step
            if (_currentFileBufferItem == null)
                return;

            // send buffer to stream
            await SendBufferAsync(_sessionKey, _currentFileBufferItem, _responseStream, ct);

            return;
        }

        private static async Task SendBufferAsync(
              int sessionKey
            , FileBufferItem currentFileBufferItem
            , IServerStreamWriter<FileDownloadResponse> responseStream
            , CancellationToken ct
            )
        {
            if (ct.IsCancellationRequested) return;

            var filePart = new FileDownloadResponse
            {
                SessionKey = sessionKey,
                MsgType = FileDownloadResponse.Types.FileDownloadResponseType.DataPartMsg,
                PartData = new FileDownloadResponse.Types.DataPartItem()
                {
                    FilePartData = ByteString.CopyFrom(currentFileBufferItem.Data),
                    FilePartLen = (uint)currentFileBufferItem.Data.Length,
                    FilePartCheckSum = null,
                    FilePartNum = currentFileBufferItem.PartNum
                },

            };

            try
            {
                if (ct.IsCancellationRequested) return;

                await responseStream.WriteAsync(filePart);
            }
            finally
            {

            }
        }
    }
}
