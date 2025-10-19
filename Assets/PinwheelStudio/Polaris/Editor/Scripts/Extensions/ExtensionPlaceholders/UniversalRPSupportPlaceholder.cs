#if GRIFFIN
#if GRIFFIN && UNITY_EDITOR && !GRIFFIN_URP
using Pinwheel.Griffin.Wizard;
using UnityEngine;

namespace Pinwheel.Griffin.GriffinExtension
{
    public static class UniversalRPSupportPlaceholder
    {
        public static string GetExtensionName()
        {
            return "Universal Render Pipeline Support";
        }

        public static string GetPublisherName()
        {
            return "Pinwheel Studio";
        }

        public static string GetDescription()
        {
            return "Adding support for URP.\n" +
                "Requires Unity 2021.3 or above.";
        }

        public static string GetVersion()
        {
            return GVersionInfo.Code;
        }

        public static void OpenSupportLink()
        {
            GEditorCommon.OpenEmailEditor(
                GCommon.SUPPORT_EMAIL,
                "[Polaris] URP Support",
                "YOUR_MESSAGE_HERE");
        }

        public static void Button_Install()
        {
            GUrpPackageImporter.Import();
        }
    }
}
#endif
#endif
