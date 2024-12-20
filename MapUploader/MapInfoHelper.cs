﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gameloop.Vdf;
using MapUploader.Model;
using MapUploader.Vpk;
using Newtonsoft.Json.Linq;

namespace MapUploader
{
    public class MapInfoHelper
    {
        public static MapInfo GetMapInfo(string filePath)
        {
            MapInfo info = new MapInfo
            {
                Id = Guid.NewGuid(),
                FilePath = filePath,
                Status = "等待中"
            };
            try
            {
                byte[] buffer;
                using (var pkg = new Package())
                {
                    pkg.Read(filePath);
                    var txtEntries =
                        pkg.Entries.FirstOrDefault(o => o.Key == "txt");
                    var missionEntry = txtEntries.Value.FirstOrDefault(o => o.DirectoryName == "missions");

                    pkg.ReadEntry(missionEntry, out buffer);
                }

                if (buffer == null || buffer.Length < 1)
                {
                    return info;
                }
                string data = Encoding.UTF8.GetString(buffer);
                var start = data.Count(o => o == '{');
                var end = data.Count(o => o == '}');
                if (end < start)
                {
                    for (int it = 0; it < start - end; it++)
                    {
                        data = data + Environment.NewLine + "}";
                    }
                }
                dynamic mission = VdfConvert.Deserialize(data);
                info.MapName = mission.Value.DisplayTitle?.Value ?? "";
                info.MapName = info.MapName.Replace("/", "-");
                info.MapCode = mission.Value.modes?.coop[0]?.Value.Map?.Value ?? "";
                if (string.IsNullOrEmpty(info.MapName) || string.IsNullOrEmpty(info.MapCode))
                {
                    info.Status = "没有解析到地图信息，请确认文件或双击修改地图名和代码";
                }
            }
            catch
            {
                info.Status = "解析地图信息异常，请确认文件或双击修改地图名和代码";
            }
            return info;
        }

        public static List<MapInfo> GetMapInfoes(JArray jaData)
        {
            List<MapInfo> infoList = new List<MapInfo>();
            foreach (var jToken in jaData)
            {
                var data = (JObject)jToken;
                if (data.ContainsKey("name") && data.ContainsKey("map"))
                {
                    MapInfo info = new MapInfo
                    {
                        Id = Guid.NewGuid(),
                        MapCode = data.GetValue("map")?.ToString(),
                        MapName = data.GetValue("name")?.ToString()
                    };
                    infoList.Add(info);
                }
            }

            return infoList;
        }
    }
}
