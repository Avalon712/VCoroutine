using System;
using UnityEngine;

namespace VC.Handlers
{
    public sealed class YieldWaitWhile : YieldHandler
    {
        public override Type YieldType => typeof(WaitWhile);
        
        protected override bool HandleYield(VCoroutine.CoroutineRecorder recorder)
        {
            WaitWhile waitWhile = (WaitWhile)recorder.Yield;
            return waitWhile.keepWaiting;
        }
    }
}