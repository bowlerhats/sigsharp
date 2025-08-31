using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SigSharp.Nodes;
using SigSharp.Utils;

namespace SigSharp;

public class SignalEffect : TrackingSignalNode
{
    public SignalEffectOptions Options { get; }
    
    public CancellationToken StopToken { get; }

    public bool IsAutoRunning { get; private set; } = true;

    public override bool DisposedBySignalGroup => true;
    
    private readonly Stopwatch _scheduleTimer = new();
    
    private readonly long _debounceMs;
    private readonly int _runTimeLimitMs;
    private readonly int _runTotalTimeLimitMs;
    private readonly int _rerunDelayMs;
    private readonly int _rescheduleDelayMs;
    
    private readonly Lock _timerLock = new();
    private readonly Lock _scheduleLock = new();
    private SemaphoreSlim _runLock = new(1);
    private AsyncManualResetEvent _idle = new(true);
    
    private SignalEffectFunctor _effectFunctor;
    private readonly ISignalEffectScheduler _effectScheduler;

    private readonly CancellationTokenRegistration _tokenRegistration;
    private TaskCompletionSource? _asTask;

    private bool _canSchedule = true;
    private Timer? _debounceTimer;
    
    internal SignalEffect(
        SignalGroup group,
        SignalEffectFunctor effectFunctor,
        string? name = null,
        SignalEffectOptions? options = null,
        bool skipAutoStart = false,
        CancellationToken stopToken = default)
        : base(group, false, name)
    {
        _effectFunctor = effectFunctor;
        
        this.Name = name;
        
        this.StopToken = stopToken;
        
        this.Options = options ?? SignalEffectOptions.Defaults;

        _debounceMs = (long)this.Options.DebounceChangedTime.TotalMilliseconds;
        ArgumentOutOfRangeException.ThrowIfNegative(_debounceMs);

        _runTimeLimitMs = (int)this.Options.RerunTimeLimit.TotalMilliseconds;
        ArgumentOutOfRangeException.ThrowIfNegative(_runTimeLimitMs);
        
        _runTotalTimeLimitMs = (int)this.Options.RerunYieldTimeLimit.TotalMilliseconds;
        ArgumentOutOfRangeException.ThrowIfNegative(_runTotalTimeLimitMs);
        
        _rerunDelayMs = (int)this.Options.RerunDelay.TotalMilliseconds;
        ArgumentOutOfRangeException.ThrowIfNegative(_rerunDelayMs);
        
        _rescheduleDelayMs = (int)this.Options.RescheduleDelay.TotalMilliseconds;
        ArgumentOutOfRangeException.ThrowIfNegative(_rescheduleDelayMs);

        _effectScheduler = this.Options.EffectScheduler ?? SignalEffectScheduler.Default;
        
        if (!skipAutoStart)
            this.AutoStart();
        
        _tokenRegistration = stopToken.Register(this.StopAutoRun);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _runLock.Wait(TimeSpan.FromSeconds(3));
            
            if (_asTask is not null)
            {
                if (!_asTask.Task.IsCompleted)
                {
                    _asTask.SetResult();
                }
            }
            
            _asTask = null;

            _tokenRegistration.Unregister();
            
            this.CancelTimer();
            
            _effectFunctor = default;
            
            _idle.Dispose();
            
            _runLock.Dispose();
            _runLock = null!;
        }
        
