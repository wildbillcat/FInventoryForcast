using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using System.IO;
using System.Configuration;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;

namespace CalcService
{
    public class SKU
    {
        public int Id { get; set; }
        public List<double> MonthlyTotals { get; set; }
        public List<DateTime> Months { get; set; }
    }

    public class SKUPrediction
    {
        public int Id { get; set; }
        public int SeasonallyPredictedTotalRounded { get; set; }
        public double SeasonallyPredictedTotal { get; set; }
        public double PredictedTotal { get; set; }
        public DateTime PredictionDate { get; set; }
        public string SkuClass { get; set; }
        public double Slope { get; set; }
        public double Intercept { get; set; }
        public double GoodnessOfFitRSquare { get; set; }
        public double StandardDeviation { get; set; }
        public double ZScore { get; set; }
    }

    class Program
    {
        public static double JanuarySeasonality = double.Parse(ConfigurationSettings.AppSettings.Get("JanuarySeasonality"));
        public static double FebruarySeasonality = double.Parse(ConfigurationSettings.AppSettings.Get("FebruarySeasonality"));
        public static double MarchSeasonality = double.Parse(ConfigurationSettings.AppSettings.Get("MarchSeasonality"));
        public static double AprilSeasonality = double.Parse(ConfigurationSettings.AppSettings.Get("AprilSeasonality"));
        public static double MaySeasonality = double.Parse(ConfigurationSettings.AppSettings.Get("MaySeasonality"));
        public static double JuneSeasonality = double.Parse(ConfigurationSettings.AppSettings.Get("JuneSeasonality"));
        public static double JulySeasonality = double.Parse(ConfigurationSettings.AppSettings.Get("JulySeasonality"));
        public static double AugustSeasonality = double.Parse(ConfigurationSettings.AppSettings.Get("AugustSeasonality"));
        public static double SeptemberSeasonality = double.Parse(ConfigurationSettings.AppSettings.Get("SeptemberSeasonality"));
        public static double OctoberSeasonality = double.Parse(ConfigurationSettings.AppSettings.Get("OctoberSeasonality"));
        public static double NovemberSeasonality = double.Parse(ConfigurationSettings.AppSettings.Get("NovemberSeasonality"));
        public static double DecemberSeasonality = double.Parse(ConfigurationSettings.AppSettings.Get("DecemberSeasonality"));

        public static void CalculateMonthOverMonthSeasonality()
        {
            FebruarySeasonality = FebruarySeasonality * JanuarySeasonality;
            MarchSeasonality = MarchSeasonality * FebruarySeasonality;
            AprilSeasonality = AprilSeasonality * MarchSeasonality;
            MaySeasonality = MaySeasonality * AprilSeasonality;
            JuneSeasonality = MaySeasonality * JuneSeasonality;
            JulySeasonality = JulySeasonality * JuneSeasonality;
            AugustSeasonality = AugustSeasonality * JulySeasonality;
            SeptemberSeasonality = SeptemberSeasonality * AugustSeasonality;
            OctoberSeasonality = OctoberSeasonality * SeptemberSeasonality;
            NovemberSeasonality = NovemberSeasonality * OctoberSeasonality;
            DecemberSeasonality = DecemberSeasonality * NovemberSeasonality;
        }

