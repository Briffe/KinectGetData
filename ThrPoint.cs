using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectGetData
{
    public struct ThrPoint
    {
        public double X;
        public double Y;
        public double Z;
        /// <summary>
        /// 三维结构体的赋值
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        public ThrPoint(double X, double Y, double Z)
        {
            if ( X == null || Y == null || Z == null) throw new ArgumentException();
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }
    }
}
