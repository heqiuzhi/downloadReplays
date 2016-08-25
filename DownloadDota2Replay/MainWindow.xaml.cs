﻿using FirstFloor.ModernUI.Windows.Controls;
using ServiceStack.OrmLite;
using SteamKit2;
using SteamKit2.GC.Dota.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.Serialization;
using System.IO;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Core;

namespace DownloadDota2Replay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        private string downloadFolder;
        private Dictionary<int, string> allTeams;
        private IDbConnection myDB;
        private Dota2Client dota2Client;
        private delegate void StartDota2ClientDelegate();
        private delegate void GetMatchDetailDelegate();



        //委托函数不能传参数，待修改
        private int matchNum = 0;

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            // Handle closing logic, set e.Cancel as needed
            dota2Client.Disconnect();
        }

        public MainWindow()
        {
            InitializeComponent();

            //初始化要下载的队伍
            allTeams = new Dictionary<int, string>();
            allTeams.Add(4, "EHOME");
            allTeams.Add(5, "IG");
            allTeams.Add(15, "LGD");
            allTeams.Add(20, "TongFu");//GetTeamInfo pro=false
            allTeams.Add(726228, "VG");
            allTeams.Add(1375614, "Newbee");
            allTeams.Add(1520578, "CDEC");
            allTeams.Add(1836806, "Wings");
            allTeams.Add(1951061, "NB.Y");//GetTeamInfo pro=false
            allTeams.Add(1983234, "Bheart");//GetTeamInfo pro=false
            allTeams.Add(2208748, "DuoBao");//GetTeamInfo pro=false
            allTeams.Add(2414475, "Way");//GetTeamInfo pro=false
            allTeams.Add(2552118, "FTD.C");//GetTeamInfo pro=false
            allTeams.Add(2626685, "EHOME.K");//GetTeamInfo pro=false 
            allTeams.Add(2634810, "EHOME.L");//GetTeamInfo pro=false 
            allTeams.Add(2635099, "CDEC.Y");
            allTeams.Add(2640025, "IG.V");//GetTeamInfo pro=false
            allTeams.Add(2643401, "CDEC.A");//GetTeamInfo pro=false
            allTeams.Add(2777247, "VG.R");
            allTeams.Add(2860081, "FTD.A");//GetTeamInfo pro=false
            allTeams.Add(2860414, "TRG");//GetTeamInfo pro=false


            this.Title = "Dota2录像下载器——正在模拟DOTA2客户端登陆Steam...（请稍等）";
            Closing += this.OnWindowClosing;

            //初始化下载路径
            downloadFolder = @"D:\Dota2Replays\";
            downloadPathTB.Text = "当前路径:" + downloadFolder;

            //模拟DOTA2客户端，连接steam->登陆->开始Dota2游戏->收到OnClientWelcome消息（此时就可以向GC发消息了）
            StartDota2ClientDelegate startDota2ClientDelegate = new StartDota2ClientDelegate(this.StartDota2Client);
            startDota2ClientDelegate.BeginInvoke(null, null);
        }

        private void StartDota2Client()
        {
            dota2Client = new Dota2Client("heqiuzhidingtalk", "14heqiuzhi");
            dota2Client.Connect();
            Action action = () =>
            {
                this.Title = "Dota2录像下载器——可以使用了！";
                this.downloadReplay.IsEnabled = true;
            };
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, action);
            
        }


        private KeyValue repeatCallWebAPI(WebAPI.Interface APIInterface,string APIName, Dictionary<string, string> APIArgs,int APIVersion=1)
        {
            KeyValue keyValues = new KeyValue();
            try
            {
                keyValues=APIInterface.Call(APIName, APIVersion, APIArgs);
            }
            catch (WebException ex)
            {
                System.Threading.Thread.Sleep(31000);
                return repeatCallWebAPI(APIInterface, APIName, APIArgs, APIVersion);
            }
            return keyValues;
        }

        private void downloadReplay_Click(object sender, RoutedEventArgs e)
        {
            GetMatchDetailDelegate getMatchDetailDelegate = new GetMatchDetailDelegate(this.getMatchDetail);
            getMatchDetailDelegate.BeginInvoke(null, null); 
        }

        private void getMatchDetail()
        {
            using (WebAPI.Interface DOTA2Match_570 = WebAPI.GetInterface("IDOTA2Match_570", "5F578BA33DC48279C5C3026BBB7A6E2D"))
            {
                
                int results_remaining = int.MaxValue;
                ulong lastMatchId = 0;
                KeyValue kvMatchs = new KeyValue();
                while (results_remaining != 0)
                {
                    if (results_remaining == int.MaxValue)//第一次调用GetMatchHistory接口
                    {
                        Dictionary<string, string> newsArgs = new Dictionary<string, string>();
                        newsArgs["league_id"] = "4664";
                        kvMatchs = repeatCallWebAPI(DOTA2Match_570, "GetMatchHistory", newsArgs);
                    }
                    else
                    {
                        Dictionary<string, string> newsArgs = new Dictionary<string, string>();
                        newsArgs["league_id"] = "4664";
                        newsArgs["start_at_match_id"] = lastMatchId.ToString();
                        kvMatchs = repeatCallWebAPI(DOTA2Match_570, "GetMatchHistory", newsArgs);
                    }

                    results_remaining = kvMatchs["results_remaining"].AsInteger();
                    foreach (KeyValue aMatch in kvMatchs["matches"].Children)
                    {
                        ulong start_time = aMatch["start_time"].AsUnsignedLong();
                        lastMatchId = aMatch["match_id"].AsUnsignedLong();
                        if (start_time >= 1470153600)//只计入外卡赛之后的数据
                        {
                            
                            MyMatch theMatch = new MyMatch();
                            theMatch.matchDetail= dota2Client.getMatchDetail(lastMatchId);

                            if (theMatch.matchDetail.replay_state == CMsgDOTAMatch.ReplayState.REPLAY_AVAILABLE)
                            {
                                string match_replay_url = "http://replay" + theMatch.matchDetail.cluster.ToString() + ".valve.net/570/" + theMatch.matchDetail.match_id.ToString() + "_" + theMatch.matchDetail.replay_salt.ToString() + ".dem.bz2";
                                string downloadFileName = downloadFolder + theMatch.matchDetail.match_id.ToString() + ".dem.bz2";
                                //已经成功下载
                                if (File.Exists(downloadFolder + theMatch.matchDetail.match_id.ToString() + ".dem"))
                                    continue;
                                //没下载完成,删掉重新下载
                                if ((File.Exists(downloadFileName)) && (!File.Exists(downloadFolder + theMatch.matchDetail.match_id.ToString() + ".dem")))
                                    File.Delete(downloadFileName);
                                WebClient webClient = new WebClient();
                                //webClient.DownloadFile(new Uri(match_replay_url), System.AppDomain.CurrentDomain.BaseDirectory + @"\Assets\" + theMatch.matchDetail.match_id.ToString() + ".dem.bz2");
                                ServicePointManager.DefaultConnectionLimit = 512;
                                webClient.DownloadProgressChanged += (sender, e) => MyProgressChanged(sender, e, theMatch.matchDetail.match_id);
                                webClient.DownloadFileAsync(new Uri(match_replay_url), downloadFileName);
                                webClient.DownloadFileCompleted += (sender,e) => Completed(sender,e, downloadFileName);
                            }
                            
                        }
                        
                    }
                }
                
            }
        }

        private void MyProgressChanged(object sender, DownloadProgressChangedEventArgs e,ulong matchId)
        {
            Action action = () =>
            {
                downloadingInfoTB.Text = "当前正在下载：" + matchId.ToString() + "进度：" + e.ProgressPercentage.ToString() + "%";
            };
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, action);
        }
        

        private void Completed(object sender, AsyncCompletedEventArgs e,string downloadFileName)
        {
            matchNum++;
            Action action = () =>
            {
                downloadInfoTB.Text = "已下载" + matchNum.ToString() + "场比赛。";
            };
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, action);
            ExtractGZipSample(downloadFileName, downloadFolder);
        }

        public void ExtractGZipSample(string gzipFileName, string targetDir)
        {

            // Use a 4K buffer. Any larger is a waste.    
            byte[] dataBuffer = new byte[4096];

            using (System.IO.Stream fs = new FileStream(gzipFileName, FileMode.Open, FileAccess.Read))
            {
                using ( BZip2InputStream gzipStream = new BZip2InputStream(fs))
                {

                    // Change this to your needs
                    string fnOut = System.IO.Path.Combine(targetDir, System.IO.Path.GetFileNameWithoutExtension(gzipFileName));

                    using (FileStream fsOut = File.Create(fnOut))
                    {
                        StreamUtils.Copy(gzipStream, fsOut, dataBuffer);
                    }
                }
            }
        }

        private void setDownloadPathDB_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                downloadFolder = folderBrowserDialog.SelectedPath;
                downloadPathTB.Text = "当前路径:" + downloadFolder;
            }
        }
    }

}
