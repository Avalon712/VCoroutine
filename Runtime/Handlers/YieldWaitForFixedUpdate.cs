using System;
using UnityEngine;

namespace VC.Handlers
{
    public sealed class YieldWaitForFixedUpdate : YieldHandler
    {
        public override Type YieldType =>  typeof(WaitForFixedUpdate);
        
        protected override bool HandleYield(VCoroutine.CoroutineRecorder recorder)
        {
            if (recorder.environment == VCoroutine.CoroutineRunEnvironment.Update)
            {
                recorder.environment = VCoroutine.CoroutineRunEnvironment.FixedUpdate;
                return false;
            }
            recorder.environment = VCoroutine.CoroutineRunEnvironment.Update;
            return true;
        }
    }
}