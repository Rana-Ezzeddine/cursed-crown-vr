using System;
using System.IO;
using UnityEngine;

namespace CursedCrown
{
    // Opt-in local integration harness. Inactive in ordinary builds/sessions.
    public sealed class FoundationSmokeProbe : MonoBehaviour
    {
        [Serializable] public sealed class Result
        {
            public string mode, status;
            public bool connected, rejected, sessionEnded, validPoses=true, uniqueSpawns=true;
            public int peakPlayers, joinedEvents, leftEvents, errors, connections;
            public float remoteMotion;
        }
        Result result;
        string path;
        float started,exitAfter;
        bool active,launched;
        bool reconnectRequested, reconnectStarted;
        float reconnectAfter, reconnectDelay;
        FoundationSession session;
        readonly System.Collections.Generic.Dictionary<ulong,Vector3> previousRemote=new();
        string Arg(string name,string fallback="")
        {
            string[] args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]==name)return args[i+1];
            return fallback;
        }
        void Start()
        {
            string mode=Arg("-ccMode");
            if(mode!="host"&&mode!="client")return;
            active=true;started=Time.realtimeSinceStartup;Application.targetFrameRate=30;QualitySettings.vSyncCount=0;
            result=new Result{mode=mode};path=Arg("-ccReport",Path.Combine(Application.temporaryCachePath,"cc-test.json"));
            exitAfter=float.TryParse(Arg("-ccExitAfter","25"),out var seconds)?seconds:25;
            session=GetComponent<FoundationSession>();
            session.Port=ushort.Parse(Arg("-ccPort","17877"));session.Address="127.0.0.1";
            float.TryParse(Arg("-ccReconnectAfter","0"),out reconnectAfter);
            float.TryParse(Arg("-ccReconnectDelay","3"),out reconnectDelay);
            session.PlayerJoined+=id=>{result.joinedEvents++;if(id==session.Manager.LocalClientId)result.connections++;};
            session.PlayerLeft+=_=>result.leftEvents++;
            session.SessionEnded+=_=>result.sessionEnded=true;
            Application.logMessageReceived+=Log;
            if(FoundationRig.Instance!=null)FoundationRig.Instance.AutomatedMotion=true;
        }
        void Log(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)result.errors++;}
        void Update()
        {
            if(!active)return;
            if(!launched){launched=true;session.StartLan(result.mode=="host");}
            result.connected|=session.Manager.IsConnectedClient;
            result.peakPlayers=Mathf.Max(result.peakPlayers,session.PlayerCount);
            result.status=session.Status;
            result.rejected|=session.Status.Contains("Session full",StringComparison.OrdinalIgnoreCase);
            float elapsed=Time.realtimeSinceStartup-started;
            if(reconnectAfter>0&&!reconnectRequested&&elapsed>=reconnectAfter)
            {reconnectRequested=true;session.Leave();}
            if(reconnectRequested&&!reconnectStarted&&elapsed>=reconnectAfter+reconnectDelay&&session.CanStart)
            {reconnectStarted=true;session.StartLan(false);previousRemote.Clear();}
            var avatars=FindObjectsByType<FoundationAvatar>(FindObjectsSortMode.None);
            for(int i=0;i<avatars.Length;i++)
            {
                if(!avatars[i].IsSpawned)continue;
                if(Time.realtimeSinceStartup-started>3)result.validPoses&=avatars[i].TrackedPose.Value.IsValid();
                for(int j=i+1;j<avatars.Length;j++)
                    if(avatars[j].IsSpawned&&Vector3.Distance(avatars[i].RootPosition.Value,avatars[j].RootPosition.Value)<1)
                        result.uniqueSpawns=false;
                if(!avatars[i].IsOwner)
                {
                    Vector3 head=avatars[i].TrackedPose.Value.Head;
                    if(previousRemote.TryGetValue(avatars[i].OwnerClientId,out var previous))result.remoteMotion=Mathf.Max(result.remoteMotion,Vector3.Distance(previous,head));
                    previousRemote[avatars[i].OwnerClientId]=head;
                }
            }
            if(Time.realtimeSinceStartup-started>=exitAfter)Finish();
        }
        void Finish()
        {
            if(!active)return;
            active=false;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path,JsonUtility.ToJson(result,true));
            Application.logMessageReceived-=Log;
            session.Leave();Application.Quit(0);
        }
        void OnDestroy(){if(result!=null)Application.logMessageReceived-=Log;}
    }
}
