using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using NHibernate.Cfg;
using System.IO;
using System.Threading;

namespace MyHibernateUtil
{
    public class NHibernateHelper
    {
        public static IList<NHibernateHelper> ms_Helpers = new List<NHibernateHelper>();
        private static _InitNHibernateHelperSys ms_oInitNHibernateHelperSys = new _InitNHibernateHelperSys();
        private class _InitNHibernateHelperSys
        {
            public _InitNHibernateHelperSys()
            {
                NHibernate.AdoNet.Util.SqlStatementLogger.CloseSessionCallback = NHibernateHelper.CloseSessionCallback;
            }
        }

        private const int MAX_SESSION = 20;
        class SessionProxy
        {
            public ISession session = null;
            public DateTime? startDT = null;
            public int tid = 0;
        }
        SessionProxy[] sessionPool = new SessionProxy[MAX_SESSION];

        private Configuration Configuration { get; set; }
        public ISessionFactory SessionFactory { get; set; }
        //private ISession session = null;
        //private IStatelessSession statelessSession = null;

        public NHibernateHelper()
        {
            ms_Helpers.Add(this);
        }

        private void initSessionPool()
        {
            lock (sessionPool)
            {
                if (sessionPool[0] != null) return;
                for (int i = 0; i < MAX_SESSION; i++)
                    sessionPool[i] = new SessionProxy();
            }
        }

        public void CloseSessionPool()
        {
            lock (sessionPool)
            {
                for (int i = 0; i < MAX_SESSION; i++)
                {
                    SessionProxy sp = sessionPool[i];
                    if (sp == null || sp.session == null || sp.tid == 0 || sp.startDT == null) continue;
                    ITransaction currXact = sp.session.Transaction;
                    if (currXact != null)
                    {
                        if (currXact.IsActive)
                            currXact.Rollback();
                        currXact.Dispose();
                        currXact = null;
                    }
                    sp.session.Dispose();
                    sp.session = null;
                    sp.startDT = null;
                    sp.tid = 0;
                }
            }
        }


        private Configuration ConfigureNHibernate(string assembly)
        {
            Configuration = new Configuration();
            Configuration.AddAssembly(assembly);
            return Configuration;
        }
        public void Initialize(string fileName, string factoryName)
        {
            Configuration = new Configuration();
            Configuration = MyConfigurationExtensions.Configure(Configuration, factoryName, fileName);
            SessionFactory = Configuration.BuildSessionFactory();
            initSessionPool();
        }

        public void Initialize(StreamReader sr, string factoryName)
        {
            Configuration = new Configuration();
            Configuration = MyConfigurationExtensions.Configure(Configuration, factoryName, sr);
            SessionFactory = Configuration.BuildSessionFactory();
            initSessionPool();
        }
        public void Initialize(string assembly)
        {
            Configuration = ConfigureNHibernate(assembly);
            SessionFactory = Configuration.BuildSessionFactory();
            initSessionPool();
        }

        public void Dispose()
        {
            CloseSessionPool();
            lock (SessionFactory)
            {
                if (SessionFactory != null)
                {
                    SessionFactory.Close();
                    SessionFactory.Dispose();
                    SessionFactory = null;
                }
            }
        }
        
        public void CloseSession()
        {
            lock (sessionPool)
            {
                int tid = System.Threading.Thread.CurrentThread.ManagedThreadId;
                SessionProxy sp = null;
                int i;
                for (i = 0; i < MAX_SESSION; i++)
                {
                    sp = sessionPool[i];
                    if (sp == null || sp.session == null || sp.tid == 0 || sp.startDT == null) continue;
                    if (sp.tid == tid) break;
                }
                if (i == MAX_SESSION) return; // NOT FOUND
                ITransaction currXact = sp.session.Transaction;
                if (currXact != null)
                {
                    if (currXact.IsActive)
                        currXact.Rollback();
                    currXact.Dispose();
                    currXact = null;
                }
                sp.session.Clear();
                sp.session.Dispose();
                sp.session = null;
                sp.startDT = null;
                sp.tid = 0;
            }
        }
        
        public ISession Session
        {
            get
            {
                lock (sessionPool)
                {
                    int tid = System.Threading.Thread.CurrentThread.ManagedThreadId;

                    SessionProxy sp = null;
                    int i;
                    for (i = 0; i < MAX_SESSION; i++)
                    {
                        sp = sessionPool[i];
                        if (sp == null || sp.session == null || sp.tid == 0 || sp.startDT == null) continue;
                        if (sp.tid == tid) break;
                    }
                    if (i == MAX_SESSION) // not found
                    {
                        for (i = 0; i < MAX_SESSION; i++)
                        {
                            sp = sessionPool[i];
                            if (sp == null || sp.session == null || sp.tid == 0 || sp.startDT == null) break;
                        }
                        if(i == MAX_SESSION) // no spare session, free oldest session
                        {
                            DateTime now = DateTime.Now;
                            long max = 0, iTmp;
                            for (i = 0; i < MAX_SESSION; i++)
                            {
                                if ((iTmp = now.Ticks - sessionPool[i].startDT.Value.Ticks) > max)
                                {
                                    max = iTmp;
                                    sp = sessionPool[i];
                                }
                            }
                            // close session
                            if (sp.session.IsOpen)
                            {
                                ITransaction currXact = sp.session.Transaction;
                                if (currXact != null)
                                {
                                    if (currXact.IsActive)
                                        currXact.Rollback();
                                    currXact.Dispose();
                                    currXact = null;
                                }
                            }
                            sp.session.Dispose();
                            sp.session = null;
                            sp.startDT = null;
                            sp.tid = 0;
                        }
                    }
                    if (sp.session == null)
                    {
                        sp.session = SessionFactory.OpenSession();
                        sp.startDT = DateTime.Now;
                    }
                    sp.tid = tid;

                    return sp.session;
                }
            }
        }

        public string DumpSessions()
        {
            string rtnStr = "{tid, startDate, IsConnected, IsOpen}\n";
            lock(sessionPool)
            {
                SessionProxy sp = null;
                int i;
                for (i = 0; i < MAX_SESSION; i++)
                {
                    sp = sessionPool[i];
                    if (sp == null || sp.startDT == null) continue;
                    rtnStr += "{" + sp.tid + "," + sp.startDT.Value.ToString("yyyy/MM/dd hh:mm:ss" + "," + sp.session.IsConnected + "," + sp.session.IsOpen + "}\n");
                }
            }
            return rtnStr;
        }

        /*
        public IStatelessSession StatelessSession
        {
            get
            {
                if (statelessSession == null)
                {
                    statelessSession = SessionFactory.OpenStatelessSession();
                }
                return statelessSession;
            }
        }
         */

        public IList<T> ExecuteICriteria<T>()
        {
            using (ITransaction transaction = Session.BeginTransaction())
            {
                try
                {
                    IList<T> result = Session.CreateCriteria(typeof(T)).List<T>();
                    transaction.Commit();
                    return result;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public static void CloseSessionCallback(string sLastAction)
        {
            try
            {
                foreach (NHibernateHelper obj in ms_Helpers)
                    obj.Dispose();
                ms_Helpers.Clear();
            }
            catch (Exception) { }
        }
    }
}
