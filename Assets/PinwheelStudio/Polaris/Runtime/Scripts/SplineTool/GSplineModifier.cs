#if GRIFFIN
using UnityEngine;

namespace Pinwheel.Griffin.SplineTool
{
    [System.Serializable]
    public abstract class GSplineModifier : MonoBehaviour
    {
        [SerializeField]
        protected GSplineCreator splineCreator;
        public GSplineCreator SplineCreator
        {
            get
            {
                if (splineCreator == null)
                {
                    splineCreator = GetComponentInParent<GSplineCreator>();
                }
                return splineCreator;
            }
            set
            {
                splineCreator = value;
            }
        }

        [SerializeField]
        protected int curviness = 5;
        public int Curviness
        {
            get
            {
                return curviness;
            }
            set
            {
                curviness = Mathf.Max(2, value);
            }
        }

        [SerializeField]
        protected float width = 5;
        public float Width
        {
            get
            {
                return width;
            }
            set
            {
                width = Mathf.Max(0, value);
            }
        }

        [SerializeField]
        protected float falloffWidth = 5;
        public float FalloffWidth
        {
            get
            {
                return falloffWidth;
            }
            set
            {
                falloffWidth = Mathf.Max(0, value);
            }
        }

        public abstract void Apply();
    }
}
#endif
