using System;

namespace SigSharp;

public record SignalEffectOptions
{
    public static SignalEffectOptions Defaults { get; } = new();

    /// <summary>
    /// Effect will run immediately on creation. <br/>
    /// Defaults to true.
    /// </summary>
    public bool AutoSchedule { get; init; } = true;
    
    /// <summary>
    /// Throw when during effect a signal attempts to change.<br/>
    /// Defaults to false.
    /// </summary>
    public bool PreventSignalChange { get; init; }
    
    /// <summary>
    /// The effect will stop running when any run results in no signals touched.<br/>
    /// Defaults to true.
    /// </summary>
    public bool AutoStopWhenNoTrackedSignal { get; init; } = true;
    
    /// <summary>
    /// How much time the effect should wait before scheduling a run.<br/>
    /// Any change event during this time will reset the timer but not invoke a schedule.<br/>
    /// Use TimeSpan.Zero to disable debouncing.
    /// </summary>
    /// <remarks>
    /// The behaviour is similar to RX debounceTime().
    /// </remarks>
    public TimeSpan DebounceChangedTime { get; init; } = TimeSpan.FromMilliseconds(30);

    /// <summary>
    /// Maximum times the effect can rerun immediately without rescheduling.
    /// </summary>
    /// <remarks>
    /// Rerun occurs when the effect is still dirty after a run.
    /// </remarks>
    public int RerunLimit { get; init; } = 10;

    /// <summary>
    /// Prevent rerun when a run takes at least this much time. <br/>
    /// The effect will reschedule instead. Use TimeSpan.Zero to disable.
    /// </summary>
    /// <remarks>
    /// Rerun occurs when the effect is still dirty after a run.<br />
    /// Should be used in conjunction with either RerunDelay or RescheduleDelay to
    /// prevent heavy effects to choke the system 
    /// </remarks>
    public TimeSpan RerunTimeLimit { get; init; } = TimeSpan.Zero;
    
    /// <summary>
    ///  Stops reruns when this much time elapsed since the start of reruns.
    /// </summary>
    /// <remarks>
    /// Rerun occurs when the effect is still dirty after a run. <br/>
    /// ex.: When 5 reruns occured and 1 seconds elapsed since the start of reruns, then the effect will reschedule instead.<br />
    /// Use it in conjunction with RescheduleDelay to prevent heavy effects to choke the system. 
    /// </remarks>
    public TimeSpan RerunYieldTimeLimit { get; init; } = TimeSpan.Zero;
    
    /// <summary>
    /// Time to wait between reruns
    /// </summary>
    public TimeSpan RerunDelay { get; init; } = TimeSpan.Zero;
    
    /// <summary>
    /// Time to wait before rescheduling a dirty run.
    /// </summary>
    /// <remarks>
    /// NB: 
    /// </remarks>
    public TimeSpan RescheduleDelay { get; init; } = TimeSpan.Zero;
    
    public ISignalEffectScheduler? EffectScheduler { get; init; }
}