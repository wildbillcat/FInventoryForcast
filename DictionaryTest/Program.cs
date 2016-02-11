using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictionaryTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //10,
            double[] xdata = new double[] { 20, 30 };
            //15,
            double[] ydata = new double[] { 20, 25 };

            Tuple<double, double> p = Fit.Line(xdata, ydata);
            double a = p.Item1; // == 10; intercept
            double b = p.Item2; // == 0.5; slope
            System.Console.WriteLine(string.Concat("Intercept: ", a));
            System.Console.WriteLine(string.Concat("Slope: ", b));
            System.Console.WriteLine();
            Dictionary<double, double> vals = new Dictionary<double, double>();
            vals.Add(10, 15);
            vals.Add(14, 13);
            vals.Add(30, 25);
            vals.Add(20, 20);
            Dictionary<double, double> newvals = vals.OrderByDescending(o => o.Key).Take(2).ToDictionary(pair => pair.Key, pair => pair.Value);

            Tuple<double, double> n = Fit.Line(newvals.Keys.ToArray(), newvals.Values.ToArray());
            double c = n.Item1; // == 10; intercept
            double d = n.Item2; // == 0.5; slope
            System.Console.WriteLine(string.Concat("Intercept2: ", c));
            System.Console.WriteLine(string.Concat("Slope2: ", d));
            System.Console.ReadLine();
        }
    }
}
