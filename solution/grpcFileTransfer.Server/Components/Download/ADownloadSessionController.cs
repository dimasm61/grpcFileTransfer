using Grpc.Core;
using grpcFileTransfer.Model;
using grpcFileTransfer.Model.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace grpcFileTransfer.Server.Components
{
    /// <summary>
    /// Each session is a download of one file from one client.
    /// The client can download the file in several streams, named "client call".
    /// </summary>
    public class ADownloadSessionController
    {
        public SmLocker Locker = new SmLocker("SessionLck");

        private readonly Dictionary<int, ADownloadSessionTh> _sessionDict = new Dictionary<int, ADownloadSessionTh>();

        IFileTransferSettingsServer _sett;


        public void Init(IFileTransferSettingsServer sett)
        {
            _sett = sett;
        }

        public bool SessionKeyIsExists(int sessionKey)
        {
            if (!Locker.TryLock(20000))
                throw new Exception("Can't get lock");
            try
            {
                return _sessionDict.ContainsKey(sessionKey);
            }
            finally
            {
                Locker.ReleaseLock();
            }
        }

        public ADownloadSessionTh StartNewSession(
            int sessionKey
            , string relativeFilePath
            , IServerStreamWriter<FileDownloadResponse> responseStream
            , ServerCallContext context

        )
        {
            if (!Locker.TryLock(20000))
                throw new Exception("Can't get lock");

            ADownloadSessionTh downloadSession = null;
            try
            {
                // create new file download session
                downloadSession = new ADownloadSessionTh(
                    _sett
                    , sessionKey
                    , relativeFilePath
                    , responseStream
                    , context
                    );

                _sessionDict.Add(sessionKey, downloadSession);
            }
            finally
            {
                Locker.ReleaseLock();
            }

            // run it
            downloadSession.Start();

            // return
            return downloadSession;
        }

        public ADownloadSessionTh JoinToSession(
            int sessionKey
            , IServerStreamWriter<FileDownloadResponse> responseStream
            , ServerCallContext context )
        {
            if (!Locker.TryLock(20000))
                throw new Exception("Can't get lock");

            try
            {
                // find exists session
                var downloadSession = _sessionDict[sessionKey];

                // join
                downloadSession.JoinClientCall(sessionKey, context, responseStream);

                //
                return downloadSession;
            }
            finally
            {
                Locker.ReleaseLock();
            }

        }

        /// <summary>
        /// Interrupt file download
        /// </summary>
        /// <param name="sessionKey"></param>
        public void FinishSession(int sessionKey)
        {
            if (!Locker.TryLock(20000))
                throw new Exception("Can't get lock");

            try
            {
                if (!_sessionDict.ContainsKey(sessionKey))
                    return;

                var downloadSession = _sessionDict[sessionKey];

                // interrupt
                downloadSession.ForceFinishDOwnload();

            }
            finally
            {
                Locker.ReleaseLock();
            }
        }


    }
}
