using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public static class Math
    {
        private static Random rnd = new Random();

        /// <summary>
        /// Returns a random double between min and max
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double RandomDouble(double min, double max)
        {
            return min + (max - min) * rnd.NextDouble();
        }





    }
}
