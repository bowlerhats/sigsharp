using SigSharp;

var engine = new CarEngine();
using var signalGroup = new SignalGroup(); // every effect has to be in a group
var effect = signalGroup.Effect(() =>
{
    Console.WriteLine($"Torque: {engine.CurrentTorque}");
    Console.Out.Flush();
});

engine.AddGas();
effect.WaitIdle(); //wait for effect to handle changes
// effect outputs:  Torque: 1.3

engine.AddGas();
engine.AddGas();
effect.WaitIdle(); //wait for the effect of last change
// effect outputs: Torque: 3.9000000000000004

engine.RemoveGas();
engine.RemoveGas();
effect.WaitIdle();

// effect outputs:  Torque: 1.3

return 0;

// -----------------------
public class CarEngine
{
    // using a static delegate for better performance (recommended)
    public double CurrentTorque => this.Computed(static engine => engine.CurrentRev * 1.3);

    private readonly Signal<int> _rev = new(0);
    private readonly Signal<bool> _running = new();
    
    // using a lambda instance for convenience
    private int CurrentRev => this.Computed(() => _rev.Value);

    public CarEngine()
    {
        this.Effect(WatchPedal);
    }
    
    public void AddGas() => _rev.Value++;
    
    public void RemoveGas() => _rev.Value--;
    
    private void WatchPedal() 
    {
        if (CurrentRev > 0)
        {
            StartEngine();
        } else {
            StopEngine();
        }
    }
    
    private void StartEngine() => _running.Set(true);
    
    private void StopEngine() 
    {
        _running.Set(false);
        
        // using untracked value to explicitly prevent infinite effect runs
        // here it won't be the case, because if _rev is already 0, nothing happens
        if (_rev.Untracked < 0)
            _rev.Set(0);
    }
}