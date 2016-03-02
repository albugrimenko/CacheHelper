using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace CacheHelper.Helpers {
    #region ----- StaticHelper -----
    public class StaticHelper {
        internal static readonly ILog log = LogManager.GetLogger("CacheHelper");
        internal const int _SQLCommandTimeout_ = 5;  // in seconds

        #region --- Config Attributes ---
        internal static string ConString(string stringName) {
            if (System.Configuration.ConfigurationManager.ConnectionStrings[stringName] != null)
                return System.Configuration.ConfigurationManager.ConnectionStrings[stringName].ConnectionString;
            else
                return string.Empty;
        } // ConString

        public static int GetConfigAttrAsInt(string attrName, int defaultValue = 0) {
            int res = defaultValue;
            string r = GetConfigAttr(attrName);
            if (!string.IsNullOrEmpty(r) && int.TryParse(r, out res))
                return res;
            else
                return defaultValue;
        }
        public static bool GetConfigAttrAsBool(string attrName, bool defaultValue = false) {
            bool res = defaultValue;
            string r = GetConfigAttr(attrName);
            if (!string.IsNullOrEmpty(r)) {
                if (r.ToLower() == "true")
                    return true;
                else
                    return false;
            }
            else
                return res;
        }
        internal static string GetConfigAttr(string attrName) {
            if (System.Configuration.ConfigurationManager.AppSettings[attrName] != null)
                return System.Configuration.ConfigurationManager.AppSettings[attrName];
            else
                return string.Empty;
        } // GetConfigAttr
        #endregion --- Config Attributes ---
    }
    #endregion ----- StaticHelper -----
}
