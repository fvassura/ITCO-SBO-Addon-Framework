using ITCO.SboAddon.Framework.Helpers;
using SAPbobsCOM;
using System;
using System.Linq;
using System.Threading;

namespace ITCO.SboAddon.Framework.Extensions
{
    public static class TransactionExtensions
    {
        /// <summary>
        /// Wait for open transaction to complete
        /// Useful when using BeginTransaction
        /// </summary>
        /// <param name="company"></param>
        /// <param name="sleep"></param>
        /// <param name="tryCount"></param>
        public static void WaitForOpenTransactions(this Company company, int sleep = 500, int tryCount = 10)
        {
            for (var i = 0; i < tryCount; i++)
            {
                using (var query = new SboRecordsetQuery(
                    string.Format("SELECT hostname, loginame FROM sys.sysprocesses WHERE open_tran=1 AND dbid=DB_ID('{0}')", company.CompanyDB)))
                {
                    if (query.Count == 0)
                        return;

                    var openTransaction = query.Result.First();

                    SboApp.Logger.Trace(string.Format("Open Transaction by {0}, waiting {1} ms...", openTransaction.Item("hostname").Value, sleep));
                }
                Thread.Sleep(sleep);
            }

            throw new Exception(string.Format("Waiting for open transactions to long! ({0} ms)", sleep * tryCount));
        }
    }
}
