﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace IISCI.Build
{
    public class XDTService
    {

        public static XDTService Instance = new XDTService();

        public string Process(BuildConfig config){
            string buildFolder = config.BuildFolder;

            string buildXDT = buildFolder + "\\build.xdt";


            File.WriteAllText(buildXDT, CreateXDT(config));

            string webConfigPath = Path.GetDirectoryName( config.BuildFolder + "\\Source\\" + config.WebProjectPath )  + "\\web.config";

            string webConfig = null;

            if (File.Exists(webConfigPath))
            {
                webConfig = File.ReadAllText(webConfigPath);

                webConfig = Transform(webConfig, buildXDT);

                if (!string.IsNullOrWhiteSpace(config.CustomXDT))
                {
                    string customXDT = buildFolder + "\\custom.xdt";
                    File.WriteAllText(customXDT, config.CustomXDT);
                    webConfig = Transform(webConfig, customXDT);
                }

            }

            return webConfig;
        }


        private string CreateXDT(BuildConfig config) {

            XNamespace xdt = "http://schemas.microsoft.com/XML-Document-Transform";

            var doc = XDocument.Parse("<?xml version=\"1.0\"?><configuration xmlns:xdt=\"http://schemas.microsoft.com/XML-Document-Transform\"></configuration>");

            var connectionStrings = new XElement(XName.Get("connectionStrings"));
            doc.Root.Add(connectionStrings);
            foreach (var item in config.ConnectionStrings)
            {
                XElement cnstr = new XElement(XName.Get("add"));
                connectionStrings.Add(cnstr);
                cnstr.SetAttributeValue(XName.Get("name"), item.Name);
                cnstr.SetAttributeValue(XName.Get("connectionString"), item.ConnectionString);
                cnstr.SetAttributeValue(xdt + "Transform", "SetAttributes");
                cnstr.SetAttributeValue(xdt + "Locator", "Match(name)");
                if(item.ProviderName!=null){
                    cnstr.SetAttributeValue(XName.Get("providerName"), item.ProviderName);
                }
            }
            var appSettings = new XElement(XName.Get("appSettings"));
            doc.Root.Add(appSettings);
            foreach (var item in config.AppSettings)
            {
                XElement cnstr = new XElement(XName.Get("add"));
                appSettings.Add(cnstr);
                cnstr.SetAttributeValue(XName.Get("key"), item.Key);
                cnstr.SetAttributeValue(XName.Get("value"), item.Value);
                cnstr.SetAttributeValue(xdt + "Transform", "SetAttributes");
                cnstr.SetAttributeValue(xdt + "Locator", "Match(key)");
            }

            return doc.ToString(SaveOptions.OmitDuplicateNamespaces);
        }

        private string Transform(string inputXml, string xdtPath) 
        {
            using (StringWriter outStream = new StringWriter())
            {
                Microsoft.Web.XmlTransform.XmlTransformation xtr = new Microsoft.Web.XmlTransform.XmlTransformation(xdtPath);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(inputXml);

                xtr.Apply(doc);

                doc.Save(outStream);

                return outStream.ToString();
            }
            
        }

    }
}
