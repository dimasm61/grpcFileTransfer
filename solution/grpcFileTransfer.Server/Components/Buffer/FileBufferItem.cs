using System;

namespace grpcFileTransfer.Server.Components
{
    public class FileBufferItem : IDisposable
    {
        public BuffStateEnum BuffState = BuffStateEnum.Created;

        public uint PartNum;

        public byte[] Data;

        public byte[] Checksum;

        public int Len => Data?.Length ?? 0;

        public void Dispose()
        {
            if (Data == null) return;
            for (var i = 0; i < Data.Length; i++, Data[i] = 0) ;
            Data = null;
        }

        public override string ToString() => $"{PartNum}:{BuffState}, len:{Len}";
    }
}
