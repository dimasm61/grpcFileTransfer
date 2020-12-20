using Microsoft.VisualStudio.Threading;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace grpcFileTransfer.Model.Classes
{
    /// <summary>
    /// Task based "stream" for long running works. It's some sort of background worker.
    /// </summary>
    public class SmThread
    {
        ILogger _logger = Serilog.Log.Logger;

        private AsyncAutoResetEvent _wh = new AsyncAutoResetEvent(false);

        private System.Threading.Timer _wakeupTimer;

        protected Task WorkTask { get; private set; }

        public TaskStatus TaskStatus => WorkTask?.Status ?? TaskStatus.WaitingForActivation;

        private readonly TaskFactory _taskFactory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.ExecuteSynchronously);

        protected CancellationTokenSource _cts;

        protected CancellationToken _ct;

        protected TimeSpan StepInterval { get; private set; } = TimeSpan.FromSeconds(5);

        private SmLocker _startStopLocker = new SmLocker("StartStopLck");

        private static TaskStatus[] _taskNeedStart = new[] {
             TaskStatus.Canceled
            , TaskStatus.Created
            , TaskStatus.Faulted
            , TaskStatus.RanToCompletion 
            //, TaskStatus.Running
            //, TaskStatus.WaitingForActivation
            //, TaskStatus.WaitingToRun
            //, TaskStatus.WaitingForChildrenToComplete
        };

        public SmThread() { }

        public SmThread(int stepIntervalSecond)
        {
            StepInterval = TimeSpan.FromSeconds(stepIntervalSecond);
        }

        private void WakeupTick(object state)=> _wh.Set();
  
        public void DoForceStep() => _wh.Set();

        public virtual async Task StartAsync(int delaySec = 0)
        {
            if (!await _startStopLocker.TryLockAsync(2000))
                throw new Exception("Не удалось получить блокивроку для запуска потока");
            try
            {
                // если поток уже есть
                if (WorkTask != null)
                {
                    // и он работает
                    if (!_taskNeedStart.Contains(WorkTask.Status))
                    {
                        // то запускать снова его ненужно
                        return;//_task;
                    }
                }

                if (_wakeupTimer != null)
                    _wakeupTimer.Dispose();

                _wakeupTimer = new System.Threading.Timer(WakeupTick, null, TimeSpan.FromSeconds(delaySec), StepInterval);

                //BeforeStartWrapper();

                _cts = new CancellationTokenSource();
                _ct = _cts.Token;

                WorkTask = _taskFactory.StartNew(() =>
                {
                    WorkLoopAsync().Wait();
                });

                //AfterStartWrapper();
            }
            catch (Exception ex)
            {
                _logger.Error("Error: {ex}", ex);
            }
            finally
            {
                _startStopLocker.ReleaseLock();
            }
            //return _task;
        }

        public virtual void Start(int delaySec = 0)
        {
            if (!Monitor.TryEnter(_startStopLocker, 2000))
                throw new Exception("Не удалось получить блокивроку для запуска потока");
            try
            {
                // if Task exists
                if (WorkTask != null)
                {
                    // and it works
                    if (!_taskNeedStart.Contains(WorkTask.Status))
                    {
                        // shouldn't start again
                        return;
                    }
                }

                if (_wakeupTimer != null) _wakeupTimer.Dispose();

                _wakeupTimer = new System.Threading.Timer(WakeupTick, null, TimeSpan.FromSeconds(delaySec), StepInterval);

               // BeforeStartWrapper();

                _cts = new CancellationTokenSource();
                _ct = _cts.Token;

                WorkTask = _taskFactory.StartNew(async () => await WorkLoopAsync());

                //AfterStartWrapper();
            }
            catch (Exception ex)
            {
                _logger.Error("Error: {ex}", ex);
            }
            finally
            {
                Monitor.Exit(_startStopLocker);
            }
        }

        public virtual async Task StopAsync()
        {
            if (!Monitor.TryEnter(_startStopLocker, 5000))
                throw new Exception("Can't get start/stop locker");
            try
            {
                _wakeupTimer?.Dispose();
                _cts?.Cancel();
                _wh?.Set();

                try
                {
                    if (WorkTask != null)
                    {
                        Task.WaitAny(WorkTask);
                        //await _task.ConfigureAwait(true);
                        //_task.GetAwaiter().GetResult();
                        WorkTask.Dispose();
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error: {ex}", ex);
            }
            finally
            {
                _startStopLocker.ReleaseLock();
            }
        }

        public virtual void Stop()
        {
            if (!Monitor.TryEnter(_startStopLocker, 5000))
                throw new Exception("Can't get start/stop locker");
            try
            {
                _wakeupTimer?.Dispose();
                _cts?.Cancel();
                _wh?.Set();

                try
                {
                    if (WorkTask != null)
                    {
                        //_task.Wait();
                        Task.WaitAll(WorkTask);
                        //_task?.GetAwaiter().GetResult();
                        WorkTask?.Dispose();
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error: {ex}", ex);
            }
            finally
            {
                Monitor.Exit(_startStopLocker);
            }
        }

        public virtual async Task WorkLoopAsync()
        {
            try
            {
                await _wh.WaitAsync(_ct);
            }
            catch (TaskCanceledException)
            {
                //Logger.Error($"catch while WaitAsync {nameof(TaskCanceledException)}");
            }

            while (true)
            {
                if (_cts.IsCancellationRequested)
                {
                    //Logger.LogDebug($"IsCancellationRequested = true, finishing...");
                    break;
                }

                try
                {
                    await DoStepAsync(_ct);

                    if (_cts.IsCancellationRequested)
                    {
                        //Logger.LogDebug($"IsCancellationRequested = true, finishing...");
                        break;
                    }

                    //Console.WriteLine($"Wait in {nameof(WorkLoop)} step...");

                    await _wh.WaitAsync(_ct);
                }
                catch (TaskCanceledException)
                {
                    //Console.WriteLine($"catch {nameof(TaskCanceledException)}");
                }
                catch (OperationCanceledException)
                {
                    //Console.WriteLine($"catch {nameof(OperationCanceledException)}");
                }
                catch (ThreadAbortException)
                {
                    //Console.WriteLine($"catch {nameof(ThreadAbortException)}");
                }
                catch (Exception ex)
                {
                    _logger.Error("Error: {ex}", ex);
                }
            }// while true

        }

        protected virtual async Task DoStepAsync(CancellationToken ct)
        {
            await Task.Delay(0);
            throw new NotImplementedException();
        }

        //private void BeforeStartWrapper(params object[] args)
        //{
        //    try
        //    {
        //        BeforeStart(args);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error("Error: {ex}", ex);
        //    }
        //}

        //private void AfterStartWrapper(params object[] args)
        //{
        //    try
        //    {
        //        AfterStart(args);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error("Error: {ex}", ex);
        //    }
        //}

        //public virtual void BeforeStart(params object[] args)
        //{

        //}

        //public virtual void AfterStart(params object[] args)
        //{

        //}

        //public virtual void AfterStop(params object[] args)
        //{

        //}

    }
}
