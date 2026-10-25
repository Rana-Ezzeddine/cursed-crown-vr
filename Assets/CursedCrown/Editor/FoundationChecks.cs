using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug=UnityEngine.Debug;

namespace CursedCrown.Editor
{
    public static class FoundationChecks
    {
        static readonly List<Process> processes=new();
        static string reports;
        static double nextAction;
        static int launched, stage;
        static bool capacityPassed;
        static bool hostLossPassed;
        static string binary;
        static readonly double[] starts={0,2,3,4,5,9};
        static FoundationChecks()
        {
            AssemblyReloadEvents.beforeAssemblyReload+=Abort;
            EditorApplication.quitting+=Abort;
        }
        [MenuItem("Cursed Crown/4 - Run Local Six-Process Network Checks")]
        public static void Run()
        {
            if(processes.Count>0)throw new InvalidOperationException("Checks already running.");
            binary=DesktopTestPlayer.BinaryPath();
            VerifyPoseValidation();
            MemberOneChecks.VerifyInputs();
            reports=Path.Combine(Path.GetTempPath(),"cursed-crown-smoke-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(reports);launched=0;stage=0;capacityPassed=false;hostLossPassed=false;nextAction=EditorApplication.timeSinceStartup;
            EditorApplication.update+=Tick;
            Debug.Log("CC_NETWORK_CHECKS_STARTED reports="+reports);
        }
        static void Tick()
        {
            try { Advance(); }
            catch (Exception ex) { Abort(); Debug.LogException(ex); }
        }
        static void Advance()
        {
            double elapsed=EditorApplication.timeSinceStartup-nextAction;
            if(launched<(stage==0?6:2)&&elapsed>=starts[launched])
            {
                int localIndex=launched++;
                int index=stage==0?localIndex:localIndex+(stage==1?6:8);
                int duration=stage==2?30:stage==1?(localIndex==0?10:18):(index==0?32:index==1?15:index==5?8:27);
                string args="-batchmode -nographics -ccMode "+(localIndex==0?"host":"client")+" -ccPort "+(17877+stage)+" -ccExitAfter "+duration+
                    " -ccReport \""+Path.Combine(reports,index+".json")+"\" -logFile \""+Path.Combine(reports,index+".log")+"\"";
                if(stage==2&&localIndex==1)args+=" -ccReconnectAfter 8 -ccReconnectDelay 3";
                processes.Add(Process.Start(new ProcessStartInfo(binary,args){UseShellExecute=false}));
            }
            if(elapsed>(stage==0?50:stage==1?27:40)){Finish();}
        }
        static void Finish()
        {
            EditorApplication.update-=Tick;
            bool passed=true;
            var summary=new List<string>();
            for(int i=stage==0?0:stage==1?6:8;i<(stage==0?6:stage==1?8:10);i++)
            {
                string path=Path.Combine(reports,i+".json");
                if(!File.Exists(path)){passed=false;summary.Add(i+": missing report");continue;}
                var r=JsonUtility.FromJson<FoundationSmokeProbe.Result>(File.ReadAllText(path));
                bool ok=stage==0?(i==5?r.rejected&&!r.connected&&r.errors==0:
                    r.connected&&r.peakPlayers==5&&r.validPoses&&r.uniqueSpawns&&r.remoteMotion>.01f&&r.errors==0):
                    stage==1?r.connected&&r.peakPlayers==2&&r.errors==0&&(i==6||r.sessionEnded):
                    r.connected&&r.peakPlayers==2&&r.validPoses&&r.uniqueSpawns&&r.remoteMotion>.01f&&r.errors==0&&
                    (i==8?r.joinedEvents>=3&&r.leftEvents>=1:r.connections>=2);
                if(i==0)ok&=r.leftEvents>0;
                passed&=ok;summary.Add(i+": "+(ok?"PASS":"FAIL")+" "+JsonUtility.ToJson(r));
            }
            File.AppendAllLines(Path.Combine(reports,"summary.txt"),summary);
            CleanupProcesses();
            if(stage==0&&passed)
            {
                capacityPassed=true;stage=1;launched=0;nextAction=EditorApplication.timeSinceStartup;
                EditorApplication.update+=Tick;
                Debug.Log("CC_CAPACITY_CHECKS_PASS. Now checking host departure.");return;
            }
            if(stage==1&&passed)
            {
                hostLossPassed=true;stage=2;launched=0;nextAction=EditorApplication.timeSinceStartup;
                EditorApplication.update+=Tick;
                Debug.Log("CC_HOST_LOSS_PASS. Now checking late join and reconnect.");return;
            }
            if(passed&&capacityPassed&&hostLossPassed)Debug.Log("CC_NETWORK_CHECKS_PASS: capacity, poses, departures, late join and reconnect. "+reports);
            else Debug.LogError("CC_NETWORK_CHECKS_FAIL: inspect "+reports);
        }
        static void CleanupProcesses()
        {
            foreach(var process in processes)
            {
                if(process==null)continue;
                try { if(!process.HasExited)process.Kill(); }
                finally { process.Dispose(); }
            }
            processes.Clear();
        }
        static void Abort()
        {
            EditorApplication.update-=Tick;
            CleanupProcesses();
        }
        public static void VerifyPoseValidation()
        {
            var pose=new AvatarPose{Head=Vector3.up*1.65f,Left=new Vector3(-.2f,1.2f,.3f),Right=new Vector3(.2f,1.2f,.3f),
                HeadRotation=Quaternion.identity,LeftRotation=Quaternion.identity,RightRotation=Quaternion.identity};
            if(!pose.IsValid())throw new Exception("Valid pose rejected.");
            pose.Head.x=float.NaN;if(pose.IsValid())throw new Exception("NaN pose accepted.");
            pose.Head=Vector3.up*1.65f;pose.Left=Vector3.one*20;if(pose.IsValid())throw new Exception("Out-of-range controller accepted.");
            pose.Left=new Vector3(-.2f,1.2f,.3f);pose.HeadRotation=new Quaternion(0,0,0,0);if(pose.IsValid())throw new Exception("Invalid quaternion accepted.");
            Debug.Log("CC_POSE_VALIDATION_PASS");
        }
        [MenuItem("Cursed Crown/Run All Foundation Checks (build + network)")]
        public static void RunAll()
        {if(!File.Exists(StarterSetup.ScenePath))StarterSetup.CreateScene();StarterSetup.BuildDesktop();Run();}
    }
}
