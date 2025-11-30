using System;
using UnityEngine;

namespace CysharpCoroutine.Handlers
{
    public sealed class YieldAsyncOperation : YieldHandler
    {
        public override Type YieldType => typeof(AsyncOperation);
        
        protected override bool HandleYield(VCoroutine.CoroutineRecorder recorder)
        {
            AsyncOperation asyncOperation = (AsyncOperation)recorder.Yield;
            return asyncOperation.isDone;
        }
    }
}