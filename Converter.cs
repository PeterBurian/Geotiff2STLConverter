using OSGeo.GDAL;
using STLConverter;
using System;

namespace Geotiff2STLConverter
{
    public class Converter
    {
        private string destination;
        private string path;
        private bool renderFaces;
        private int stride;
        private double scale;
        private bool isBinary;

        private double YMin;
        private double YMax;
        private double XMin;
        private double XMax;

        public Converter(string path)
        {
            renderFaces = true;
            stride = 1;
            scale = 0.1;
            isBinary = true;

            YMin = -1.0;
            YMax = -1.0;
            XMin = -1.0;
            XMax = -1.0;

            this.path = path;

            Init();
        }

        public void Init()
        {
            GdalConfiguration.ConfigureGdal();
            Gdal.AllRegister();
            if (destination == null)
            {
                destination = System.IO.Path.GetDirectoryName(path) + "/Output.stl";
            }
        }

        public void Convert()
        {
            var dataset = Gdal.Open(path, Access.GA_ReadOnly);
            Driver drv = dataset.GetDriver();

            //Console.WriteLine("number of things is:");
            //Console.WriteLine(dataset.RasterCount);
            //band numbers start at 1
            var heightdata = dataset.GetRasterBand(1);
            double[] minmax = new double[2];
            heightdata.ComputeRasterMinMax(minmax, 0);

            //decide what max and min index should be
            int YMinIndex;
            int YMaxIndex;
            (YMinIndex, YMaxIndex) = GetMinMax(YMin, YMax, heightdata.YSize);
            int XMinIndex;
            int XMaxIndex;
            (XMinIndex, XMaxIndex) = GetMinMax(XMin, XMax, heightdata.XSize);

            int YIndexSize = YMaxIndex - YMinIndex;
            int XIndexSize = XMaxIndex - XMinIndex;
            //our buffer indices go from 0 to xxx
            int bufferXIndexSize = XIndexSize / stride;
            int bufferYIndexSize = YIndexSize / stride;

            //create a buffer to hold data in memory.
            double[] databuffer = new double[bufferXIndexSize * bufferYIndexSize];

            heightdata.ReadRaster(XMinIndex, YMinIndex, XIndexSize, YIndexSize, databuffer, bufferXIndexSize, bufferYIndexSize, 0, 0);
            //misc stuff
            heightdata.GetNoDataValue(out double nodataval, out int hasnodataval);
            //Console.WriteLine($"has nodata val: {hasnodataval}, is {nodataval}");
            var thing = heightdata.GetRasterColorInterpretation();
            var thing2 = Gdal.GetDataTypeSize(heightdata.DataType);

            using (var outfile = new STLExporter(destination, isBinary))
            {
                if (renderFaces)
                {
                    for (int bufferYIndex = 0; bufferYIndex < bufferYIndexSize - 1; ++bufferYIndex)
                    {
                        for (int bufferXIndex = 0; bufferXIndex < bufferXIndexSize - 1; ++bufferXIndex)
                        {
                            int index = bufferYIndex * bufferXIndexSize + bufferXIndex;
                            int xValue = bufferXIndex * stride + XMinIndex;
                            int yValue = bufferYIndex * stride + YMinIndex;
                            double zvalue = databuffer[index];

                            V3 pointA = new V3((float)(xValue * scale), (float)(yValue * scale), (float)(zvalue * scale));
                            V3 pointB = new V3((float)(xValue * scale), (float)((yValue + stride) * scale), (float)(databuffer[index + bufferXIndexSize] * scale));
                            V3 pointC = new V3((float)((xValue + stride) * scale), (float)(yValue * scale), (float)(databuffer[index + 1] * scale));
                            V3 pointD = new V3((float)((xValue + stride) * scale), (float)((yValue + stride) * scale), (float)(databuffer[index + 1 + bufferXIndexSize] * scale));
                            outfile.WriteTriangle(pointA, pointB, pointC);
                            outfile.WriteTriangle(pointB, pointC, pointD);
                        }
                    }
                }
                else
                {
                    for (int bufferYIndex = 0; bufferYIndex < bufferYIndexSize; ++bufferYIndex)
                    {
                        for (int bufferXIndex = 0; bufferXIndex < bufferXIndexSize; ++bufferXIndex)
                        {
                            int index = bufferYIndex * bufferXIndexSize + bufferXIndex;
                            int xValue = bufferXIndex * stride + XMinIndex;
                            int yValue = bufferYIndex * stride + YMinIndex;
                            double zvalue = databuffer[index];
                            outfile.WritePoint((float)(xValue * scale), (float)(yValue * scale), (float)(zvalue * scale));
                        }
                    }
                }
            }
            Console.WriteLine("Finished!");
        }

        private (int MinIndex, int MaxIndex) GetMinMax(double Min, double Max, int Size)
        {
            int MaxIndex;
            int MinIndex;
            //YMax
            if (Max >= 1.0)
            {
                MaxIndex = (int)Max;
            }
            else if (Max >= 0.0)
            {
                MaxIndex = (int)(Max * Size);
            }
            else
            {
                MaxIndex = Size;
            }

            if (Min >= 1.0)
            {
                MinIndex = (int)Min;
            }
            else if (Min >= 0.0)
            {
                MinIndex = (int)(Min * Size);
            }
            else
            {
                MinIndex = 0;
            }

            return (MinIndex, MaxIndex);
        }
    }
}
