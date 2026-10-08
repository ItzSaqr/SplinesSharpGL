using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NurbsEditor.Rendering
{
    public class Camera2D
    {
        public double X
        {
            get;
            set;
        } = 0.0;
        public double Y
        {
            get;
            set;
        } = 0.0;

        public double Zoom
        {
            get;
            set;
        } = 50.0;

        public double ScreenToWorldX(
        int screenX, int width)
        {
            return (screenX - width / 2.0) / Zoom + X;
        }

        public double ScreenToWorldY(
        int screenY, int height)
        {
            return (height / 2.0 - screenY) / Zoom + Y;
        }
    }
}