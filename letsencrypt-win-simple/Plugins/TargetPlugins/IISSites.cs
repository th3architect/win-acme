﻿using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;
using System.Collections.Generic;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class IISSitesFactory : BaseTargetPluginFactory<IISSites>
    {
        public const string SiteServer = "IISSiteServer";
        public IISSitesFactory() : base(nameof(IISSites), "SAN certificate for all bindings of multiple IIS sites") { }
    }

    class IISSites : IISSite, ITargetPlugin
    {
        public IISSites(ILogService log, IISClient iisClient) : base(log, iisClient) {}

        Target ITargetPlugin.Default(IOptionsService optionsService) {
            var rawSiteId = optionsService.TryGetRequiredOption(nameof(optionsService.Options.SiteId), optionsService.Options.SiteId);
            var totalTarget = GetCombinedTarget(GetSites(false, false), rawSiteId);
            totalTarget.ExcludeBindings = optionsService.Options.ExcludeBindings;
            return totalTarget;
        }

        Target GetCombinedTarget(List<Target> targets, string sanInput)
        {
            List<Target> siteList = new List<Target>();
            if (sanInput == "s")
            {
                siteList.AddRange(targets);
            }
            else
            {
                string[] siteIDs = sanInput.Trim().Trim(',').Split(',').Distinct().ToArray();
                foreach (var idString in siteIDs)
                {
                    int id = -1;
                    if (int.TryParse(idString, out id))
                    {
                        var site = targets.Where(t => t.SiteId == id).FirstOrDefault();
                        if (site != null)
                        {
                            siteList.Add(site);
                        }
                        else
                        {
                            _log.Warning($"SiteId '{idString}' not found");
                        }
                    }
                    else
                    {
                        _log.Warning($"Invalid SiteId '{idString}', should be a number");
                    }
                }
                if (siteList.Count == 0)
                {
                    _log.Warning($"No valid sites selected");
                    return null;
                }
            }
            Target totalTarget = new Target
            {
                PluginName = IISSitesFactory.SiteServer,
                Host = string.Join(",", siteList.Select(x => x.SiteId)),
                HostIsDns = false,
                IIS = true,
                WebRootPath = "x", // prevent FileSystem
                AlternativeNames = siteList.SelectMany(x => x.AlternativeNames).Distinct().ToList()
            };
            return totalTarget;
        }

        Target ITargetPlugin.Aquire(IOptionsService optionsService, IInputService inputService)
        {
            List<Target> targets = GetSites(optionsService.Options.HideHttps, true).Where(x => x.Hidden == false).ToList();
            inputService.WritePagedList(targets.Select(x => Choice.Create(x, $"{x.Host} ({x.AlternativeNames.Count()} bindings) [@{x.WebRootPath}]", x.SiteId.ToString())).ToList());
            var sanInput = inputService.RequestString("Enter a comma separated list of site IDs, or 'S' to run for all sites").ToLower().Trim();
            var totalTarget = GetCombinedTarget(targets, sanInput);
            inputService.WritePagedList(totalTarget.AlternativeNames.Select(x => Choice.Create(x, "")));
            totalTarget.ExcludeBindings = inputService.RequestString("Press enter to include all listed hosts, or type a comma-separated lists of exclusions");
            return totalTarget;
        }

        Target ITargetPlugin.Refresh(Target scheduled)
        {
            // TODO: check if the sites still exist, log removed sites
            // and return null if none of the sites can be found (cancel
            // the renewal of the certificate). Maybe even save the "S"
            // switch somehow to add sites if new ones are added to the 
            // server.
            return scheduled;
        }

        public override IEnumerable<Target> Split(Target scheduled)
        {
            List<Target> targets = GetSites(false, false);
            string[] siteIDs = scheduled.Host.Split(',');
            var filtered = targets.Where(t => siteIDs.Contains(t.SiteId.ToString())).ToList();
            filtered.ForEach(x => {
                x.ExcludeBindings = scheduled.ExcludeBindings;
                x.ValidationPluginName = scheduled.ValidationPluginName;
                x.DnsAzureOptions = scheduled.DnsAzureOptions;
                x.DnsScriptOptions = scheduled.DnsScriptOptions;
                x.HttpFtpOptions = scheduled.HttpFtpOptions;
                x.HttpWebDavOptions = scheduled.HttpWebDavOptions;
            });
            return filtered.Where(x => x.GetHosts(true, true).Count > 0).ToList(); ;
        }
    }
}