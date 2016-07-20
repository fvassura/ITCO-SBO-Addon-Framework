using ITCO.SboAddon.Framework.Dialogs;
using ITCO.SboAddon.Framework.Dialogs.Inputs;
using ITCO.SboAddon.Framework.Helpers;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace ITCO.SboAddon.Framework.Services
{
    /// <summary>
    /// Generic Setting Service
    /// </summary>
    public static class SettingService
    {
        private const string UdtSettings = "ITCO_FW_Settings";
        private const string UdfSettingValue = "ITCO_FW_SValue";
        private static bool _setupOk;

        /// <summary>
        /// Initialize Setting Service
        /// </summary>
        /// <returns></returns>
        public static bool Init()
        {
            if (_setupOk)
                return true;

            try
            {
                UserDefinedHelper.CreateTable(UdtSettings, "Settings")
                    .CreateUDF(UdfSettingValue, "Value");

                _setupOk = true;

                SboApp.Logger.Info("SettingService Init [OK]");
            }
            catch (Exception e)
            {
                SboApp.Logger.Error(string.Format("SettingService Init [NOT OK] {0}", e.Message), e);
                _setupOk = false;
                throw;
            }

            return _setupOk;
        }
        /// <summary>
        /// Create Empty Setting if not exists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue">Default Value</param>
        public static void InitSetting<T>(string key, string name, T defaultValue = default(T))
        {
            if (GetSettingAsString(key) == null)
                SaveSetting(key, defaultValue, name: name);
        }

        /// <summary>
        /// Get Setting for Current User
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <param name="askIfNotFound"></param>
        /// <returns></returns>
        public static T GetCurrentUserSettingByKey<T>(string key, T defaultValue = default(T), bool askIfNotFound = false)
        {
            var userCode = SboApp.Company.UserName;
            return GetSettingByKey(key, defaultValue, userCode, askIfNotFound);
        }

        /// <summary>
        /// Save Settings for Current User
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SaveCurrentUserSetting<T>(string key, T value)
        {
            var userCode = SboApp.Company.UserName;
            SaveSetting(key, value, userCode);
        }

        private static string GetSettingAsString(string key, string userCode = null)
        {
            var sqlKey = key.Trim().ToLowerInvariant();

            if (userCode != null)
                sqlKey = string.Format("{0}[{1}]", sqlKey, userCode);

            var sql = string.Format("SELECT [U_{0}], [Name] FROM [@{1}] WHERE [Code] = '{2}'", UdfSettingValue, UdtSettings, sqlKey);
            using (var query = new SboRecordsetQuery(sql))
            {
                if (query.Count == 0)
                    return null;

                var result = query.Result.First();
                return result.Item(0).Value as string;
            }
        }

        /// <summary>
        /// Get Setting
        /// </summary>
        /// <typeparam name="T">Return Type</typeparam>
        /// <param name="key">Setting Key</param>
        /// <param name="defaultValue">Default Value</param>
        /// <param name="userCode">User Code</param>
        /// <param name="askIfNotFound">Ask for value if not found</param>
        /// <returns>Setting Value</returns>
        public static T GetSettingByKey<T>(string key, T defaultValue = default(T), string userCode = null, bool askIfNotFound = false)
        {
            Init();

            if (string.IsNullOrEmpty(key))
                return defaultValue;

            var returnValue = defaultValue;
            var notFound = true;

            try
            {
                var value = GetSettingAsString(key, userCode);

                if (value != null)
                    notFound = false;

                if (value == "")
                    value = null;

                returnValue = To<T>(value);
            }
            catch (Exception e)
            {
                SboApp.Logger.Error(string.Format("SettingService Error: {0} (Key={1}, UserCode={2})", e.Message, key, userCode), e);
                return returnValue;
            }

            if (!notFound || !askIfNotFound)
                return returnValue;

            var name = GetSettingTitle(key);
            var inputTitle = string.Format("Insert setting {0}", name);
            if (userCode != null)
                inputTitle += string.Format(" for {0}", userCode);

            var input = new TextDialogInput("setting", name, required: true) as IDialogInput;

            if (typeof(T) == typeof(bool))
                input = new CheckboxDialogInput("setting", name);

            if (typeof(T) == typeof(DateTime))
                input = new DateDialogInput("setting", name, required: true);

            if (typeof(T) == typeof(int))
                input = new IntegerDialogInput("setting", name, required: true);

            if (typeof(T) == typeof(decimal))
                input = new DecimalDialogInput("setting", name, required: true);

            var result = InputHelper.GetInputs(inputTitle)
                .AddInput(input).Result();

            var newSetting = result.First().Value;
            SaveSetting(key, newSetting, userCode);

            returnValue = To<T>(newSetting);

            return returnValue;
        }

        private static string GetSettingTitle(string key)
        {
            var sqlKey = key.Trim().ToLowerInvariant();
            var sql = string.Format("SELECT [Name] FROM [@{0}] WHERE [Code] = '{1}'", UdtSettings, sqlKey);

            using (var query = new SboRecordsetQuery(sql))
            {
                if (query.Count == 0)
                    return key;

                var result = query.Result.First();
                var name = result.Item(0).Value as string;

                return string.IsNullOrEmpty(name) ? key : name;
            }
        }

        /// <summary>
        /// Save Setting
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="userCode"></param>
        /// <param name="name"></param>
        public static void SaveSetting<T>(string key, T value = default(T), string userCode = null, string name = null)
        {
            Init();

            var sqlKey = key.Trim().ToLowerInvariant();

            if (userCode != null)
                sqlKey = string.Format("{0}[{1}]", key, userCode);

            var sql = string.Format("SELECT [U_{0}], [Name] FROM [@{1}] WHERE [Code] = '{2}'", UdfSettingValue, UdtSettings, sqlKey);

            bool exists;
            using (var query = new SboRecordsetQuery(sql))
            {
                exists = query.Count == 1;
            }

            var sqlValue = string.Format(CultureInfo.InvariantCulture, "'{0}'", value);
            if (value == null)
                sqlValue = "NULL";

            if (exists)
            {
                sql = string.Format("UPDATE [@{0}] SET [U_{1}] = {2} WHERE [Code] = '{3}'", UdtSettings, UdfSettingValue, sqlValue, sqlKey);
            }
            else
            {
                if (name == null)
                    name = sqlKey;

                sql = string.Format("INSERT INTO [@{0}] ([Code], [Name], [U_{1}]) VALUES ('{2}', '{3}', {4})", UdtSettings, UdfSettingValue, sqlKey, name, sqlValue);
            }

            SboRecordset.NonQuery(sql);
        }

        private static object To(object value, Type destinationType)
        {
            return To(value, destinationType, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a value to a destination type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="destinationType">The type to convert the value to.</param>
        /// <param name="culture">Culture</param>
        /// <returns>The converted value.</returns>
        private static object To(object value, Type destinationType, CultureInfo culture)
        {
            if (value == null)
                return destinationType.IsValueType ? Activator.CreateInstance(destinationType) : null;

            var sourceType = value.GetType();

            var destinationConverter = GetTypeConverter(destinationType);
            var sourceConverter = GetTypeConverter(sourceType);
            if (destinationConverter != null && destinationConverter.CanConvertFrom(value.GetType()))
                return destinationConverter.ConvertFrom(null, culture, value);
            if (sourceConverter != null && sourceConverter.CanConvertTo(destinationType))
                return sourceConverter.ConvertTo(null, culture, value, destinationType);
            if (destinationType.IsEnum && value is int)
                return Enum.ToObject(destinationType, (int)value);
            if (!destinationType.IsInstanceOfType(value))
                return Convert.ChangeType(value, destinationType, culture);
            return value;
        }

        /// <summary>
        /// Converts a value to a destination type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <returns>The converted value.</returns>
        private static T To<T>(object value)
        {
            //return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            return (T)To(value, typeof(T));
        }

        private static TypeConverter GetTypeConverter(Type type)
        {
            return TypeDescriptor.GetConverter(type);
        }
    }
}
