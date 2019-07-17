﻿using NewLife.Net;
using NewLife.Serialization;
using System;
using System.Collections.Generic;
using Vsd.Entity;

namespace Vsd.Server
{
    class VsdServer : NetServer<VsdSession>
    {
        /// <summary>日志命令。打开后显示收发命令详情</summary>
        public Boolean CommandLog { get; set; }
    }

    class VsdSession : NetSession<VsdServer>
    {
        #region 属性
        /// <summary>当前登录设备</summary>
        public Device Device { get; set; }

        /// <summary>当前在线对象</summary>
        public DeviceOnline Online { get; set; }
        #endregion

        #region 主循环
        protected override void OnReceive(ReceivedEventArgs e)
        {
            //base.OnReceive(e);

            var str = e.Packet.ToStr();
            if (str.IsNullOrEmpty()) return;

            var dic = new JsonParser(str).Decode() as IDictionary<String, Object>;
            if (dic == null || dic.Count == 0) return;

            object result = null;
            var cmd = dic["cmd"] + "";

            // 输出日志
            if (Host.CommandLog)
                WriteLog("<={0}", str.Trim());
            else
                WriteLog("<={0}", cmd);

            switch (cmd)
            {
                case "dHeartbeat":
                    result = Heartbeat(cmd, dic);
                    break;
                case "dRecord":
                    result = UploadRecord(cmd, dic);
                    break;
            }

            // 处理结果，做出响应
            if (result != null)
            {
                var js = result.ToJson();

                if (Host.CommandLog) WriteLog("=>{0}", js.Trim());

                Send(js.GetBytes());
            }
        }
        #endregion

        #region 心跳
        public virtual Object Heartbeat(String cmd, IDictionary<String, Object> parameters)
        {
            var ps = parameters;
            var code = ps["snr"] + "";
            var ip = ps["ip"] + "";
            var name = ps["name"] + "";

            // 登录
            var dv = Login(code, name);

            // 在线
            var olt = CheckOnline();
            olt.PingCount++;
            olt.SaveAsync();

            // 修改日志前缀
            if (!name.IsNullOrEmpty()) LogPrefix = name + " ";

            return new
            {
                cmd,
                snr = code,
                ip,
                name,
                time = DateTime.Now.ToFullString(),
                heartInterval = 60,
                keepAliveTime = 10,
                loraID = 0,
                resetTime = TimeSpan.FromDays(1).Subtract(TimeSpan.FromSeconds(1)).ToString(),
                terminalMode = 1,
                terminalBaud = 115200,
                terminalParity = "none",
                terminalStopbit = 1,
            };
        }

        protected virtual Device Login(String code, String name)
        {
            var dv = Device.FindByCode(code);
            if (dv == null)
            {
                dv = new Device
                {
                    Name = name,
                    Code = code,
                    Enable = true,
                };
                dv.Insert();
            }

            dv.SaveAsync();

            Device = dv;

            return dv;
        }

        protected virtual DeviceOnline CheckOnline()
        {
            var olt = Online;
            if (olt == null)
            {
                var uri = Remote.EndPoint + "";
                olt = DeviceOnline.FindBySessionID(uri);
                if (olt == null)
                {
                    olt = new DeviceOnline
                    {
                        SessionID = uri,
                    };

                    var dv = Device;
                    if (dv != null)
                    {
                        olt.Name = dv + "";
                        olt.DeviceID = dv.ID;
                    }

                    olt.Insert();
                }

                Online = olt;
            }

            return olt;
        }
        #endregion

        #region 下发数据
        public virtual void DownloadData()
        {

        }
        #endregion

        #region 下发脚本
        public virtual void DownloadScript()
        {

        }
        #endregion

        #region 上传记录
        public virtual Object UploadRecord(String cmd, IDictionary<String, Object> parameters)
        {
            var ps = parameters;
            var code = ps["snr"] + "";
            var ip = ps["ip"] + "";
            var seq = ps["seq"].ToInt();

            // 记录集合
            var records = ps["records"] as IList<Object>;
            if (records != null && records.Count > 0)
            {
                foreach (var item in records)
                {
                    // 解析记录，数据是base64编码
                    var time = ps["date"].ToDateTime();
                    var data = (ps["record"] + "").ToBase64();
                    var s = ps["seq"].ToInt();
                    var name = ps["name"] + "";
                }
            }

            return new
            {
                cmd,
                snr = code,
                ip,
                seq,
                rsq = "pk",
            };
        }
        #endregion
    }
}