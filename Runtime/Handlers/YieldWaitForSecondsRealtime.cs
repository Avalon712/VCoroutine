using System;
using UnityEngine;

namespace CysharpCoroutine.Handlers
{
    public sealed class YieldWaitForSecondsRealtime : YieldHandler
    {
        public override Type YieldType => typeof(WaitForSecondsRealtime);
        
        protected override bool HandleYield(VCoroutine.CoroutineRecorder recorder)
        {
            WaitForSecondsRealtime waitForSecondsRealtime = (WaitForSecondsRealtime)recorder.Yield;
            return !waitForSecondsRealtime.keepWaiting;
        }
    }
}