using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectGetData
{
    public struct ThrPoint
    {
        public float X;
        public float Y;
        public float Z;
        /// <summary>
        /// 三维结构体的赋值
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        public ThrPoint(float X, float Y, float Z)
        {
            if (X == null || Y == null || Z == null) throw new ArgumentException();
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }
    }
}
