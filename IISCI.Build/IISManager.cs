﻿using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IISCI.Build
{
    public class IISManager
    {
        public static IISManager Instance = new IISManager();


        internal void DeployFiles(BuildConfig config, string webConfig)
        {
            using (ServerManager mgr = new ServerManager()) {
                Site site = null;
                if (!string.IsNullOrWhiteSpace(config.SiteId))
                {
                    site = mgr.Sites.FirstOrDefault(x => x.Name == config.SiteId);
                }
                if (site == null) {
                    throw new KeyNotFoundException("No site with id " + config.SiteId + " found in IIS");
                }

                var app = site.Applications.FirstOrDefault();



                // copy all files...
                var dir = app.VirtualDirectories.FirstOrDefault();
                var rootFolder = dir.PhysicalPath;
                DirectoryInfo rootDir = new DirectoryInfo(rootFolder);

                if (config.DeployInNewFolder)
                {

                    //string newFolder = Guid.NewGuid().ToString().Trim('{', '}');
                    string newFolder = DateTime.Now.ToString("yyyy-MMM-dd-hh-mm-ss");
                    DirectoryInfo deploymentFolder = rootDir.Parent.CreateSubdirectory(config.SiteId + "-" + newFolder, rootDir.GetAccessControl());
                    rootFolder = deploymentFolder.FullName;
                }
                else
                {
                    if (config.StopForDeploy)
                    {
                        site.Stop();
                    }
                }


                if (config.UseMSBuild) {
                    DeployWebProject(config, rootFolder);
                }

                FileInfo configFile = new FileInfo(rootFolder + "\\Web.Config");
                
                FileService.Instance.WriteAllText(configFile.FullName, webConfig , UnicodeEncoding.Unicode);

                if (config.DeployInNewFolder)
                {
                    dir.PhysicalPath = rootFolder;                    
                    mgr.CommitChanges();

                    rootDir.Delete(true);
                }
                else
                {
                    if (config.StopForDeploy)
                    {
                        site.Start();
                    }
                }
            }

            //Thread.Sleep(5000);

        }

        private void DeployWebProject(BuildConfig config, string rootFolder)
        {

            FileInfo webProjectFile = new FileInfo(config.BuildFolder + "\\Source\\" + config.WebProjectPath);
            DirectoryInfo dir = webProjectFile.Directory;

            XDocument doc = XDocument.Load(webProjectFile.FullName);

            List<WebProjectFile> files = GetContentList(doc.Descendants()).ToList();

            foreach(var file in files)
            {
                var sourcePath = dir.FullName + "\\" + file.FilePath;
                var targetPath = rootFolder + "\\" + file.WebPath;
                string targetDir = System.IO.Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
                if (File.Exists(sourcePath))
                {
                    FileService.Instance.Copy(sourcePath, targetPath, true);
                }
            };

            dir = new DirectoryInfo(dir.FullName + "\\bin");

            DirectoryInfo tdir = new DirectoryInfo(rootFolder + "\\bin");
            if (!tdir.Exists) {
                tdir.Create();
            }


            CopyDirectory(dir, tdir);
        }

        private void CopyDirectory(DirectoryInfo dir, DirectoryInfo tdir)
        {
            foreach (var dll in dir.EnumerateFiles())
            {
                var targetPath = tdir.FullName + "\\" + dll.Name;
                dll.CopyTo(targetPath, true);
            }
            foreach (var child in dir.EnumerateDirectories()) {
                var childTarget = new DirectoryInfo(tdir.FullName + "\\" + child.Name);
                if (!childTarget.Exists)
                    childTarget.Create();
                CopyDirectory(child, childTarget);
            }
        }

        private IEnumerable<WebProjectFile> GetContentList(IEnumerable<XElement> enumerable)
        {
            foreach (var item in enumerable.Where(x=>x.Name.LocalName == "Content"))
            {
                var at = item.Attributes().FirstOrDefault(x => x.Name.LocalName == "Include");
                if (at != null) {

                    var link = item.Elements().FirstOrDefault(x => x.Name.LocalName == "Link");
                    if (link != null) {
                        yield return new WebProjectFile { 
                            WebPath = link.Value,
                            FilePath = at.Value
                        };
                        continue;
                    }

                    yield return new WebProjectFile { 
                        WebPath = at.Value ,
                        FilePath = at.Value
                    };
                }
            }
        }

        public class WebProjectFile {
            public string WebPath { get; set; }
            public string FilePath { get; set; }
        }
    }
}
