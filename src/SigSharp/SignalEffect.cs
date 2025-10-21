using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SigSharp.Nodes;
using SigSharp.Utils;
using SigSharp.Utils.Perf;

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
    private readonly AsyncManualResetEvent _idle = new(true);
    
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
        : base(group, false, false, name)
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
        
        Perf.Increment("signal.effect.count");
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        // ReSharper disable once MethodSupportsCancellation
        await _runLock.WaitAsync();
        
        try
        {
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
            
            _canSchedule = false;

            _effectFunctor = default;

            _idle.Dispose();
        }
        finally
        {
            var rLock = _runLock;
            _runLock = null!;
            
            rLock.Dispose();
        }
        
        Perf.Decrement("signal.effect.count");

        await base.DisposeAsyncCore();
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
            _canSchedule = false;
            
            Signals.Detached(() =>
            {
                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(100, this.StopToken);
                    await this.DisposeAsync();
                }, this.StopToken);
            });
        }

        return ValueTask.FromResult(result);
    }
    
    protected override void OnDirty()
    {
        if (!this.IsAutoRunning)
            return;
        
        if (this.IsDisposing || this.StopToken.IsCancellationRequested)
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

        if (this.EnqueueAsSuspended())
            return;
        
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

    public bool WaitIdle(TimeSpan? timeout = null, CancellationToken? stopToken = null)
    {
        if (this.IsDisposing)
            return false;
        
        if (_idle.IsSet)
            return false;

        stopToken = stopToken.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(this.StopToken, stopToken.Value).Token
            : this.StopToken;

        if (timeout.HasValue)
        {
            _idle.Wait(timeout, stopToken.Value);
        }
        else
        {
            _idle.Wait(stopToken: stopToken.Value);
        }

        return true;
    }

    public async Task<bool> WaitIdleAsync(TimeSpan? timeout = null, CancellationToken? stopToken = null)
    {
        if (this.IsDisposing)
            return false;
        
        if (_idle.IsSet)
            return false;

        if (SignalTracker.Current?.IsInEffect(this) ?? false)
            return false;
        
        stopToken = stopToken.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(this.StopToken, stopToken.Value).Token
            : this.StopToken;
        
        if (timeout.HasValue)
        {
            await _idle.WaitAsync(timeout, stopToken.Value);
        }
        else
        {
            while (!await _idle.WaitAsync(TimeSpan.FromSeconds(1), stopToken.Value))
            {
                
            }
        }

        return true;
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
    
    internal void Schedule(bool forced)
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
                    
                    this.NotIdle();
                    
                    return;
                }
            }
            else
            {
                lock (_timerLock)
                {
                    if (_debounceTimer is not null)
                    {
                        this.ExtendDebounce();

                        return;
                    }
                }
            }

            var currentTracker = SignalTracker.Current;
            
            if (currentTracker?.AcceptEffects ?? false)
            {
                currentTracker.PostEffect(this);

                return;
            }

            _canSchedule = false;
            
            this.NotIdle();
            
            try
            {
                Signals.Detached(
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

                lock (_scheduleLock)
                {
                    _canSchedule = true;

                    this.Idle();
                }
            }
        }
    }
    
    public void RunImmediate()
    {
        this.CheckDisposed();

        this.DoRun().GetAwaiter().GetResult();
    }
    
    private async Task<bool> DoRun()
    {
        using var activity = this.StartActivity();

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
                if (this.IsDisposing || this.Group.IsDisposing)
                {
                    return false;
                }
                
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
                        using var runTime = Perf.MeasureTime(this, "signal.effect.run");
                        
                        this.MarkPristine();

                        var tracker = this.StartTrack(true)
                            .Readonly(this.Options.PreventSignalChange)
                            .Recursive()
                            .CollectEffects()
                            .EnableChangeTracking();

                        try
                        {
                            activity.Event("Effect run start");

                            var shouldMeasureRun = _runTimeLimitMs > 0;

                            if (shouldMeasureRun)
                                swRun.Start();

                            var effectResult = await this.InvokeRunnerFunc();

                            if (shouldMeasureRun)
                                swRun.Stop();

                            activity.Event("Effect run end");

                            if (tracker.Effects.HasAny)
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
                                return false;

                            if (effectResult.ShouldReschedule)
                                return true;

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
                        catch (SignalVersionChangedException)
                        {
                            Perf.MonoIncrement(this, "signal.effect.run.errors.versionchanged");
                            activity.Event("VersionChanged raised");
                            
                            tracker.DisableTracking();
                            this.MarkDirty();
                            
                            dirtyEffects?.Clear();

                            return !this.IsDisposing;
                        }
                        catch (SignalDeadlockedException)
                        {
                            Perf.MonoIncrement(this, "signal.effect.run.errors.deadlocks");
                            
                            activity.Event("Deadlock raised");

                            return !this.IsDisposing;
                        }
                        catch (SignalDisposedException)
                        {
                            Perf.MonoIncrement(this, "signal.effect.run.errors.disposed");
                            
                            activity.Event("Signal disposed error");

                            return !this.IsDisposing;
                        }
                        catch (SignalPreemptedException preemptException)
                        {
                            Perf.MonoIncrement(this, "signal.effect.run.errors.preemptions");
                            
                            activity.Event("Signal preempted");

                            tracker.DisableTracking();
                            this.MarkDirty();
                            
                            dirtyEffects?.Clear();
                            
                            return !this.IsDisposing && !preemptException.IsRescheduled;
                        }
                        catch (Exception ex)
                        {
                            Perf.MonoIncrement(this, "signal.effect.run.errors.unexpected");
                            
                            activity.Event($"Unexpected signal effect run error: {ex.Message}");
                            
                            if (this.IsDisposing)
                                return false;
                            
                            throw;
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
                            await Task.Delay(_rerunDelayMs, this.StopToken);
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
                foreach (var effect in dirtyEffects.OrderBy(d => d.NodeId))
                {
                    if (!effect.EnqueueAsSuspended())
                    {
                        effect.ReScheduleDebounced();
                    }
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
            if (this.IsDisposing || this.StopToken.IsCancellationRequested)
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
                await Task.Delay(_rescheduleDelayMs, this.StopToken);
            
            this.Schedule();

            return;
        }

        lock (_scheduleLock)
        {
            _canSchedule = _debounceTimer is null;
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
            || (SignalGroup.Current?.TryQueueSuspended(this) ?? false)
            || (SignalGroup.GlobalSuspender?.TryQueueSuspended(this) ?? false);
    }
    
    private static void TimerTick(object? state)
    {
        if (state is not SignalEffect effect)
            return;
        
        effect.CancelTimer();

        if (effect.IsDisposing)
            return;
        
        effect.Schedule(true);
    }
}