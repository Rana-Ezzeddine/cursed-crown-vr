using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CursedCrown.Editor
{
    // One-shot local CI entry. The request lives in ignored Temp/, never in Git.
    [InitializeOnLoad]
    public static class FoundationAutomation
    {
        public static bool Running { get; private set; }
        static double readyAfter;
        static FoundationAutomation()
        {readyAfter=EditorApplication.timeSinceStartup+3;EditorApplication.update+=Check;}
        static void Check()
        {
            const string request="Temp/RunFoundationChecks.request";
            if(Running||!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<readyAfter)return;
            File.Delete(request);Running=true;
            try{Debug.Log("CC_AUTOMATION_START");FoundationChecks.RunAll();}
            catch(Exception ex){Debug.LogException(ex);File.WriteAllText("Temp/FoundationAutomationFailure.txt",ex.ToString());}
            finally{Running=false;}
        }
    }
}
