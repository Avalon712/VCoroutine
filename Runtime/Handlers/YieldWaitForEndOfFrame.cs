using System;
using UnityEngine;

namespace CysharpCoroutine.Handlers
{
    public sealed class YieldWaitForEndOfFrame : YieldHandler
    {
        public override Type YieldType => typeof(WaitForEndOfFrame);
        
        protected override bool HandleYield(VCoroutine.CoroutineRecorder recorder)
        {
            if (recorder.environment == VCoroutine.CoroutineRunEnvironment.Update)
            {
                recorder.environment = VCoroutine.CoroutineRunEnvironment.LateUpdate;
                return false;
            }
            recorder.environment = VCoroutine.CoroutineRunEnvironment.Update;
            return true;
        }
    }
}