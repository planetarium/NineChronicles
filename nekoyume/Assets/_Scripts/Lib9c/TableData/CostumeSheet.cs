namespace Nekoyume.TableData
{
    public class CostumeSheet
    {
        public static string GetEyeOpenResourceByIndex(int eyeIndex)
        {
            return string.Format(GetEyeResourceFormatByIndex(eyeIndex), "open");
        }
        
        public static string GetEyeHalfResourceByIndex(int eyeIndex)
        {
            return string.Format(GetEyeResourceFormatByIndex(eyeIndex), "half");
        }
        
        private static string GetEyeResourceFormatByIndex(int eyeIndex)
        {
            switch (eyeIndex)
            {
                default:
                    return "eye_red_{0}";
                case 1:
                    return "eye_blue_{0}";
                case 2:
                    return "eye_green_{0}";
                case 3:
                    return "eye_violet_{0}";
                case 4:
                    return "eye_white_{0}";
                case 5:
                    return "eye_yellow_{0}";
            }
        } 
    }
}
