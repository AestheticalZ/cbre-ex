﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace CBRE.Editor.UI
{
    public class UpdateReleaseDetails
    {
        public string Tag { get; set; }
        public string Name { get; set; }
        public string Changelog { get; set; }
        public string FileName { get; set; }
        public string DownloadUrl { get; set; }

        public UpdateReleaseDetails(string jsonString)
        {
            JArray obj = JsonConvert.DeserializeObject(jsonString) as JArray;
            if (obj == null || obj.Count < 1) return;
            JObject rel = obj[0] as JObject;
            if (rel == null) return;
            JArray assets = rel.GetValue("assets") as JArray;
            if (assets == null || assets.Count < 1) return;
            JObject exeAsset = assets.FirstOrDefault(x => x is JObject && ((JObject)x).GetValue("name").ToString().EndsWith(".exe")) as JObject;
            if (exeAsset == null) return;

            Tag = rel.GetValue("tag_name").ToString();
            Name = rel.GetValue("name").ToString();
            Changelog = rel.GetValue("body").ToString();
            FileName = exeAsset.GetValue("name").ToString();
            DownloadUrl = exeAsset.GetValue("url").ToString();
        }

        public bool Exists { get { return Tag != null; } }
    }
}