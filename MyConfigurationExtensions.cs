using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using NHibernate.Cfg;

namespace MyHibernateUtil
{
    public static class MyConfigurationExtensions
    {
        public static Configuration Configure(this Configuration config, string factoryName, StreamReader sr)
        {

            // Load Configuration XML
            XmlDocument doc = new XmlDocument();
            doc.Load(sr);

            return config.Configure(PrepareConfiguration(doc, factoryName));
        }
        public static Configuration Configure(this Configuration config, string factoryName, string fileName = "hibernate.cfg.xml")
        {

            // Load Configuration XML
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            return config.Configure(PrepareConfiguration(doc, factoryName));
        }
        /// <summary>
        ///		Configure NHibernate from a specified session-factory.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="textReader">The XmlReader containing the hibernate-configuration.</param>
        /// <param name="factoryName">Name value of the desired session-factory.</param>
        /// <returns></returns>
        public static Configuration Configure(this Configuration config, XmlReader textReader, string factoryName)
        {
            // Load Configuration XML
            XmlDocument doc = new XmlDocument();
            doc.Load(textReader);

            return config.Configure(PrepareConfiguration(doc, factoryName));
        }
        /// <summary>
        ///		Parses the configuration xml and ensures the target session-factory is the only one included.
        /// </summary>
        /// <param name="doc">The XmlDocument containing the hibernate-configuration.</param>
        /// <param name="factoryName">Name value of the desired session-factory.</param>
        /// <returns></returns>
        private static XmlTextReader PrepareConfiguration(XmlDocument doc, string factoryName)
        {
            const string CONFIG_XSD_MUTATION = "-x-factories";

            // Add Proper Namespace
            XmlNamespaceManager namespaceMgr = new XmlNamespaceManager(doc.NameTable);
            namespaceMgr.AddNamespace("nhibernate", "urn:nhibernate-configuration-2.2" + CONFIG_XSD_MUTATION);

            // Query Elements
            XmlNode nhibernateNode = doc.SelectSingleNode("descendant::nhibernate:hibernate-configuration", namespaceMgr);

            if (nhibernateNode != null)
            {
                XmlNodeList xnl = nhibernateNode.SelectNodes("descendant::nhibernate:session-factory[@name!='" + factoryName + "']", namespaceMgr);
                if (xnl.Count > 0) //nhibernateNode.SelectSingleNode("descendant::nhibernate:session-factory[@name='" + factoryName + "']", namespaceMgr) != default(XmlNode))
                {
                    //foreach (XmlNode node in xnl)
                    for (int i = 0; i < xnl.Count; i++)
                    {
                        XmlNode node = xnl[i];
                        nhibernateNode.RemoveChild(node);
                    }
                }
                else
                    throw new Exception("<session-factory name=\"" + factoryName + "\"> element was not found in the configuration file.");
            }
            else
                throw new Exception("<hibernate-configuration xmlns=\"urn:nhibernate-configuration-2.2-x-factories\"> element was not found in the configuration file.");

            return new XmlTextReader(new StringReader(nhibernateNode.OuterXml.Replace(CONFIG_XSD_MUTATION, "")));
        }

        public static List<string> getFactoryList(string fileName)
        {
            XmlDocument doc = new XmlDocument();
            List<string> rtnList = new List<string>();
            try
            {
                doc.Load(fileName);
                const string CONFIG_XSD_MUTATION = "-x-factories";

                // Add Proper Namespace
                XmlNamespaceManager namespaceMgr = new XmlNamespaceManager(doc.NameTable);
                namespaceMgr.AddNamespace("nhibernate", "urn:nhibernate-configuration-2.2" + CONFIG_XSD_MUTATION);

                // Query Elements
                XmlNode nhibernateNode = doc.SelectSingleNode("descendant::nhibernate:hibernate-configuration", namespaceMgr);

                if (nhibernateNode != null)
                {
                    if (nhibernateNode.SelectNodes("descendant::nhibernate:session-factory", namespaceMgr).Count > 0 )
                        foreach (XmlNode node in nhibernateNode.SelectNodes("descendant::nhibernate:session-factory", namespaceMgr))
                            rtnList.Add(node.Attributes["name"].Value);
                    else
                        throw new Exception("<session-factory> element was not found in the configuration file.");
                }
                else
                    throw new Exception("<hibernate-configuration xmlns=\"urn:nhibernate-configuration-2.2-x-factories\"> element was not found in the configuration file.");
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
            }

            return rtnList;

        }

    }
}
