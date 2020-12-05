using System;

namespace grpcFileTransfer.Model
{
    public interface IFileTransferSettingsCommon
    {
        /// <summary>
        /// A tmp folder for compresing files. c:/ProgramData/FileTransfer/TmpCompress
        /// </summary>
        string ProgramDataTmpCompresPath { get; }

        /// <summary>
        /// A tmp folder for transfer files. c:/ProgramData/FileTransfer/TmpTransfer
        /// </summary>
        string ProgramDataTmpTransferPath { get; }

        void Reload();

        bool IsLoaded{ get; }
    }
}
