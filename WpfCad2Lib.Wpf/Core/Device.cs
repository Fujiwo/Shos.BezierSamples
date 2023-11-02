using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace WpfCad2Lib.Wpf
{
    public class Device
    {
        Rect         modelArea         = new Rect(new Size(1.0, 1.0));
        Rect         viewArea          = new Rect(new Size(1.0, 1.0));
        Vector       offset            = new Vector();
        double       rate              = 1.0;
        Matrix       modelToViewMatrix = new Matrix();
        Matrix       viewToModelMatrix = new Matrix();

        public Rect ModelArea
        {
            get { return modelArea; }
            set
            {
                if (value != modelArea) {
                    modelArea = value;
                    SetModelToViewMatrix();
                }
            }
        }

        public Rect ViewArea
        {
            get { return viewArea; }
            set
            {
                if (value != viewArea) {
                    viewArea = value;
                    SetModelToViewMatrix();
                }
            }
        }

        public bool IsHome
        {
            get { return Rate == 1.0; }
        }

        public Matrix ModelToViewMatrix
        {
            get { return modelToViewMatrix; }
        }

        public Matrix ViewToModelMatrix
        {
            get { return viewToModelMatrix; }
        }

        double Rate
        {
            get { return rate; }
            set
            {
                var newRate = Math.Min(Math.Max(value, Common.ZoomMinimumRate), Common.ZoomMaximumRate);
                if (newRate != rate) {
                    rate = newRate;
                    SetModelToViewMatrix();
                }
            }
        }

        Vector Offset
        {
            get { return offset; }
            set
            {
                if (value != offset) {
                    offset = value;
                    SetModelToViewMatrix();
                }
            }
        }

        public void Home()
        {
            Offset = new Vector();
            Rate   = 1.0;
        }

        public void Zoom(Point zoomCenterModelPoint, double rate)
        {
            var oldZoomCenterViewPoint        = zoomCenterModelPoint * ModelToViewMatrix;
            Rate *= rate;
            var newZoomCenterViewPoint        = zoomCenterModelPoint * ModelToViewMatrix;
            var zoomCenterViewPointDifference = newZoomCenterViewPoint - oldZoomCenterViewPoint;
            modelToViewMatrix.Translate(-zoomCenterViewPointDifference.X, -zoomCenterViewPointDifference.Y);
            SetViewToModelMatrix();
        }

        public void Pan(Direction direction, double rate)
        {
            var viewAreaTopLeft     = ViewArea.TopLeft    ;
            var viewAreaBottomRight = ViewArea.BottomRight;
            var panDistance         = viewAreaTopLeft.Distance(viewAreaBottomRight) * rate;
            var panX                = 0.0;
            var panY                = 0.0;
            switch (direction) {
                case Direction.Left : panX =  panDistance; break;
                case Direction.Right: panX = -panDistance; break;
                case Direction.Up   : panY =  panDistance; break;
                case Direction.Down : panY = -panDistance; break;
            }
            modelToViewMatrix.Translate(panX, panY);
            SetViewToModelMatrix();
        }

        void SetModelToViewMatrix()
        {
            modelToViewMatrix = GetModelToViewMatrix();
            SetViewToModelMatrix();
        }

        void SetViewToModelMatrix()
        {
            viewToModelMatrix = modelToViewMatrix;
            viewToModelMatrix.Invert();
        }

        Matrix GetModelToViewMatrix()
        {
            var matrix = new Matrix(); 
            //var modelAreaCenter = modelArea.Center();
            //matrix.Translate(-modelAreaCenter.X - offset.X, -modelAreaCenter.Y - offset.Y);

            Debug.Assert(modelArea.Width != 0.0 && modelArea.Height != 0.0);
            var xRate           = modelArea.Width  == 0.0 ? viewArea.Width  : viewArea.Width  / modelArea.Width ;
            var yRate           = modelArea.Height == 0.0 ? viewArea.Height : viewArea.Height / modelArea.Height;
            var scaleRate       = Math.Min(xRate, yRate) * rate;
            matrix.Scale(scaleRate, scaleRate);

            //var viewAreaCenter  = viewArea.Center();
            //matrix.Translate(viewAreaCenter.X, viewAreaCenter.Y);

            return matrix;
        }
    }
}
