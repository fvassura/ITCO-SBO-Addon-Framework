
using ITCO.SboAddon.Framework.Helpers;
using SAPbobsCOM;
using System.Linq;
using System.Text.RegularExpressions;

namespace ITCO.SboAddon.Framework.Extensions
{
    public static class UserQueryExtensions
    {
        /// <summary>
        /// Get or create User Query
        /// </summary>
        /// <param name="company">Company Object</param>
        /// <param name="userQueryName">User Query Name</param>
        /// <param name="userQueryDefaultQuery">Query</param>
        /// <param name="formatToSqlParams">Replace [%0] to @p0</param>
        /// <returns></returns>
        public static string GetOrCreateUserQuery(this Company company, string userQueryName, string userQueryDefaultQuery, bool formatToSqlParams = false)
        {
            var userQuery = userQueryDefaultQuery;

            using (var userQueryObject = new SboRecordsetQuery<UserQueries>(
                string.Format("SELECT [IntrnalKey] FROM [OUQR] WHERE [QName] = '{0}'", userQueryName), BoObjectTypes.oUserQueries))
            {
                if (userQueryObject.Count == 0)
                {
                    userQueryObject.BusinessObject.QueryDescription = userQueryName;
                    userQueryObject.BusinessObject.Query = userQueryDefaultQuery;
                    userQueryObject.BusinessObject.QueryCategory = -1;
                    var response = userQueryObject.BusinessObject.Add();

                    ErrorHelper.HandleErrorWithException(response, string.Format("Could not create User Query '{0}'", userQueryName));
                }
                else
                {
                    userQuery = userQueryObject.Result.First().Query;
                }
            }

            if (formatToSqlParams)
                userQuery = Regex.Replace(userQuery, @"\[%([0-9])\]", "@p$1");

            return userQuery;
        }
    }
}