        base.Dispose(disposing);
    }

    protected virtual async ValueTask<SignalEffectResult> InvokeRunnerFunc()
    {
        if (!_effectFunctor.IsValid)
        {
            this.StopAutoRun();

            return SignalEffectResult.Stop();
        }

        var effectResult = await _effectFunctor.Invoke();

        return await this.HandleEffectResult(effectResult);
    }

    protected ValueTask<SignalEffectResult> HandleEffectResult(SignalEffectResult result)
    {
        if (result.ShouldStop || result.ShouldDestroy)
        {
            this.StopAutoRun();
        }

        if (result.ShouldDestroy)
        {
            Signals.Untracked(() =>
            {
                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(500, this.StopToken);
                    this.Dispose();
                }, this.StopToken);
            });
        }

        return ValueTask.FromResult(result);
    }
    
    protected override void OnDirty()
    {
        if (!this.IsAutoRunning)
            return;
        
        if (this.IsDisposed || this.StopToken.IsCancellationRequested)
            return;

        if (SignalTracker.Current?.AcceptEffects ?? false)
        {
            SignalTracker.Current.PostEffect(this);

            return;
        }
        
        lock (_scheduleLock)
        {
            if (!_canSchedule || !_idle.IsSet)
                return;
        }
        
        if (_debounceMs <= 0)
        {
            this.Schedule();
        
            return;
        }
        
        this.ReScheduleDebounced();
    }

    private void StartScheduling()
    {
        lock (_scheduleLock)
        {
            if (_canSchedule)
            {
                _canSchedule = false;
                        
                this.NotIdle();
            }
        }
    }

    public void StopAutoRun()
    {
        this.IsAutoRunning = false;

        lock (_scheduleLock)
        {
            if (_asTask is not null)
            {
                if (!_asTask.Task.IsCompleted)
                {
                    _asTask.SetResult();
                }
            }

            this.CancelTimer();
            
            this.Idle();
        }
    }

    public bool WaitIdle(CancellationToken? stopToken = null)
    {
        this.CheckDisposed();

        if (_idle.IsSet)
            return false;

        stopToken = stopToken.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(this.StopToken, stopToken.Value).Token
            : this.StopToken;
        
        _idle.Wait(stopToken.Value);
        
        return true;
    }

    public Task<bool> WaitIdleAsync(CancellationToken? stopToken = null)
    {
        this.CheckDisposed();

        if (_idle.IsSet)
            return Task.FromResult(false);

        stopToken = stopToken.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(this.StopToken, stopToken.Value).Token
            : this.StopToken;
        
        return _idle.WaitAsync(stopToken.Value)
            .ContinueWith(static _ => true, TaskContinuationOptions.ExecuteSynchronously);
    }
    
    public Task AsTask()
    {
        this.CheckDisposed();
        
        if (!this.IsAutoRunning)
            return Task.CompletedTask;

        lock (_scheduleLock)
        {
            _asTask ??= new TaskCompletionSource();

            return _asTask.Task;
        }
    }

    public void RunImmediate()
    {
        this.CheckDisposed();

        var shouldReschedule = this.DoRunSync();
        
        if (shouldReschedule)
            this.Schedule(true);
    }

    private void NotIdle()
    {
        _idle.Reset();
    }
    
    private void Idle()
    {
        lock (_timerLock)
        {
            if (_debounceTimer is not null)
                return;
            
            _idle.Set();
        }
    }

    public void Schedule()
    {
        this.Schedule(false);
    }

    protected void AutoStart()
    {
        if (this.Options.AutoSchedule)
        {
            if (!this.EnqueueAsSuspended())
            {
                this.Schedule();
            }
        }
    }

    private void ReScheduleDebounced()
    {
        if (this.ExtendDebounce())
            return;
        
        lock (_scheduleLock)
        {
            if (_canSchedule)
            {
                if (this.StartDebounce())
                {
                    this.StartScheduling();
                }
                else
                {
                    this.Schedule();
                }
            }
        }
    }
    
    private void Schedule(bool forced)
    {
        if (this.StopToken.IsCancellationRequested)
        {
            this.Idle();

            return;
        }
        
        lock (_scheduleLock)
        {
            if (!forced)
            {
                lock (_timerLock)
                {
                    if (_debounceTimer is not null)
                        return;
                }

                if (!_canSchedule)
                    return;

                if (this.EnqueueAsSuspended())
                {
                    _canSchedule = false;
                    
                    return;
                }
            }

            _canSchedule = false;
            
            this.NotIdle();
            
            try
            {
                Signals.Untracked(
                    () =>
                        {
                            if (!_effectScheduler.Schedule(this, this.RunScheduled, this.StopToken))
                            {
                                SignalEffectScheduler.Default.Schedule(this, this.RunScheduled, this.StopToken);
                            }
                        }
                    );
            }
            catch (Exception e)
            {
                Logger.LogError(e,"Failed to schedule: {ErrorMessage}", e.Message);
            }
        }
    }

    private bool DoRunSync()
    {
        var stackInfo = Signals.Options.Logging.CaptureStackInfo?.Invoke(this);
        
        try
        {
            return this.DoRun().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            try
            {
                var sigEx = new SignalException("Immediate effect run failed", ex);
                
                if (stackInfo is not null && Signals.Options.Logging.AugmentWithStackInfo is not null)
                {
                    Signals.Options.Logging.AugmentWithStackInfo(this, sigEx, stackInfo);
                }
                
                throw sigEx;
            }
            catch (Exception ex2)
            {
                Logger.LogError(ex2, "{ErrorMessage}", ex2.Message);
            }
        }
    
        return false;
    }
    
    private async Task<bool> DoRun()
    {
        bool shouldReschedule;
        
        var prevTracker = SignalTracker.ReplaceWith(null);
        if (prevTracker is not null)
        {
            Logger.LogWarning("Unexpected signal tracker on stack");
        }
        
        ConcurrentHashSet<SignalEffect>? dirtyEffects = null;

        try
        {
            await _runLock.WaitAsync(this.StopToken);
            try
            {
                var swTotal = new Stopwatch();
                var swRun = new Stopwatch();

                try
                {
                    var shouldMeasureTotal = _runTotalTimeLimitMs > 0;
                    if (shouldMeasureTotal)
                        swTotal.Start();

                    var runCount = 0;
                    bool shouldRerun;

                    do
                    {
                        this.MarkPristine();
                        
                        var tracker = this.StartTrack(true)
                            .Readonly(this.Options.PreventSignalChange)
                            .Recursive()
                            .CollectEffects()
                            .EnableChangeTracking();
                        try
                        {
                            var shouldMeasureRun = _runTimeLimitMs > 0;

                            if (shouldMeasureRun)
                                swRun.Start();

                            var effectResult = await this.InvokeRunnerFunc();

                            if (shouldMeasureRun)
                                swRun.Stop();

                            if (!tracker.Effects.IsEmpty)
                            {
                                foreach (var effect in tracker.Effects)
                                {
                                    if (effect == this)
                                        continue;

                                    dirtyEffects ??= [];
                                    dirtyEffects.Add(effect);
                                }
                            }
                            
                            if (effectResult.ShouldDestroy)
                            {
                                return false;
                            }

                            shouldRerun = false;

                            foreach (var trackedNode in tracker.Tracked)
                            {
                                if (trackedNode is TrackingSignalNode { IsDirty: true })
                                {
                                    shouldRerun = true;

                                    break;
                                }

                                if (trackedNode is IWritableSignal && tracker.Changed.Contains(trackedNode))
                                {
                                    shouldRerun = true;

                                    break;
                                }
                            }
                            
                            if (!shouldRerun)
                            {
                                this.MarkPristine();
                            }

                            if (shouldMeasureRun && swRun.ElapsedMilliseconds > _runTimeLimitMs)
                                break;
                        }
                        finally
                        {
                            swRun.Stop();
                            this.EndTrack(tracker);
                        }

                        if (++runCount > this.Options.RerunLimit)
                            break;

                        if (shouldMeasureTotal && swTotal.ElapsedMilliseconds > _runTotalTimeLimitMs)
                            break;

                        if (this.IsDirty && _rerunDelayMs > 0)
                        {
                            await Task.Delay(_rerunDelayMs, CancellationToken.None);
                        }

                    } while (this.IsDirty);

                    shouldReschedule = this.IsDirty || shouldRerun;

                }
                finally
                {
                    swTotal.Stop();
                    swRun.Stop();
                }
            }
            finally
            {
                _runLock.Release();
            }
        }
        finally
        {
            var lastTracker = SignalTracker.ReplaceWith(prevTracker);
            if (lastTracker is not null)
            {
                Logger.LogWarning("Leaked tracker!");
            }

            if (dirtyEffects is not null)
            {
                foreach (var effect in dirtyEffects)
                {
                    effect.ReScheduleDebounced();
                }
            }
        }

        return shouldReschedule;
    }
    
    private async Task RunScheduled()
    {
        lock (_scheduleLock)
        {
            if (_canSchedule)
            {
                Logger.LogWarning("Run was scheduled, but can still schedule?!");
            }    
        }

        var shouldReschedule = false;

        try
        {
            if (this.IsDisposed || this.StopToken.IsCancellationRequested)
                return;

            shouldReschedule = await this.DoRun();

            if (!this.HasTracking && this.Options.AutoStopWhenNoTrackedSignal)
            {
                this.StopAutoRun();

                return;
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Effect run failed: {ErrorMessage}", e.Message);
            
        }
        finally
        {
            lock (_scheduleLock)
            lock(_timerLock)
            {
                _canSchedule = _debounceTimer is null;
            }
        }

        if (shouldReschedule)
        {
            if (_rescheduleDelayMs > 0)
                await Task.Delay(_rescheduleDelayMs, CancellationToken.None);
            
            this.Schedule();

            return;
        }

        lock (_scheduleLock)
        {
            if (_canSchedule)
            {
                this.Idle();
            }
        }
    }
    
    private void CancelTimer()
    {
        lock (_timerLock)
        {
            _debounceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            
            _scheduleTimer.Stop();
            
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }
    }
    
    private bool StartDebounce()
    {
        // ReSharper disable once InconsistentlySynchronizedField
        if (_debounceTimer is not null)
            return false;
        
        lock (_timerLock)
        {
            if (_debounceTimer is null)
            {
                _scheduleTimer.Restart();
                
                _debounceTimer = new Timer(
                    TimerTick,
                    this,
                    TimeSpan.FromMilliseconds(_debounceMs),
                    Timeout.InfiniteTimeSpan
                    );

                return true;
            }
        }

        return false;
    }
    
    private bool ExtendDebounce()
    {
        // ReSharper disable once InconsistentlySynchronizedField
        if (_debounceTimer is null)
            return false;
        
        lock (_timerLock)
        {
            if (_debounceTimer is null)
                return false;
            
            var elapsed = _scheduleTimer.ElapsedMilliseconds;
            _debounceTimer.Change(TimeSpan.FromMilliseconds(elapsed + _debounceMs), Timeout.InfiniteTimeSpan);
        }

        return true;
    }

    private bool EnqueueAsSuspended()
    {
        return this.Group.TryQueueSuspended(this)
            || (SignalGroup.Current?.TryQueueSuspended(this) ?? false);
    }
    
    private static void TimerTick(object? state)
    {
        if (state is not SignalEffect effect)
            return;
        
        effect.CancelTimer();

        if (effect.IsDisposed)
            return;
        
        effect.Schedule(true);
    }
}