using System;

namespace Geotiff2STLConverter
{
    public class Program
    {
        static void Main(string[] args)
        {
            string stlPath = args[0];
            Converter converter = new Converter(stlPath);
            converter.SetUpperThresholdInMeter(15);
            converter.ConvertToStl();

            Console.ReadKey();
        }
    }
}