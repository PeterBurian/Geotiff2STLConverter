using Newtonsoft.Json;
using OSGeo.GDAL;
using STLConverter;
using System;
using System.Collections.Generic;
using System.IO;

namespace Geotiff2STLConverter
{
    public class Converter
    {
        private string destination;
        private string path;
        private int stride;
        private double scale;
        private bool isBinary;

        private double YMin;
        private double YMax;
        private double XMin;
        private double XMax;

        private double lowerThreshold;
        private double upperThreshold;

        public delegate void ValidatedTetras(V3 a, V3 b, V3 c, V3 d);

        public Converter(string path)
        {
            stride = 1;
            scale = 1;
            isBinary = true;

            YMin = -1.0;
            YMax = -1.0;
            XMin = -1.0;
            XMax = -1.0;

            this.path = path;

            lowerThreshold = -1;
            upperThreshold = -1;

            Init();
        }

        public void Init()
        {
            GdalConfiguration.ConfigureGdal();
            Gdal.AllRegister();
            if (destination == null)
            {
                string outputFileName = Path.GetFileNameWithoutExtension(path);
                destination = Path.Combine(Path.GetDirectoryName(path), String.Format("{0}.stl", outputFileName));
            }
        }

        /// <summary>
        /// Set the lower threshold in meter
        /// Validate points higher than bottom plus threshold
        /// If the bottom elevation is 10m and the threshold is 15 you will create stl from points higher than 25m
        /// </summary>
        /// <param name="threshold"></param>
        public void SetLowerThresholdInMeter(double threshold)
        {
            this.lowerThreshold = threshold;
        }

        /// <summary>
        /// Set the upper threshold in meter
        /// Validate points higher than top minus threshold
        /// If the top elevation is 40m and the threshold is 15 you will create stl from points higher than 25m
        /// </summary>
        /// <param name="threshold"></param>
        public void SetUpperThresholdInMeter(double threshold)
        {
            this.upperThreshold = threshold;
        }

        public void ConvertToStl()
        {
            using (var outfile = new STLExporter(destination, isBinary))
            {
                ProcessData((a, b, c, d) =>
                {
                    outfile.WriteTriangle(a, b, c);
                    outfile.WriteTriangle(b, c, d);
                });
            }
            Console.WriteLine("Finished");
        }

        public string ConvertToJson()
        {
            List<List<V3>> points = new List<List<V3>>();

            ProcessData((a, b, c, d) =>
            {
                points.Add(new List<V3>() { a, b, c, d });
            });

            return JsonConvert.SerializeObject(points);

        }

        private void ProcessData(ValidatedTetras mthd)
        {
            var dataset = Gdal.Open(path, Access.GA_ReadOnly);

            //band numbers start at 1
            var heightdata = dataset.GetRasterBand(1);
            double[] minmax = new double[2];
            heightdata.ComputeRasterMinMax(minmax, 0);

            int YMinIndex;
            int YMaxIndex;
            (YMinIndex, YMaxIndex) = GetMinMaxIndex(YMin, YMax, heightdata.YSize);
            int XMinIndex;
            int XMaxIndex;
            (XMinIndex, XMaxIndex) = GetMinMaxIndex(XMin, XMax, heightdata.XSize);

            int YIndexSize = YMaxIndex - YMinIndex;
            int XIndexSize = XMaxIndex - XMinIndex;
            //our buffer indices go from 0 to xxx
            int bufferXIndexSize = XIndexSize / stride;
            int bufferYIndexSize = YIndexSize / stride;

            //create a buffer to hold data in memory.
            double[] databuffer = new double[bufferXIndexSize * bufferYIndexSize];

            heightdata.ReadRaster(XMinIndex, YMinIndex, XIndexSize, YIndexSize, databuffer, bufferXIndexSize, bufferYIndexSize, 0, 0);



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

                    if (IsValidByZ(minmax[0], pointA, pointB, pointC, pointD))
                    {
                        if ((upperThreshold != -1 && IsHigerThanThreshold(minmax[1] - upperThreshold, pointA, pointB, pointC, pointD))
                        || (lowerThreshold != -1 && IsHigerThanThreshold(minmax[0] + lowerThreshold, pointA, pointB, pointC, pointD))
                        || (lowerThreshold == -1 && upperThreshold == -1))
                        {
                            mthd?.Invoke(pointA, pointB, pointC, pointD);
                        }
                    }
                }
            }
        }

        private bool IsValidByZ(double minZ, params V3[] vectors)
        {
            if (vectors != null)
            {
                bool isValid = true;
                for (int i = 0; i < vectors.Length; i++)
                {
                    isValid &= vectors[i].Z > minZ;
                }
                return isValid;
            }
            return false;
        }

        private bool IsHigerThanThreshold(double threshold, params V3[] vectors)
        {
            if (vectors != null)
            {
                bool isValid = true;
                for (int i = 0; i < vectors.Length; i++)
                {
                    isValid &= vectors[i].Z > threshold;
                }
                return isValid;
            }
            return false;
        }

        private (int MinIndex, int MaxIndex) GetMinMaxIndex(double Min, double Max, int Size)
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
