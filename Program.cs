using System;

namespace Geotiff2STLConverter
{
    public class Program
    {
        static void Main(string[] args)
        {
            string stlPath = @"f:\Development\SunMeData\Data\Geotiff\DSM_50cm.asc";
            Converter converter = new Converter(stlPath);
            converter.Convert();

            Console.ReadKey();
        }
    }
}
