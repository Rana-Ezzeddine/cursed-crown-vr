using System;
using UnityEditor;
using UnityEngine;

namespace CursedCrown.Editor
{
    public static class MemberOneChecks
    {
        [MenuItem("Cursed Crown/Member 1/Check Inputs and Pose Guards")]
        public static void Run()
        {
            VerifyInputs();
            FoundationChecks.VerifyPoseValidation();
            Debug.Log("CC_MEMBER1_GUARDS_PASS");
        }

        public static void VerifyInputs()
        {
            ExpectAddress(" 192.168.001.010 ", "192.168.1.10");
            ExpectAddress("LOCALHOST", "127.0.0.1");
            ExpectAddress("10.0.0.1", "10.0.0.1");
            foreach (string value in new[] { null, "", "192.168.1", "192.168.1.256", "1", "1.2.3.4.5",
                "1.2.-3.4", "1.2. 3.4", "0.0.0.0", "224.0.0.1", "255.255.255.255", "::1", "host/name" })
                if (SessionInput.TryAddress(value, out _)) throw new Exception("Invalid LAN address accepted: " + value);
            if (!SessionInput.TryRelayCode(" ab12cd ", out string code) || code != "AB12CD")
                throw new Exception("Relay code was not trimmed and normalized.");
            foreach (string value in new[] { null, "", "   ", "AB CD", "CODE!", "../CODE", "\u00e9AB123", new string('A', 13) })
                if (SessionInput.TryRelayCode(value, out _)) throw new Exception("Invalid Relay code accepted.");
            if (!SessionInput.TryRelayCode(new string('A', SessionInput.MaxRelayCodeLength), out _))
                throw new Exception("Maximum code length should be accepted.");
            Debug.Log("CC_SESSION_INPUTS_PASS");
        }

        static void ExpectAddress(string input, string expected)
        {
            if (!SessionInput.TryAddress(input, out string actual) || actual != expected)
                throw new Exception("LAN normalization failed for " + input);
        }
    }
}
