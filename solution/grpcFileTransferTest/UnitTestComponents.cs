using grpcFileTransfer.Model.Classes;
using grpcFileTransfer.Server.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace grpcFileTransferTest
{
    public class UnitTestComponents
    {
        private readonly ITestOutputHelper output;

        public UnitTestComponents(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Test1()
        {
            var g = Guid.NewGuid().ToByteArray();

            int i = BitConverter.ToInt32(g);
        }

        internal class TestTh : SmThread
        {
            public int Counter = 0;
            ITestOutputHelper _output;
            public TestTh(ITestOutputHelper output) : base(1) { _output = output; }

            protected override Task DoStepAsync(CancellationToken ct)
            {
                Counter++;
                _output.WriteLine($"Work step: {Counter}");
                return Task.CompletedTask;
            }

            public override void Start(int delaySec = 0)
            {
                _output.WriteLine($"Call Start");
                base.Start(delaySec);
            }
            public override Task StartAsync(int delaySec = 0)
            {
                _output.WriteLine($"Call StartAsync");
                return base.StartAsync(delaySec);
            }
            public override void Stop()
            {
                _output.WriteLine($"Call Stop");
                base.Stop();
            }
            public override Task StopAsync()
            {
                _output.WriteLine($"Call StopAsync");
                return base.StopAsync();
            }
        }

        [Fact]
        public void SmThreadTest1()
        {
            var th = new TestTh(output);

            output.WriteLine($"Status: {th.TaskStatus}");
            th.Start();
            output.WriteLine($"Status: {th.TaskStatus}");
            while (th.Counter < 5)
            {
                Thread.Sleep(200);
                output.WriteLine($"Status: {th.TaskStatus}");
            }
            output.WriteLine($"Status: {th.TaskStatus}");

            th.Stop();
            output.WriteLine($"Status: {th.TaskStatus}");
        }
        [Fact]
        public async Task SmThreadTest2()
        {
            var th = new TestTh(output);

            output.WriteLine($"Status: {th.TaskStatus}");
            await th.StartAsync();
            output.WriteLine($"Status: {th.TaskStatus}");
            while (th.Counter < 5)
            {
                Thread.Sleep(200);
                output.WriteLine($"Status: {th.TaskStatus}");
            }
            output.WriteLine($"Status: {th.TaskStatus}");

            await th.StopAsync();
            output.WriteLine($"Status: {th.TaskStatus}");
        }


        [Fact]
        public async Task TestBufferAsync()
        {
            var buffCount = 10;
            var buffSize = 100 * 1024;
            var testBuffSize = 20 * 1024 * 1024;
            var cs = new CancellationTokenSource();
            var ct = cs.Token;

            var bufferController = new FileBufferController();

            var byteArraySource = new byte[testBuffSize];
            var byteArrayDest = new byte[testBuffSize];

            var rnd = new Random();

            rnd.NextBytes(byteArraySource);

            Stream stream = new MemoryStream();
            await stream.WriteAsync(byteArraySource, 0, byteArraySource.Length);

            bufferController.Init((uint)buffCount, (uint)buffSize, ct);


            // first time fill
            await bufferController.LoadBuffersAfterInitAsync(2500, stream, ct);

            // all buffers should be loaded

            var cn = bufferController.FreeBuffersCount;

            Assert.Equal(0, cn);

            // sending 'thread'
            var taskSend = Task.Run(async () =>
            {
                try
                {
                    while (!ct.IsCancellationRequested
                        && !(bufferController.FileHasBeenReaded && bufferController.NotSendedBuffersCount == 0)
                        )
                    {
                        // get buffer for sending
                        var buff = await bufferController.GetNextBufferForAsync(
                            new[] { BuffStateEnum.Loaded }, BuffStateEnum.Sending, 1000, 1000, ct);

                        if (buff == null)
                        {
                            await Task.Delay(1);
                            continue;
                        }

                        // send to dest buff
                        var shift = buff.PartNum * buff.Len;

                        output.WriteLine($"{DateTime.Now:HH:mm:ss.fff}  Sending part {buff.PartNum}, shift:{shift}, len:{buff.Len}");
                        var len = (shift + buff.Len < byteArrayDest.Length) ? buff.Len : byteArrayDest.Length - shift;
                        Array.Copy(buff.Data, 0, byteArrayDest, shift, len);

                        // tag as 'sended'
                        await bufferController.SetBufferStateToSendedAsync(10000, buff);
                    }
                }
                catch (Exception ex)
                {

                    throw;
                }
            });

            await Task.Delay(1000);

            while (bufferController.StreamLoadPosition < byteArraySource.Length - 10)
            {
                await bufferController.LoadNextFreeBufferAsync(4000, stream, ct);

                var prst = (double)bufferController.StreamLoadPosition / (double)byteArraySource.Length * 100.0;

                output.WriteLine($"{DateTime.Now:HH:mm:ss.fff}  Filling ... {prst:#00.0}%");
                await Task.Delay(1);
            }


            // both source and dest arrays should be equals

            using var md5 = MD5.Create();

            var chk1 = md5.ComputeHash(byteArraySource, 0, byteArraySource.Length);
            var chk2 = md5.ComputeHash(byteArrayDest, 0, byteArrayDest.Length);

            var b = true;
            for (var i = 0; i < chk1.Length; i++)
                b &= (chk1[i] == chk2[i]);

            Assert.True(b);

            await taskSend;

            await stream.DisposeAsync();

            byteArraySource = null;
        }
    }
}
