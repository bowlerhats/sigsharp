using SigSharp;

var calculator = new InvoiceCalculator();

calculator.InvoiceLinePrices.Add(4);

Console.WriteLine($"Debug total: {calculator.InvoiceTotalDebug}");
Console.Out.Flush();

Console.WriteLine($"Debug total: {calculator.InvoiceTotalDebug}");
Console.Out.Flush();

/* Output:
Total updating...
Debug total: 10
Total updated: 10
Debug total: 10
*/

class InvoiceCalculator
{
    public readonly HashSetSignal<double> InvoiceLinePrices = new([1, 2, 3]);

    public double InvoiceTotal => this.Computed(() => InvoiceLinePrices.Sum());
    
    public double InvoiceTotalDebug => this.Computed(() =>
    {
        Console.WriteLine("Total updating...");
        
        return InvoiceTotal;
    });

    public InvoiceCalculator()
    {
        this.Effect(() =>
        {
            Console.WriteLine($"Total updated: {InvoiceTotal}");
            Console.Out.Flush();
        });
    }
}
