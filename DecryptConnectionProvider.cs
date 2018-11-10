using CrypTool;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyHibernateUtil
{
    public class DecryptConnectionProvider : NHibernate.Connection.ConnectionProvider
    {
        private static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof(DecryptConnectionProvider));

        /// <summary>
        /// Closes and Disposes of the <see cref="IDbConnection"/>.
        /// </summary>
        /// <param name="conn">The <see cref="IDbConnection"/> to clean up.</param>
        public override void CloseConnection(IDbConnection conn)
        {
            base.CloseConnection(conn);
            conn.Dispose();
        }

        /// <summary>
        /// Gets a new open <see cref="IDbConnection"/> through 
        /// the <see cref="NHibernate.Driver.IDriver"/>.
        /// </summary>
        /// <returns>
        /// An Open <see cref="IDbConnection"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// If there is any problem creating or opening the <see cref="IDbConnection"/>.
        /// </exception>
        public override IDbConnection GetConnection()
        {
            log.Debug("Obtaining IDbConnection from Driver");
            IDbConnection conn = Driver.CreateConnection();
            try
            {
                string[] words = ConnectionString.Split(';');
                string newConnectionStr = "";
                foreach (string token in words)
                {
                    if (token.ToUpper().StartsWith("PASSWORD=") && token.Length >　11)
                    {
                        newConnectionStr += "Password=" + Crypto.Decrypt(token.Substring(10, token.Length - 11)) + ";";
                    }
                    else
                        newConnectionStr += token + ";";
                }
                conn.ConnectionString = newConnectionStr;
                conn.Open();
            }
            catch (Exception)
            {
                conn.Dispose();
                throw;
            }

            return conn;
        }

    }
}
