using CrypTool;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

#if !NET_4_0
using System.Threading;
using System.Threading.Tasks;
#endif

namespace MyHibernateUtil
{
    public class DecryptConnectionProvider : NHibernate.Connection.ConnectionProvider
    {
#if NET_4_0
        private static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof(DecryptConnectionProvider));
#else
        private static readonly INHibernateLogger log = NHibernateLogger.For(typeof(DecryptConnectionProvider));
#endif

        /// <summary>
        /// Closes and Disposes of the <see cref="IDbConnection"/>.
        /// </summary>
        /// <param name="conn">The <see cref="IDbConnection"/> to clean up.</param>
#if NET_4_0
        public override void CloseConnection(IDbConnection conn)
#else
        public override void CloseConnection(DbConnection conn)
#endif
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
#if NET_4_0
        public override IDbConnection GetConnection()
#else
        public override DbConnection GetConnection()
#endif
        {
#if NET_4_0
            log.Debug("Obtaining IDbConnection from Driver");
            IDbConnection conn = Driver.CreateConnection();
#else
            log.Debug("Obtaining DbConnection from Driver");
            DbConnection conn = Driver.CreateConnection();
#endif
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

#if !NET_4_0
        public override async Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            log.Debug("Obtaining DbConnection from Driver");
            var conn = Driver.CreateConnection();
            try
            {
                string[] words = ConnectionString.Split(';');
                string newConnectionStr = "";
                foreach (string token in words)
                {
                    if (token.ToUpper().StartsWith("PASSWORD=") && token.Length > 11)
                    {
                        newConnectionStr += "Password=" + Crypto.Decrypt(token.Substring(10, token.Length - 11)) + ";";
                    }
                    else
                        newConnectionStr += token + ";";
                }
                conn.ConnectionString = newConnectionStr;
                await(conn.OpenAsync(cancellationToken)).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception)
            {
                conn.Dispose();
                throw;
            }

            return conn;
        }
#endif
    }
}
