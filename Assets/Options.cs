using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OptionsFile
{
    public class Options
    {
        float boundsHeight = 10;
        float xZCellSize = 0.25f; 
        float yCellSize = 0.5f;
        int maxCheckDistValue = 10;
        int initialRegionSize = 2000;
        int minRegionDistFromBorder = 3;

        float stepHeight = 1.0f;

        float maxNullRegionContour = 0.5f;

        #region Getters/Setters
        public float BoundsHeight
        {
            get { return boundsHeight; }
            set { boundsHeight = value; }
        }

        public float XZCellSize
        {
            get { return xZCellSize; }
            set { xZCellSize = value; }
        }
        public float YCellSize
        {
            get { return yCellSize; }
            set { yCellSize = value; }
        }
        public int MaxCheckDistValue
        {
            get { return maxCheckDistValue; }
            set { maxCheckDistValue = value; }
        }
        public int InitialRegionSize
        {
            get { return initialRegionSize; }
            set { initialRegionSize = value; }
        }
        public int MinRegionDistFromBorder
        {
            get { return minRegionDistFromBorder; }
            set { minRegionDistFromBorder = value; }
        }

        public float StepHeight
        {
            get { return stepHeight; }
            set { stepHeight = value; }
        }

        public float MaxNullRegionContour
        {
            get { return maxNullRegionContour; }
            set { maxNullRegionContour = value; }
        }
        #endregion
    }
}


