using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VC;

public sealed class FuncTest : MonoBehaviour
{
    void Start()
    {
        TestVCoroutine().RunAsCoroutine();
    }
    
    private IEnumerator TestVCoroutine()
    {
        Debug.Log($"{nameof(TestVCoroutine)} Start");

        yield return new WaitForEndOfFrame();
        
        print(VCoroutine.Environment.ToString());
        
        int i = 0;
        while (++i < 10)
        {
            yield return null;
            print(VCoroutine.Environment.ToString());
        }
        
        yield return new WaitForFixedUpdate();
        
        print(VCoroutine.Environment.ToString());
        
        yield return new WaitUntil(() => ++i >= 30);
        
        Debug.Log(i);
        
        yield return new WaitWhile(() => ++i >= 50);
        
        Debug.Log(i);
        
        yield return new Func<bool>(() => ++i >= 100);
        
        Debug.Log(i);
        
        UnityWebRequest request = UnityWebRequest.Get("https://www.baidu.com");
        yield return request.SendWebRequest();
        
        print(request.downloadHandler.text);
        
        yield return Task.Run(() => { while (++i <= 100000) { } });
        
        Debug.Log(i);
        
        Debug.Log("WaitForSecondsRealtime Start");
        yield return new WaitForSecondsRealtime(3);
        Debug.Log("WaitForSecondsRealtime End");
        
        yield return TestVCoroutine2();
        
        yield return TestVCoroutine3(); //支持泛型
        
        Debug.Log("WaitForSeconds Start");
        yield return new WaitForSeconds(5); 
        Debug.Log("WaitForSeconds End");
        
        Debug.Log($"{nameof(TestVCoroutine)} End");
    }

    private IEnumerator TestVCoroutine2()
    {
        Debug.Log($"{nameof(TestVCoroutine2)} Start");
        yield return new WaitForSecondsRealtime(3);
        Debug.Log($"{nameof(TestVCoroutine2)} End");
    }
    
    private IEnumerator<Func<bool>> TestVCoroutine3()
    {
        int i = 0;
        Debug.Log($"{nameof(TestVCoroutine3)} Start");
        yield return () => ++i >= 100;
        Debug.Log(i);
        Debug.Log($"{nameof(TestVCoroutine3)} End");
    }
}
