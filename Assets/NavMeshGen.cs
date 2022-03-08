using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NavMeshGen
{
    public static class NavMeshGen
    {
        public const int MaxArea = 60;
        public const int UnwalkArea = 0;
        public const float MinCellSize = 0.01f;
        public const int MaxVertsPerPolygon = 3;
        public const float MaxWalkableAngle = 70.0f;
        public const int MinWalkableHeight = 3;

        public static void FindSizeOfCellGrid(Vector3 boundsMin, Vector3 boundsMax, float xzCellSize, out int width, out int depth)
        {
            width = (int)((boundsMax.x - boundsMin.x) / xzCellSize);
            depth = (int)((boundsMax.z - boundsMin.z) / xzCellSize);
        }
    }
}
