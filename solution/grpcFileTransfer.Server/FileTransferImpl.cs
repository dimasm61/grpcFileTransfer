using grpcFileTransfer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grpcFileTransfer.Server
{
    public class FileTransferImpl : FileTransfer.FileTransferBase
    {
        public void Init(IFileTransferSettingsServer sett)
        { }
    }
}
