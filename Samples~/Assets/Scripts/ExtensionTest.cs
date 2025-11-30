using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CysharpCoroutine;

public sealed class ExtensionTest : MonoBehaviour
{
    /// <summary>
    /// 创建一个自定义的处理器上下文，处理定时回调功能
    /// </summary>
    private readonly CoroutineYieldHandleContext _timerYieldHandlerContext = new CoroutineYieldHandleContext(new List<YieldHandler>(){new  YieldFloat()});
    
    private void Awake()
    {
        CoroutineYieldHandleContext.RegisterHandlerToDefaultContext(new YieldFloat()); //注册到默认的yield处理器上下文中
    }

    void Start()
    {
        TestWaitTime().RunAsCoroutine(); 
        TestTimer().RunAsCoroutine(_timerYieldHandlerContext); //使用自定义的处理器上下文处理协程的返回类型
    }

    IEnumerator TestWaitTime()
    {
        print("TestWaitTime Start");
        yield return 5f; //等待5秒
        print("TestWaitTime End");
    }

    IEnumerator TestTimer()
    {
        yield return 10f; //等待10秒后再开始执行，默认延时效果
        
        print("TestTimer Start");
        yield return 1f;
        print("TestTimer End");
    }

    /// <summary>
    /// 当yield return float时表示等待指定的秒数
    /// </summary>
    private sealed class YieldFloat : YieldHandler
    {
        public override Type YieldType => typeof(float);
        
        protected override bool HandleYield(VCoroutine.CoroutineRecorder recorder)
        {
            float seconds = (float)recorder.Yield - Time.deltaTime;
            recorder.Yield = seconds;
            return seconds <= 0;
        }
    }
}
