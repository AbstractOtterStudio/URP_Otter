#if GRIFFIN
namespace Pinwheel.Griffin
{
    /// <summary>
    /// Utility class contains product info
    /// </summary>
    public static class GVersionInfo
    {
        public const int Major = 3000;
        public const int Minor = 3;
        public const int Patch = 0;

        public static float Number
        {
            get
            {
                return Major * 1.0f + Minor * 1.0f / 100f;
            }
        }

        public static string Code
        {
            get
            {
                return string.Format("{0}.{1}.{2}", (Major/1000).ToString(), Minor.ToString(), Patch.ToString());
            }
        }

        public static string ProductName
        {
            get
            {
                return "Polaris - Low Poly Terrain Engine";
            }
        }

        public static string ProductNameAndVersion
        {
            get
            {
                return string.Format("{0} {1}", ProductName, Code);
            }
        }

        public static string ProductNameShort
        {
            get
            {
                return "Polaris";
            }
        }

        public static string ProductNameAndVersionShort
        {
            get
            {
                return string.Format("{0} {1}", ProductNameShort, Code);
            }
        }
    }
}
#endif
