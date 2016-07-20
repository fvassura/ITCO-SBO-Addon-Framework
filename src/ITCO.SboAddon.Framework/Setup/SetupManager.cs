using ITCO.SboAddon.Framework.Services;
using System;
using System.Linq;
using System.Reflection;

namespace ITCO.SboAddon.Framework.Setup
{
    /// <summary>
    /// Setup Manager
    /// Uses setup version for faster startup
    /// </summary>
    public static class SetupManager
    {
        /// <summary>
        /// Find ISetup classes and Run Setup
        /// </summary>
        /// <param name="assembly">Assembly to search in</param>
        public static void FindAndRunSetups(Assembly assembly)
        {
            var setups = (from t in assembly.GetTypes()
                          where !t.IsInterface && t.GetInterfaces().Contains(typeof(ISetup))
                          select t).ToArray();

            if (!setups.Any())
                return;

            SettingService.Init();

            foreach (var setupInstance in setups.Select(setup => Activator.CreateInstance(setup) as ISetup))
            {
                RunSetup(setupInstance);
            }
        }

        /// <summary>
        /// Run Setup, check version
        /// </summary>
        /// <typeparam name="TSetup"></typeparam>
        /// <param name="setupInstance"></param>
        public static void RunSetup<TSetup>(TSetup setupInstance) where TSetup : ISetup
        {
            var setup = setupInstance.GetType();
            var key = string.Format("setup.lastversion.{0}", setup.Name.Replace("Setup", string.Empty));
            var lastVersionInstalled = SettingService.GetSettingByKey(key, 0);

            if (lastVersionInstalled < setupInstance.Version)
            {
                try
                {
                    SboApp.Logger.Info(string.Format("Running setup for {0}, current version is {1}, new version is {2})", setup.Name, lastVersionInstalled, setupInstance.Version));

                    setupInstance.Run();
                    SettingService.SaveSetting(key, setupInstance.Version);
                }
                catch (Exception ex)
                {
                    SboApp.Logger.Error(string.Format("Setup error in {0}: {1}", setup.Name, ex.Message), ex);
                    throw;
                }
            }

            SboApp.Logger.Info(string.Format("Setup for {0} is up-to-date! (v.{1})", setup.Name, setupInstance.Version));
        }
    }
}
