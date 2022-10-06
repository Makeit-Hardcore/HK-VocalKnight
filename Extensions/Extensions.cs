using System;
using System.IO;
using System.Collections.Generic;
using VocalKnight.Commands;
using UnityEngine;
using HutongGames.PlayMaker;
using SFCore;

namespace VocalKnight.Extensions
{
    public static class Extensions
    {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> self, out TKey key, out TValue value) 
            => (key, value) = (self.Key, self.Value);

        public static bool HasValue(this CameraEffects @enum, CameraEffects toCheck)
            => (@enum & toCheck) != 0;
        
        public static GameObject GetChild(this GameObject go, string child)
        {
            return go.transform.Find(child).gameObject;
        }

        public static void ClearState(this FsmState state)
        {
            state.Actions = Array.Empty<FsmStateAction>();
            state.Transitions = Array.Empty<FsmTransition>();
        }

        public static void ClearTransitions(this FsmState state)
        {
            state.Transitions = Array.Empty<FsmTransition>();
        }
        public static void ClearActions(this FsmState state)
        {
            state.Actions = Array.Empty<FsmStateAction>();
        }

        public static byte[] ReadAllBytes(this Stream instream)
        {
            if (instream is MemoryStream memStream)
                return memStream.ToArray();

            using (var memoryStream = new MemoryStream())
            {
                instream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
