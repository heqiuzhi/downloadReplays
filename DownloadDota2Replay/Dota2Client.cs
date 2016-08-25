using FirstFloor.ModernUI.Windows.Controls;
using SteamKit2;
using SteamKit2.Discovery;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;
using SteamKit2.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static DownloadDota2Replay.MainWindow;

namespace DownloadDota2Replay
{
    class Dota2Client
    {
        

        SteamClient steamClient;
        SteamUser steamUser;
        SteamGameCoordinator steamGameCoordinator;
        CallbackManager callbackMgr;

        string userName;
        string password;
        private bool isStartOver;
        private string startInfo;

        //steamGameCoordinator.Send没有同步方法？只能这样先等着
        private CMsgDOTAMatch tmpMatch;


        //public CMsgDOTAMatch Match { get; private set; }

        // dota2's appid
        const int APPID = 570;

        public Dota2Client(string userName, string password)
        {
            this.userName = userName;
            this.password = password;
            isStartOver = false;
            steamClient = new SteamClient();
            // get our handlers
            steamUser = steamClient.GetHandler<SteamUser>();
            steamGameCoordinator = steamClient.GetHandler<SteamGameCoordinator>();
            // setup callbacks
            callbackMgr = new CallbackManager(steamClient);
            callbackMgr.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            callbackMgr.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            callbackMgr.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            callbackMgr.Subscribe<SteamGameCoordinator.MessageCallback>(OnGCMessage);
        }

        public CMsgDOTAMatch getMatchDetail(ulong match_id)
        {
            var requestMatch = new ClientGCMsgProtobuf<CMsgGCMatchDetailsRequest>((uint)EDOTAGCMsg.k_EMsgGCMatchDetailsRequest);
            requestMatch.Body.match_id = match_id;
            tmpMatch = null;
            steamGameCoordinator.Send(requestMatch, APPID);
            while (tmpMatch == null)
            {
                callbackMgr.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
            return tmpMatch;
        }

        public void Connect()
        {
            //SteamClient.Servers.CellID = 148;
            //SteamClient.Servers.ServerListProvider = new FileStorageServerListProvider("servers_list.bin");
            SteamDirectory.Initialize().Wait();
            steamClient.Connect();
            while(!isStartOver)
            {
                // continue running callbacks until we get match details
                callbackMgr.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }

        public void Disconnect()
        {
            //程序关闭的时候，调用这个
            steamUser.LogOff();
            //steamClient.Disconnect();
        }

        void OnConnected(SteamClient.ConnectedCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Thread.Sleep(31000);
                steamClient.Connect();
                return;
            }
            //成功连接到steam...
            //startInfo = "成功连接到steam...";
            //ShowStartInfo();
            
            steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = userName,
                Password = password,
            });
        }

        void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Thread.Sleep(31000);
            steamClient.Connect();
        }

        void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                // logon failed (password incorrect, steamguard enabled, etc)
                // an EResult of AccountLogonDenied means the account has SteamGuard enabled and an email containing the authcode was sent
                // in that case, you would get the auth code from the email and provide it in the LogOnDetails
                ModernDialog.ShowMessage("登陆至Steam失败", callback.Result.ToString(), MessageBoxButton.OK);
                //等待修改为重新登陆（手动输入Email验证码）
                return;
            }
            // we've logged into the account
            // now we need to inform the steam server that we're playing dota (in order to receive GC messages)
            // steamkit doesn't expose the "play game" message through any handler, so we'll just send the message manually
            var playGame = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
            playGame.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
            {
                game_id = new GameID(APPID), // or game_id = APPID,
            });
            // send it off
            // notice here we're sending this message directly using the SteamClient
            steamClient.Send(playGame);
            // delay a little to give steam some time to establish a GC connection to us
            Thread.Sleep(5000);
            // inform the dota GC that we want a session
            var clientHello = new ClientGCMsgProtobuf<CMsgClientHello>((uint)EGCBaseClientMsg.k_EMsgGCClientHello);
            clientHello.Body.engine = ESourceEngine.k_ESE_Source2;
            steamGameCoordinator.Send(clientHello, APPID);
        }

        // called when a gamecoordinator (GC) message arrives
        // these kinds of messages are designed to be game-specific
        // in this case, we'll be handling dota's GC messages
        void OnGCMessage(SteamGameCoordinator.MessageCallback callback)
        {
            // setup our dispatch table for messages
            // this makes the code cleaner and easier to maintain
            var messageMap = new Dictionary<uint, Action<IPacketGCMsg>>
            {
                { ( uint )EGCBaseClientMsg.k_EMsgGCClientWelcome, OnClientWelcome },
                { ( uint )EDOTAGCMsg.k_EMsgGCMatchDetailsResponse, OnMatchDetails },
            };

            Action<IPacketGCMsg> func;
            if (!messageMap.TryGetValue(callback.EMsg, out func))
            {
                // this will happen when we recieve some GC messages that we're not handling
                // this is okay because we're handling every essential message, and the rest can be ignored
                return;
            }

            func(callback.Message);
        }

        // this message arrives when the GC welcomes a client
        // this happens after telling steam that we launched dota (with the ClientGamesPlayed message)
        // this can also happen after the GC has restarted (due to a crash or new version)
        void OnClientWelcome(IPacketGCMsg packetMsg)
        {
            // in order to get at the contents of the message, we need to create a ClientGCMsgProtobuf from the packet message we recieve
            // note here the difference between ClientGCMsgProtobuf and the ClientMsgProtobuf used when sending ClientGamesPlayed
            // this message is used for the GC, while the other is used for general steam messages
            var msg = new ClientGCMsgProtobuf<CMsgClientWelcome>(packetMsg);

            // 这里应该通知主线程，可以发送消息了
            // at this point, the GC is now ready to accept messages from us
            // so now we'll request the details of the match we're looking for
            isStartOver = true;
        }


        // this message arrives after we've requested the details for a match
        void OnMatchDetails(IPacketGCMsg packetMsg)
        {
            var msg = new ClientGCMsgProtobuf<CMsgGCMatchDetailsResponse>(packetMsg);

            EResult result = (EResult)msg.Body.result;
            if (result != EResult.OK)
            {
                ModernDialog.ShowMessage("无法查询比赛详情", result.ToString(), MessageBoxButton.OK);
            }

            tmpMatch = msg.Body.match;
        }

        // this is a utility function to transform a uint emsg into a string that can be used to display the name
        static string GetEMsgDisplayString(uint eMsg)
        {
            Type[] eMsgEnums =
            {
                typeof( EGCBaseClientMsg ),
                typeof( EDOTAGCMsg ),
                typeof( EGCBaseMsg ),
                typeof( EGCItemMsg ),
                typeof( ESOMsg ),
                typeof( EGCSystemMsg ),
            };

            foreach (var enumType in eMsgEnums)
            {
                if (Enum.IsDefined(enumType, (int)eMsg))
                    return Enum.GetName(enumType, (int)eMsg);

            }

            return eMsg.ToString();
        }


    }
}
