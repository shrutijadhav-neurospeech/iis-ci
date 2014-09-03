﻿using Microsoft.Web.Administration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace IISCI.Web.Controllers
{
    public class IISController : Controller
    {

        public static string IISStore = null;

        ServerManager ServerManager;

        public IISController()
        {
            ServerManager = new ServerManager();

            if (IISStore == null) {
                IISStore = System.Web.Configuration.WebConfigurationManager.AppSettings["IISCI.Store"];
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) {
                ServerManager.Dispose();
                ServerManager = null;
            }
        }

        [Authorize]
        public ActionResult Sites()
        {

            var sites = ServerManager.Sites.Select(x => new
            {
                Id = x.Id,
                Name = x.Name,
                Bindings = x.Bindings.Select(y => y.Host)
            }).ToList();

            return Json( sites, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        public ActionResult Build(int id)
        {
            string buildPath = IISStore + "\\" + id;

            string commandLine = id + " \"" + buildPath + "\"" ;

            return new BuildActionResult(commandLine);


        }

        [HttpGet]
        [Authorize]
        public ActionResult GetBuildConfig(int id)
        {
            BuildConfig config = null;
            string path = IISStore + "\\" + id + "\\build-config.json";
            if (System.IO.File.Exists(path))
            {
                config = JsonStorage.ReadFile<BuildConfig>(path);
            }
            else
            {
                config = new BuildConfig();
            }

            if (string.IsNullOrWhiteSpace(config.TriggerKey))
            {
                config.TriggerKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            }
            return Json(config, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        public ActionResult UpdateBuildConfig(int id)
        {
            string path = IISStore + "\\" + id + "\\build-config.json";

            string formValue = Request.Form["formModel"];

            var model = JsonConvert.DeserializeObject<BuildConfig>(formValue);

            JsonStorage.WriteFile(model, path);
            return Json(model);
        }

        public ActionResult BuildTrigger(int id, string key)
        {
            string path = IISStore + "\\" + id + "\\build-config.json";
            var model = JsonStorage.ReadFile<BuildConfig>(path);
            return Build(id);
        }
    }
}