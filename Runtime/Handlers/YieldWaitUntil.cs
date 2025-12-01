using System;
using UnityEngine;

namespace VC.Handlers
{
    public sealed class YieldWaitUntil : YieldHandler
    {
        public override Type YieldType => typeof(WaitUntil);
        
        protected override bool HandleYield(VCoroutine.CoroutineRecorder recorder)
        {
            WaitUntil waitUntil = (WaitUntil)recorder.Yield;
            return !waitUntil.keepWaiting;
        }
    }
}