        static void Main(string[] args)
        {
            if (bool.Parse(ConfigurationSettings.AppSettings.Get("MonthOverMonthSeasonality"))) {
                CalculateMonthOverMonthSeasonality();
            }
            Console.WriteLine("Importing Last 12 months of available data:");
            Dictionary<int, SKU> SKUs = new Dictionary<int, SKU>();
            //Take top 12 csv files in folder, ordered from most recent to oldest. //NumberMonths
            List<DateTime> importedDates = new List<DateTime>();
            int NumberMonths = int.Parse(ConfigurationSettings.AppSettings.Get("NumberMonths"));
            foreach (FileInfo file in new DirectoryInfo(ConfigurationSettings.AppSettings.Get("InputFiles")).GetFiles("*.csv").OrderByDescending(p => p.Name).Take(NumberMonths).ToArray())
            {
                Console.WriteLine(file.Name);
                int i = 0;
                DateTime ImportDate = Convert.ToDateTime(file.Name.Substring(0, file.Name.Length - 4));
                importedDates.Add(ImportDate);
                Console.WriteLine("Start Read");
                using (CsvReader csv = new CsvReader(System.IO.File.OpenText(file.FullName)))
                {
                    while (csv.Read())
                    {
                        try
                        {
                            int SKUId = csv.GetField<int>(0);
                            double SkuTotal = csv.GetField<double>(2);
                            if (!SKUs.ContainsKey(SKUId))
                            {
                                SKUs.Add(SKUId, new SKU() { Id = SKUId, MonthlyTotals = new List<double>(), Months = new List<DateTime>() });
                            }
                            SKUs[SKUId].Months.Add(ImportDate);
                            SKUs[SKUId].MonthlyTotals.Add(SkuTotal);
                            i++;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
                            Console.WriteLine(csv.GetField(0));
                            Console.WriteLine(csv.GetField(1));
                            Console.WriteLine(csv.GetField(2));
                        }                        
                    }
                }
            }
            ///////////////////////////////////////////////////////////////////////
            //Import Complete
            ///////////////////////////////////////////////////////////////////////
            //Overall output: sku id, class, projected number, and the slope
            List<SKUPrediction> Forcast = new List<SKUPrediction>();
            DateTime MostRecentDate = importedDates.Max();
            DateTime ForcastDate = MostRecentDate.AddMonths(1);
            Console.Write("Most Recent Imported Date: ");
            Console.WriteLine(MostRecentDate);
            Console.WriteLine("Starting Prediction Calculations");
            int j = 0;
            int percent = 0;
            int mod = (int)(SKUs.Values.Count() / 100);
            foreach (SKU S in SKUs.Values) {
                if(S.MonthlyTotals.Count > 1)
                {
                    Forcast.Add(GenerateSkuPreduction(S, ForcastDate));
                }
                else
                {
                    Console.Write("Skipping SKU : ");
                    Console.Write(S.Id);
                    Console.WriteLine(" Appears to have less than 2 months of sales data");
                }
                
                j++;
                if(j % mod == 0)
                {
                    Console.Write("Percent Complete: ");
                    Console.WriteLine(percent);
                    percent++;
                }
            }
            ///////////////////////////////////////////////////////////////////////
            //Calc Complete, write out data
            ///////////////////////////////////////////////////////////////////////
            Console.WriteLine("Writing Calculations to File");
            using (CsvWriter csvOut = new CsvWriter(System.IO.File.CreateText(ConfigurationSettings.AppSettings.Get("OutputFile"))))
            {
                csvOut.WriteRecords(Forcast);
            }

            Console.WriteLine("Complete!");
            Console.WriteLine("Press Any Key to close application.");
            Console.ReadLine();
        }

        public static SKUPrediction GenerateSkuPreduction(SKU sku, DateTime PredictionDate)
        {
            int Id = sku.Id;
            double[] YValues = sku.MonthlyTotals.ToArray();
            double[] NormalizedYs = RemoveSeasonality(sku.Months.Select(o => o.Month).ToArray(), YValues);
            double[] ScalarDates = sku.Months.Select(o => o.ToOADate()).ToArray();
            Tuple<double, double> p = Fit.Line(ScalarDates, NormalizedYs);
            double Slope = p.Item2;
            double Intercept = p.Item1;
            double PredictedTotal = Slope*PredictionDate.ToOADate() + Intercept;
            double SeasonallyPredictedTotal = AddSeasonality(PredictionDate.Month, PredictedTotal);
            int SeasonallyPredictedTotalRounded;
            if(Slope > 0)
            {
                SeasonallyPredictedTotalRounded = (int)Math.Ceiling(SeasonallyPredictedTotal);
            }
            else
            {
                SeasonallyPredictedTotalRounded = (int)Math.Floor(SeasonallyPredictedTotal);
            }
            
            string SkuClass = GetSkuClass(sku.MonthlyTotals.ToArray());
            
            double GoodnessOfFitVar = GoodnessOfFit.RSquared(ScalarDates.Select(x => Intercept + Slope * x), NormalizedYs); // == 1.0

            double StdDev = Statistics.PopulationStandardDeviation(YValues);

            double Zscore = (SeasonallyPredictedTotalRounded - YValues.Average()) / StdDev;

            return new SKUPrediction()
            {
                Id = Id,
                GoodnessOfFitRSquare = GoodnessOfFitVar,
                Intercept = Intercept,
                PredictedTotal = PredictedTotal,
                PredictionDate = PredictionDate,
                SeasonallyPredictedTotal = SeasonallyPredictedTotal,
                SeasonallyPredictedTotalRounded = SeasonallyPredictedTotalRounded,
                SkuClass = SkuClass,
                Slope = Slope,
                StandardDeviation = StdDev,
                ZScore = Zscore
                
            };
        }

