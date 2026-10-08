using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NurbsEditor.Geometry
{
    public class ControlPoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        public double Weight { get; set; }

        public ControlPoint(double x, double y, double weight = 1.0)
        {
            X = x;
            Y = y;
            Weight = weight;
        }
    }
}
