using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VC;

public sealed class DependencyTest : MonoBehaviour
{
    void Start()
    {
        /*
         依赖拓扑图
         A
         |----B
         |    |----C
         |    |    |----F
         |    |----E
         |----D
         */
        CoroutineID a = VCoroutine.Run(A());
        CoroutineID b = VCoroutine.Run(B());
        CoroutineID c = VCoroutine.Run(C());
        CoroutineID d = VCoroutine.Run(D());
        CoroutineID e = VCoroutine.Run(E());
        CoroutineID f = VCoroutine.Run(F());
        VCoroutine.CombineDependencies(a, true, b, d);
        VCoroutine.CombineDependencies(b, true, c, e);
        VCoroutine.CombineDependency(c,f);
        // VCoroutine.CombineDependency(f, a, false); //循环依赖，没有检测循环依赖，会导致所有依赖A的协程都无法运行
    }

    IEnumerator A()
    {
        print("A Start");
        yield return new WaitForSeconds(3);
        print("A End");
    }
        
    IEnumerator B()
    {
        print("B Start");
        yield return new WaitForSeconds(3);
        print("B End");
    }
        
    IEnumerator C()
    {
        print("C Start");
        yield return Task.Delay(3000);
        print("C End");
    }
        
    IEnumerator D()
    {
        print("D Start");
        UnityWebRequest request = UnityWebRequest.Get("https://www.baidu.com");
        yield return request.SendWebRequest();
        print("D End");
    }
        
    IEnumerator E()
    {
        print("E Start");
        ResourceRequest request = Resources.LoadAsync<GameObject>("Res");
        yield return request;
        Instantiate(request.asset);
        print("E End");
    }
    
    IEnumerator F()
    {
        print("F Start");
        yield return new WaitForFixedUpdate();
        print("F End");
    }
}