        public static string GetSkuClass(double[] TotalSales)
        {
            int TotalMonthsGreaterThanZeroPast12 = 0;
            int TotalMonthsGreaterThanZeroPast6 = 0;
            for (int i = 0; i < TotalSales.Length; i++)
            {
                if (TotalSales[i] > 0){
                    TotalMonthsGreaterThanZeroPast12++;
                    if (i < 6)
                    {
                        TotalMonthsGreaterThanZeroPast6++;
                    }
                }                
            }

            string SkuClass;
            switch (TotalMonthsGreaterThanZeroPast6)
            {    
                case 5:
                case 4:
                    SkuClass = "B";
                    break;
                case 3:
                    SkuClass = "C";
                    break;
                default:
                    if(TotalMonthsGreaterThanZeroPast6 > 5)
                    {
                        SkuClass = "A";
                        break;
                    }
                    switch (TotalMonthsGreaterThanZeroPast12)
                    {
                        case 9:
                            SkuClass = "B";
                            break;
                        case 8:
                        case 7:
                            SkuClass = "C";
                            break;
                        case 6:
                            SkuClass = "D6";
                            break;
                        case 5:
                            SkuClass = "D5";
                            break;
                        case 4:
                            SkuClass = "D4";
                            break;
                        case 3:
                            SkuClass = "D3";
                            break;
                        case 2:
                            SkuClass = "D2";
                            break;
                        case 1:
                            SkuClass = "D1";
                            break;
                        default:
                            if (TotalMonthsGreaterThanZeroPast12 > 9)
                            {
                                SkuClass = "A";
                                break;
                            }
                            SkuClass = "E";
                            break;
                    }
                    break;
            }
            return SkuClass;
        }

        public static double[] RemoveSeasonality(int[] x, double[] y)
        {
            List<double> SeasonalityRemoved = new List<double>();
            for(int i = 0; i < x.Length; i++) 
            {
                SeasonalityRemoved.Add(RemoveSeasonality(x[i], y[i]));
            }
            return SeasonalityRemoved.ToArray();
        }

        public static double RemoveSeasonality(int x, double y)
        {
            switch (x)
            {
                case 1:
                    return y / JanuarySeasonality;
                case 2:
                    return y / FebruarySeasonality;
                case 3:
                    return y / MarchSeasonality;
                case 4:
                    return y / AprilSeasonality;
                case 5:
                    return y / MaySeasonality;
                case 6:
                    return y / JuneSeasonality;
                case 7:
                    return y / JulySeasonality;
                case 8:
                    return y / AugustSeasonality;
                case 9:
                    return y / SeptemberSeasonality;
                case 10:
                    return y / OctoberSeasonality;
                case 11:
                    return y / NovemberSeasonality;
                case 12:
                    return y / DecemberSeasonality;
                default:
                    return y;
            }
        }

        public static double AddSeasonality(int x, double y)
        {
            switch (x)
            {
                case 1:
                    return y * JanuarySeasonality;
                case 2:
                    return y * FebruarySeasonality;
                case 3:
                    return y * MarchSeasonality;
                case 4:
                    return y * AprilSeasonality;
                case 5:
                    return y * MaySeasonality;
                case 6:
                    return y * JuneSeasonality;
                case 7:
                    return y * JulySeasonality;
                case 8:
                    return y * AugustSeasonality;
                case 9:
                    return y * SeptemberSeasonality;
                case 10:
                    return y * OctoberSeasonality;
                case 11:
                    return y * NovemberSeasonality;
                case 12:
                    return y * DecemberSeasonality;
                default:
                    return y;
            }
        }
    }
}
