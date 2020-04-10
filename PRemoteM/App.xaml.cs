﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using PRM;
using PRM.Core;
using PRM.Core.DB;
using PRM.Core.Model;
using PRM.Core.Protocol;
using PRM.Core.Protocol.RDP;
using Shawn.Ulits.RDP;
using PRM.View;
using Shawn.Ulits;

namespace PersonalRemoteManager
{
    // 服务端可以被代理调用的类
    internal class OneServiceRemoteProvider : MarshalByRefObject
    {
        public void Activate()
        {
            App.Window?.ActivateMe();
        }
    }



    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private Mutex _singleAppMutex = null;
        public static MainWindow Window = null;
        private const string ServiceIpcPortName = "asasdSF234asdfsegy2we456WAWDWADW"; // 定义一个 IPC 端口

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            try
            {
                _singleAppMutex = new Mutex(true, "PersonalRemoteManager", out var isFirst);
                if (!isFirst)
                {
                    var oneRemoteProvider = (OneServiceRemoteProvider)Activator.GetObject(typeof(OneServiceRemoteProvider), $"ipc://{ServiceIpcPortName}/one");
                    oneRemoteProvider.Activate();
                    Environment.Exit(0);
                }
                else
                {
                    // 服务端初始化代码：
                    var remoteProvider = new OneServiceRemoteProvider();

                    // 将 remoteProvider/OneServiceRemoteProvider 设置到这个路由，你还可以设置其它的 MarshalByRefObject 到不同的路由。
                    RemotingServices.Marshal(remoteProvider, "one");
                    ChannelServices.RegisterChannel(new IpcChannel(ServiceIpcPortName), false);



                    SystemConfig.Init(this.Resources);
                    
                    SystemConfig.GetInstance().Language.CurrentLanguageCode = "xxxx";
                    SystemConfig.GetInstance().Language.CurrentLanguageCode = "zh-cn";
                    SystemConfig.GetInstance().Language.CurrentLanguageCode = "en-us";


                    //var nw = new Window();
                    //var rdp = (Global.GetInstance().ServerList[1] as ProtocolServerRDP);
                    //rdp.RdpFullScreenFlag = ERdpFullScreenFlag.EnableFullScreen;
                    //rdp.RdpWindowResizeMode = ERdpWindowResizeMode.Fixed;
                    //nw.Content = new AxMsRdpClient09Host(rdp, nw);
                    //nw.ShowDialog();



                    Window = new MainWindow();
                    Window.Closed += (o, args) => { Environment.Exit(0); };
                    Window.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Environment.Exit(-1);
            }
        }
    }
}
