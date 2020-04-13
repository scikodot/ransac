using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RANSAC
{
    class Point
    {
        public double x, y, z;

        public Point()
        {

        }

        public Point(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Point(double[] point)
        {
            x = point[0];
            y = point[1];
            z = point[2];
        }
    }

    class Vector : Point
    {
        public double magnitude
        {
            get
            {
                return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));
            }
        }

        public Vector normalized
        {
            get
            {
                return new Vector(x / magnitude, y / magnitude, z / magnitude);
            }
        }

        public Vector(double x, double y, double z) : base(x, y, z)
        {

        }

        public Vector(double[] point) : base(point)
        {

        }

        public static Vector operator +(Vector v1, Vector v2)
        {
            return new Vector(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        public static Vector operator *(Vector v, double num)
        {
            return new Vector(v.x * num, v.y * num, v.z * num);
        }
    }

    class Program
    {
        static double p;
        static int N;
        static Point[] points;

        static double[] genPlane;
        static double inliersPercentage;
        static double[] lowLine = new double[2], highLine = new double[2];

        static void Main(string[] args)
        {
            Console.WriteLine("Choose input data:\n1 - load from file\n2 - generate sample data");
            ConsoleKeyInfo mode = Console.ReadKey(true);
            switch (mode.Key)
            {
                case ConsoleKey.D1:
                    Console.WriteLine("\nEnter filepath:");
                    string path = Console.ReadLine();
                    string[] data = File.ReadAllLines(path);

                    p = double.Parse(data[0].Contains('.') ? data[0].Replace('.', ',') : data[0]);
                    N = int.Parse(data[1]);
                    points = new Point[N];

                    for (int i = 2; i < data.Length; i++)
                    {
                        string[] line = (data[i].Contains('.') ? data[i].Replace('.', ',') : data[i]).Split('\t');
                        points[i - 2] = new Point(double.Parse(line[0]), double.Parse(line[1]), double.Parse(line[2]));
                    }

                    Console.WriteLine("\n###INPUT###");
                    break;
                case ConsoleKey.D2:
                    GenerateData();

                    Console.WriteLine("\n###INPUT###");
                    Console.WriteLine("Generated plane: {0:F6} {1:F6} {2:F6} {3:F6}", genPlane[0], genPlane[1], genPlane[2], genPlane[3]);
                    Console.WriteLine("Inliers' percentage: {0:F6}%", inliersPercentage * 100);
                    Console.WriteLine("Would you like to save generated data to a file?\nEnter - YES, anything else - NO\n");
                    if (Console.ReadKey(true).Key == ConsoleKey.Enter)
                    {
                        string[] contents = new string[N + 2];
                        contents[0] = p.ToString("F6");
                        contents[1] = N.ToString();
                        for (int i = 0; i < N; i++)
                            contents[i + 2] = points[i].x.ToString("F6") + "\t" + points[i].y.ToString("F6") + "\t" + points[i].z.ToString("F6");
                        File.WriteAllLines(@"D:\UNIVER\SNAR\HW7\Dima\Test1.txt", contents);
                    }
                    break;
                default:
                    return;
            }

            Console.WriteLine("Threshold (p): {0:F6}\nPoints' amount (N): {1}", p, N);
            Console.WriteLine("Points (first 10):");
            for (int i = 0; i < (points.Length < 10 ? points.Length : 10); i++)
                Console.WriteLine("{0:F6}\t{1:F6}\t{2:F6}", points[i].x, points[i].y, points[i].z);
            Console.WriteLine();

            //RANSAC algorithm implementation
            double w = 0.5f;  //probability of choosing an inlier from the set of data
            double alpha = 0.999f;  //probability of finding best-fitting plane

            int bestSupport = 0;
            double[] bestPlane = new double[4];
            double bestStd = double.PositiveInfinity;
            int trials = (int)Math.Round(Math.Log(1 - alpha) / Math.Log(1 - Math.Pow(w, 3)), MidpointRounding.AwayFromZero);

            Random rng = new Random();
            int j = 0;
            while (j++ <= trials)
            {
                //retrieving 3 random points from the list
                Tuple<Point, Point, Point> planePoints = GetPoints(rng);

                //constructing a new plane upon retrieved points
                double[] plane = Points2Plane(planePoints);

                //getting all valid points, as well as their distances to the plane
                Point[] qualifiedPoints = Qualify(plane, points, p, out double[] qualifiedDistances);

                //calculating standard deviation of qualified points
                double std = StandardDeviation(qualifiedDistances);

                //checking whether the current plane fits data better or not
                if (qualifiedPoints.Length > bestSupport || (qualifiedPoints.Length == bestSupport && std < bestStd))
                {
                    //if yes, consider current plane as best-fitting
                    bestSupport = qualifiedPoints.Length;
                    bestPlane = plane;
                    bestStd = std;
                }
            }

            //outputting results
            Console.WriteLine("###OUTPUT###");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Best plane: {0:F6} {1:F6} {2:F6} {3:F6}", bestPlane[0], bestPlane[1], bestPlane[2], bestPlane[3]);
            Console.ResetColor();
            Console.WriteLine("Best support (i.e. matched points): {0}", bestSupport);
            Console.WriteLine("Best standard deviation: {0}\n", bestStd);

            if (mode.Key == ConsoleKey.D2)
                Console.WriteLine("Lost points: {0}\nAccuracy: {1:F6}\n", (int)(N * inliersPercentage) - bestSupport, bestSupport / (N * inliersPercentage));

            Console.Write("Press any key to exit...");
            Console.ReadKey(true);
        }

        static Tuple<Point, Point, Point> GetPoints(Random rng)
        {
            List<int> indices = new List<int>();

            while (indices.Count < 3)
            {
                int index = rng.Next(0, points.Length);
                if (!indices.Contains(index))
                    indices.Add(index);
            }

            Point point1 = points[indices[0]],
                point2 = points[indices[1]],
                point3 = points[indices[2]];

            return Tuple.Create(point1, point2, point3);
        }

        static double[] Points2Plane(Tuple<Point, Point, Point> points)
        {
            Point point1 = points.Item1;
            Point point2 = points.Item2;
            Point point3 = points.Item3;

            Vector v1 = new Vector(point2.x - point1.x, point2.y - point1.y, point2.z - point1.z);
            Vector v2 = new Vector(point3.x - point1.x, point3.y - point1.y, point3.z - point1.z);
            Vector n = new Vector(v1.y * v2.z - v2.y * v1.z, -v1.x * v2.z + v2.x * v1.z, v1.x * v2.y - v2.x * v1.y);

            double[] plane = new double[4];
            plane[0] = n.x;
            plane[1] = n.y;
            plane[2] = n.z;
            plane[3] = -n.x * point1.x - n.y * point1.y - n.z * point1.z;

            for (int i = 0; i < 4; i++)
            {
                plane[i] *= -(plane[3] != 0 ? Math.Sign(plane[3]) : 1) / n.magnitude;
            }

            return plane;
        }

        static Point[] Qualify(double[] plane, Point[] points, double threshold, out double[] distances)
        {
            List<Point> validPoints = new List<Point>();
            List<double> validDistances = new List<double>();

            for (int i = 0; i < points.Length; i++)
            {
                double distance = Math.Abs(plane[0] * points[i].x + plane[1] * points[i].y + plane[2] * points[i].z + plane[3]) /
                    Math.Sqrt(Math.Pow(plane[0], 2) + Math.Pow(plane[1], 2) + Math.Pow(plane[2], 2));

                if (distance <= threshold)
                {
                    validPoints.Add(points[i]);
                    validDistances.Add(distance);
                }
            }

            distances = validDistances.ToArray();
            return validPoints.ToArray();
        }

        static double StandardDeviation(double[] set)
        {
            double mean = 0, variance = 0;

            for (int i = 0; i < set.Length; i++)
                mean += set[i];
            mean /= set.Length;

            for (int i = 0; i < set.Length; i++)
                variance += Math.Pow(set[i] - mean, 2);
            variance /= set.Length - 1;

            return Math.Sqrt(variance);
        }

        static void GenerateData()
        {
            Random rng = new Random();

            p = rng.NextDouble();
            if (p > 0.5)
                p -= 0.5;
            if (p < 0.01)
                p += 0.01;

            N = rng.Next(25000, 25001);

            Point p1 = new Point
            {
                x = rng.NextDouble() * 200 - 100,
                y = rng.NextDouble() * 200 - 100,
                z = rng.NextDouble() * 200 - 100
            };
            Point p2 = new Point
            {
                x = rng.NextDouble() * 200 - 100,
                y = rng.NextDouble() * 200 - 100,
                z = rng.NextDouble() * 200 - 100
            };
            Point p3 = new Point
            {
                x = rng.NextDouble() * 200 - 100,
                y = rng.NextDouble() * 200 - 100,
                z = rng.NextDouble() * 200 - 100
            };

            genPlane = Points2Plane(Tuple.Create(p1, p2, p3));

            Vector n = new Vector(genPlane[0], genPlane[1], genPlane[2]);

            Tuple<double, double, double, double, double, double> bounds = GetBounds(genPlane);
            double x_l = bounds.Item1, x_h = bounds.Item2;
            double y_l = bounds.Item3, y_h = bounds.Item4;
            double z_l = bounds.Item5, z_h = bounds.Item6;

            inliersPercentage = 1 - rng.NextDouble();
            if (inliersPercentage < 0.5)
                inliersPercentage += 0.5;

            List<Point> genPoints = new List<Point>();
            for (int i = 0; i < N; i++)
            {
                double distance;
                if (i <= inliersPercentage * N)
                    distance = rng.NextDouble() * p;
                else
                    distance = (1 - rng.NextDouble()) * 2 * p + p;

                double x = 0, y = 0, z = 0;
                if (genPlane[0] != 0 && genPlane[1] != 0 && genPlane[2] != 0)
                {
                    x = rng.NextDouble() * (x_h - x_l) + x_l;

                    y_l = lowLine[0] * x + lowLine[1];
                    if (y_l < -100) y_l = -100;

                    y_h = highLine[0] * x + highLine[1];
                    if (y_h > 100) y_h = 100;

                    y = rng.NextDouble() * (y_h - y_l) + y_l;

                    z = -(genPlane[0] * x + genPlane[1] * y + genPlane[3]) / genPlane[2];
                }
                else if (genPlane[0] == 0 && genPlane[1] == 0)
                {
                    x = rng.NextDouble() * 200 - 100;
                    y = rng.NextDouble() * 200 - 100;
                    z = -genPlane[3] / genPlane[2];
                }
                else if (genPlane[1] == 0 && genPlane[2] == 0)
                {
                    y = rng.NextDouble() * 200 - 100;
                    z = rng.NextDouble() * 200 - 100;
                    x = -genPlane[3] / genPlane[0];
                }
                else if (genPlane[2] == 0 && genPlane[0] == 0)
                {
                    x = rng.NextDouble() * 200 - 100;
                    z = rng.NextDouble() * 200 - 100;
                    y = -genPlane[3] / genPlane[1];
                }
                else if (genPlane[0] == 0)
                {
                    x = rng.NextDouble() * 200 - 100;
                    y = rng.NextDouble() * (y_h - y_l) + y_l;
                    z = -(genPlane[1] * y + genPlane[3]) / genPlane[2];
                }
                else if (genPlane[1] == 0)
                {
                    y = rng.NextDouble() * 200 - 100;
                    x = rng.NextDouble() * (x_h - x_l) + x_l;
                    z = -(genPlane[0] * x + genPlane[3]) / genPlane[2];
                }
                else if (genPlane[2] == 0)
                {
                    z = rng.NextDouble() * 200 - 100;
                    x = rng.NextDouble() * (x_h - x_l) + x_l;
                    y = -(genPlane[0] * x + genPlane[3]) / genPlane[1];
                }

                Vector point = new Vector(x, y, z);
                point += n.normalized * distance * (rng.Next(0, 2) == 0 ? 1 : -1);

                genPoints.Add(point);
            }

            points = genPoints.ToArray();
        }

        static public Tuple<double, double, double, double, double, double> GetBounds(double[] plane)
        {
            double[] p = new double[3];
            List<Point> bounds = new List<Point>();
            double[] line1 = new double[2], line2 = new double[2];
            if (plane[0] != 0 && plane[1] != 0 && plane[2] != 0)
            {
                List<double> x = new List<double>();
                for (int i = 0; i < 2; i++)
                {
                    p[2] = (i & 1) == 1 ? -100 : 100;

                    for (int j = 0; j < 4; j++)
                    {
                        int k = (j & 2) == 2 ? 1 : 0;
                        p[k] = (j & 1) == 1 ? -100 : 100;
                        p[k ^ 1] = -(plane[k] * p[k] + plane[2] * p[2] + plane[3]) / plane[k ^ 1];

                        if (k == 1)
                            x.Add(p[k ^ 1]);
                    }

                    if (i == 0)
                    {
                        line1[0] = -plane[0] / plane[1];
                        line1[1] = -(plane[2] * p[2] + plane[3]) / plane[1];
                    }
                    else
                    {
                        line2[0] = -plane[0] / plane[1];
                        line2[1] = -(plane[2] * p[2] + plane[3]) / plane[1];
                    }
                }

                if (line1[1] < line2[1])
                {
                    lowLine = line1;
                    highLine = line2;
                }
                else
                {
                    lowLine = line2;
                    highLine = line1;
                }

                double x_min = x.Min(), x_max = x.Max();
                if (x_min < -100) x_min = -100;
                if (x_max > 100) x_max = 100;

                return Tuple.Create(x_min, x_max, 0.0, 0.0, 0.0, 0.0);
            }
            else if (plane[0] == 0 || plane[1] == 0 || plane[2] == 0)
            {
                int index = 0;
                if (plane[0] == 0)
                    index = 0;
                else if (plane[1] == 0)
                    index = 1;
                else if (plane[2] == 0)
                    index = 2;

                int sum = (index + 1) % 3 + (index + 2) % 3;
                for (int j = 0; j < 4; j++)
                {
                    int k = (j & 2) == 2 ? (index + 1) % 3 : (index + 2) % 3;
                    p[k] = (j & 1) == 1 ? -100 : 100;
                    p[k ^ sum] = -(plane[k] * p[k] + plane[3]) / plane[k ^ sum];

                    if (Math.Abs(p[k ^ sum]) <= 100)
                        bounds.Add(new Point(p));
                }
            }

            double x_low_bound = bounds.Min(t => t.x), x_high_bound = bounds.Max(t => t.x);
            double y_low_bound = bounds.Min(t => t.y), y_high_bound = bounds.Max(t => t.y);
            double z_low_bound = bounds.Min(t => t.z), z_high_bound = bounds.Max(t => t.z);

            return Tuple.Create(x_low_bound, x_high_bound, y_low_bound, y_high_bound, z_low_bound, z_high_bound);
        }
    }
}
