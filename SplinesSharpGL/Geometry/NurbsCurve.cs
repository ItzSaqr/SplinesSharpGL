using System;
using System.Collections.Generic;
using System.Drawing;

namespace NurbsEditor.Geometry
{
    public enum NurbsMode
    {
        Closed,
        Periodic
    }

    public class NurbsCurve
    {
        public List<ControlPoint> Points
        {
            get;
        } = new List<ControlPoint>();

        public int Degree
        {
            get;
            set;
        } = 3;

        public NurbsMode Mode
        {
            get;
            set;
        } = NurbsMode.Closed;

        public bool CanBuild
        {
            get
            {
                return Points.Count >= Degree + 1;
            }
        }

        public List<PointF> BuildCurve(int samples = 400)
        {
            List<PointF> result = new List<PointF>();

            if (!CanBuild) return result;

            if (Mode == NurbsMode.Closed) return BuildClamped(samples);

            return BuildPeriodic(samples);
        }

        // --------------------------------------------
        // Закрытый / закреплённый вариант
        // --------------------------------------------

        private List<PointF> BuildClamped(int samples)
        {
            List<PointF> result = new List<PointF>();

            int n = Points.Count - 1;
            int p = Degree;

            double[] knots = CreateClampedKnotVector(Points.Count, p);

            double uMin = knots[p];
            double uMax = knots[n + 1];

            for (int s = 0; s <= samples; s++)
            {
                double t = (double)s / samples;

                double u = uMin + (uMax - uMin) * t;

                // Для самой последней точки
                // чуть уменьшаем параметр.
                if (s == samples) u = uMax - 1e-10;

                PointF point = Evaluate(u, Points, p, knots);

                result.Add(point);
            }

            return result;
        }

        // --------------------------------------------
        // Периодический / замкнутый вариант
        // --------------------------------------------

        private List<PointF> BuildPeriodic(int samples)
        {
            List<ControlPoint> periodicPoints = new List<ControlPoint>();

            foreach (ControlPoint p in Points)
            {
                periodicPoints.Add(new ControlPoint(p.X, p.Y, p.Weight));
            }

            // Добавляем первые Degree точек
            // после конца массива.
            for (int i = 0; i < Degree; i++)
            {
                ControlPoint p = Points[i % Points.Count];

                periodicPoints.Add(new ControlPoint(p.X, p.Y, p.Weight));
            }

            int pDegree = Degree;

            double[] knots = CreateUniformKnotVector(periodicPoints.Count, pDegree);

            int n = periodicPoints.Count - 1;

            double uMin = knots[pDegree];
            double uMax = knots[n + 1];

            // Для полного замыкания берем диапазон,
            // соответствующий исходному числу точек.
            double periodEnd = uMin + Points.Count;

            if (periodEnd < uMax) uMax = periodEnd;

            List<PointF> result = new List<PointF>();

            for (int s = 0; s <= samples; s++)
            {
                double t = (double)s / samples;

                double u = uMin + (uMax - uMin) * t;

                if (s == samples) u = uMax - 1e-10;

                result.Add(Evaluate(u, periodicPoints, pDegree, knots));
            }

            // визуально замыкаем линию
            if (result.Count > 0) result.Add(result[0]);

            return result;
        }

        // --------------------------------------------
        // NURBS
        // --------------------------------------------

        private PointF Evaluate(
        double u, List<ControlPoint> points, int degree, double[] knots)
        {
            double numeratorX = 0.0;
            double numeratorY = 0.0;
            double denominator = 0.0;

            for (int i = 0; i < points.Count; i++)
            {
                double basis = Basis(i, degree, u, knots);

                double value = basis * points[i].Weight;

                numeratorX += value * points[i].X;

                numeratorY += value * points[i].Y;

                denominator += value;
            }

            if (Math.Abs(denominator) < 1e-12) return PointF.Empty;

            return new PointF((float)(numeratorX / denominator), (float)(numeratorY / denominator));
        }

        // Формула Кокса-де Бура
        private double Basis(
        int i, int degree, double u, double[] knots)
        {
            if (degree == 0)
            {
                if (knots[i] <= u && u < knots[i + 1]) return 1.0;

                return 0.0;
            }

            double left = 0.0;
            double right = 0.0;

            double leftDenominator = knots[i + degree] - knots[i];

            if (Math.Abs(leftDenominator) > 1e-12)
            {
                left = (u - knots[i]) / leftDenominator * Basis(i, degree - 1, u, knots);
            }

            double rightDenominator = knots[i + degree + 1] - knots[i + 1];

            if (Math.Abs(rightDenominator) > 1e-12)
            {
                right = (knots[i + degree + 1] - u) / rightDenominator * Basis(i + 1, degree - 1, u, knots);
            }

            return left + right;
        }

        // --------------------------------------------
        // Узловые векторы
        // --------------------------------------------

        private double[] CreateClampedKnotVector(
        int controlPointCount, int degree)
        {
            int n = controlPointCount - 1;

            int knotCount = n + degree + 2;

            double[] knots = new double[knotCount];

            int maxValue = n - degree + 1;

            for (int i = 0; i < knotCount; i++)
            {
                if (i <= degree)
                {
                    knots[i] = 0;
                }
                else if (i >= n + 1)
                {
                    knots[i] = maxValue;
                }
                else
                {
                    knots[i] = i - degree;
                }
            }

            return knots;
        }

        private double[] CreateUniformKnotVector(
        int controlPointCount, int degree)
        {
            int knotCount = controlPointCount + degree + 1;

            double[] knots = new double[knotCount];

            for (int i = 0; i < knotCount; i++)
                knots[i] = i;

            return knots;
        }
    }
}