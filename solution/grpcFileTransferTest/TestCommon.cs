using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grpcFileTransferTest
{
    public class TestCommon
    {
        protected TestController TestContoller = new TestController();


        public void Dispose()
        {
            TestContoller.Dispose();
        }
    }
}
