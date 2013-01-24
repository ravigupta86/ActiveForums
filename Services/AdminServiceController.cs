﻿using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Web.Api;


namespace DotNetNuke.Modules.ActiveForums
{
    [ValidateAntiForgeryToken()]
    public class AdminServiceController : DnnApiController
    {
        // DTO for ToggleUrlHandler
        public class ToggleUrlHandlerDTO
        {
            public int ModuleId { get; set; }
        }

        [DnnAuthorize()]
        public HttpResponseMessage ToggleURLHandler(ToggleUrlHandlerDTO dto)
        {
            Entities.Modules.ModuleController objModules = new Entities.Modules.ModuleController();
            SettingsInfo objSettings = new SettingsInfo();
            objSettings.MainSettings = objModules.GetModuleSettings(dto.ModuleId);
            ConfigUtils cfg = new ConfigUtils();
            bool success = false;
            if (Utilities.IsRewriteLoaded())
            {
                success = cfg.DisableRewriter(HttpContext.Current.Server.MapPath("~/web.config"));
                return Request.CreateResponse(HttpStatusCode.OK, "disabled");
            }
            else
            {
                success = cfg.EnableRewriter(HttpContext.Current.Server.MapPath("~/web.config"));
                return Request.CreateResponse(HttpStatusCode.OK, "enabled");
            }
        }

        //DTO for RunMaintenance
        public class RunMaintenanceDTO
        {
            public int ModuleId { get; set; }
            public int ForumId { get; set; }
            public int olderThan { get; set; }
            public int byUserId { get; set; }
            public int lastActive { get; set; }
            public bool withNoReplies { get; set; }
            public bool dryRun { get; set; }
        }

        [DnnAuthorize()]
        public HttpResponseMessage RunMaintenance(RunMaintenanceDTO dto)
        {
            Entities.Modules.ModuleController objModules = new Entities.Modules.ModuleController();
            SettingsInfo objSettings = new SettingsInfo();
            objSettings.MainSettings = objModules.GetModuleSettings(dto.ModuleId);
            int rows = DataProvider.Instance().Forum_Maintenance(dto.ForumId, dto.olderThan, dto.lastActive, dto.byUserId, dto.withNoReplies, dto.dryRun, objSettings.DeleteBehavior);
            if (dto.dryRun)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new { Result = string.Format(Utilities.GetSharedResource("[RESX:Maint:DryRunResults]", true), rows.ToString()) });
                //return Json(new { Result = string.Format(Utilities.GetSharedResource("[RESX:Maint:DryRunResults]", true), rows.ToString()) });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.OK, new { Result = Utilities.GetSharedResource("[RESX:ProcessComplete]", true) });

                //return Json(new { Result = Utilities.GetSharedResource("[RESX:ProcessComplete]", true) });
            }
        }

        [DnnAuthorize()]
        public string GetSecurityGrid(int GroupId, int ForumId)  // Needs DTO
        {
            StringBuilder sb = new StringBuilder();

            return sb.ToString();
        }


        //DTO for ToggleSecurity
        public class ToggleSecurityDTO
        {
            public int ModuleId { get; set; }
            public string Action { get; set; }
            public int PermissionsId { get; set; }
            public string SecurityId { get; set; }
            public int SecurityType { get; set; }
            public string SecurityKey { get; set; }
            public string ReturnId { get; set; }
        }

        [DnnModuleAuthorize]
        [HttpPost]
        public HttpResponseMessage ToggleSecurity(ToggleSecurityDTO dto)
        {
            Data.Common db = new Data.Common();
            StringBuilder sb = new StringBuilder();
            switch (dto.Action)
            {
                case "delete":
                    {
                        Permissions.RemoveObjectFromAll(dto.SecurityId, dto.SecurityType, dto.PermissionsId);
                        return Request.CreateResponse(HttpStatusCode.OK);
                        //return string.Empty;
                    }
                case "addobject":
                    {
                        if (dto.SecurityType == 1)
                        {
                            UserController uc = new UserController();
                            User ui = uc.GetUser(PortalSettings.PortalId, dto.ModuleId, dto.SecurityId);
                            if (ui != null)
                            {
                                dto.SecurityId = ui.UserId.ToString();
                            }
                            else
                            {
                                dto.SecurityId = string.Empty;
                            }
                        }
                        else
                        {
                            if (dto.SecurityId.Contains(":"))
                            {
                                dto.SecurityType = 2;
                            }
                        }
                        if (!(string.IsNullOrEmpty(dto.SecurityId)))
                        {
                            string permSet = db.GetPermSet(dto.PermissionsId, "View");
                            permSet = Permissions.AddPermToSet(dto.SecurityId, dto.SecurityType, permSet);
                            db.SavePermSet(dto.PermissionsId, "View", permSet);
                        }
                        return Request.CreateResponse(HttpStatusCode.OK);
                        //return string.Empty;
                    }
                default:
                    {
                        string permSet = db.GetPermSet(dto.PermissionsId, dto.SecurityKey);
                        if (dto.Action == "remove")
                        {
                            permSet = Permissions.RemovePermFromSet(dto.SecurityId, dto.SecurityType, permSet);
                        }
                        else
                        {
                            permSet = Permissions.AddPermToSet(dto.SecurityId, dto.SecurityType, permSet);
                        }
                        db.SavePermSet(dto.PermissionsId, dto.SecurityKey, permSet);
                        return Request.CreateResponse(HttpStatusCode.OK, dto.Action + "|" + dto.ReturnId.ToString());
                        //return Action + "|" + ReturnId.ToString();
                    }
            }
        }

    }
}
