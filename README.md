# VCoroutine 使用说明

本文档基于包 `org.avalon.vcoroutine`（版本 1.0.0）源码整理，说明 **VCoroutine** 的架构、API、内置 `yield` 类型、自定义扩展与注意事项。

---

## 1. 概述

**VCoroutine** 是独立于 `MonoBehaviour.StartCoroutine` 的全局协程调度器，通过隐藏的 `DontDestroyOnLoad` 运行器在 **`Update` / `LateUpdate` / `FixedUpdate`** 三个阶段分别驱动协程。

与 Unity 内置协程相比，特点包括：

- **不依赖 MonoBehaviour**：任意处调用 `VCoroutine.Run` 即可启动。
- **显式协程 ID**：返回 `CoroutineID`（可隐式转换为 `ulong`），便于 `Kill` / `Stop` / `Resume`。
- **可插拔的 yield 处理**：通过 `CoroutineYieldHandleContext` 与 `YieldHandler` 扩展 `yield return` 支持的类型。
- **协程依赖图**：支持“先完成若干子协程，再运行主协程”的拓扑依赖，并可检测循环依赖。

命名空间为 **`VC`**；内置具体处理器在 **`VC.Handlers`**。

---

## 2. 安装与程序集

包位于工程 `Packages/org.avalon.vcoroutine@1.0.0/`，运行时程序集为 **`VCoroutine.Runtime`**。在脚本中引用：

```csharp
using VC;
using VC.Handlers; // 仅当需要继承 YieldHandler 或引用处理器类型时
```

---

## 3. 快速开始

### 3.1 启动协程

```csharp
using System.Collections;
using UnityEngine;
using VC;

IEnumerator Work()
{
    Debug.Log("开始");
    yield return new WaitForSeconds(1f);
    Debug.Log("一秒后");
}

void Start()
{
    CoroutineID id = VCoroutine.Run(Work());
    // 或带调试别名
    CoroutineID id2 = VCoroutine.Run(Work(), "MyWork");
}
```

### 3.2 扩展方法

`CoroutineExtensions` 为 `IEnumerator` 提供 `RunAsCoroutine`：

```csharp
Work().RunAsCoroutine();
Work().RunAsCoroutine("MyWork");
Work().RunAsCoroutine(customContext);
```

等价于对应的 `VCoroutine.Run` 重载。

---

## 4. 核心 API

### 4.1 `VCoroutine.Run`

| 重载                                                                        | 说明                                         |
| ------------------------------------------------------------------------- | ------------------------------------------ |
| `Run(IEnumerator)`                                                        | 使用默认 `CoroutineYieldHandleContext.Default` |
| `Run(IEnumerator, string aliasName)`                                      | 同上，并设置调试用别名                                |
| `Run(IEnumerator, CoroutineYieldHandleContext context)`                   | 指定 yield 上下文                               |
| `Run(IEnumerator, string aliasName, CoroutineYieldHandleContext context)` | 别名 + 自定义上下文                                |

首次调用 `Run` 时会懒创建名为 **`VCoroutine Runner`** 的隐藏 `GameObject`（`HideFlags.HideAndDontSave`），并 `DontDestroyOnLoad`。

**返回值**：`CoroutineID`，可与 `ulong` 互转。

### 4.2 生命周期控制

| 方法                          | 行为                     |
| --------------------------- | ---------------------- |
| `Kill(ulong coroutineId)`   | 终止协程；从调度表移除，并更新依赖图     |
| `Stop(ulong coroutineId)`   | **仅当**当前状态为 `Run` 时暂停  |
| `Resume(ulong coroutineId)` | **仅当**当前状态为 `Stop` 时恢复 |

内部状态枚举 `CoroutineStatus`：`Run`、`WaitDependencyCompleted`、`Stop`、`Kill`。

### 4.3 依赖组合

```csharp
// 多个依赖：coroutineId 需等 dependencies 全部结束后才继续调度为 Run
VCoroutine.CombineDependencies(in CoroutineID coroutineId, bool checkLoopDependency = true, params CoroutineID[] dependencies);

// 单个依赖
VCoroutine.CombineDependency(CoroutineID coroutineId, ulong dependencyCoroutineId, bool checkLoopDependency = true);
```

- 若检测到**循环依赖**且 `checkLoopDependency == true`，抛出 **`Exception("duplicate loop dependency")`**。
- 依赖关系维护在静态拓扑结构中；被依赖协程结束时，会从等待者的 `dependencies` 集合中移除对应项。

样例工程中的依赖示意（`DependencyTest`）：

```text
A ← B ← C ← F
    ←   ← E
A ← D
```

即：`A` 依赖 `B` 与 `D`；`B` 依赖 `C` 与 `E`；`C` 依赖 `F`。

### 4.4 在协程体内 `yield return CoroutineID`

内置处理器 **`YieldCoroutineID`** 会在当前协程上调用 `CombineDependency(当前, 被 yield 的 id)`，并将当前协程置为 **`WaitDependencyCompleted`**，直到被依赖协程结束。

典型用法：在串行逻辑里等待另一条协程跑完。

### 4.5 `VCoroutine.Environment`

静态属性，在每次 `Tick` 时写入当前所处的 **`CoroutineRunEnvironment`**：

- `Update`
- `LateUpdate`
- `FixedUpdate`

用于调试或区分当前回调发生在哪一阶段（见示例 `FuncTest`）。

---

## 5. 调度与执行顺序（重要）

### 5.1 新协程何时进入运行表

新 `Run` 的协程先进入 **`_appendingQueue`**，仅在 **`LateUpdate`** 阶段 tick 的开头被合并进 **`_coroutines`**。

因此：**同一帧内，`Run` 调用后第一次真正参与调度往往在当帧的 `LateUpdate`（此前 `Update` 阶段的 Tick 可能仍看不到该协程）。**

### 5.2 销毁与 `Kill` 队列

在 **`LateUpdate`** 阶段合并完追加队列后，会处理 **`_disposeQueue`**（完成或 `Kill` 的协程），执行资源回收与依赖图更新。

### 5.3 每帧步进次数

对每个已注册协程，在**与其 `CoroutineRunEnvironment` 匹配**的相位内，每帧最多执行：

1. 若上一 `yield` 尚未处理完（处理器返回 `false`），本帧只处理等待，**不** `MoveNext`。
2. 否则 **`MoveNext()` 一次**，再处理新的 `yield`。

因此 **`yield return null`** 表示“等到下一帧（在默认 `Update` 环境下）再前进一步”，与 Unity 常见习惯一致；但首帧入队时机仍受 5.1 节影响。

### 5.4 `WaitForFixedUpdate` / `WaitForEndOfFrame`

- **`WaitForFixedUpdate`**：处理器在 `Update` 与 `FixedUpdate` 之间切换协程的 `environment`，使后续逻辑对齐物理帧。
- **`WaitForEndOfFrame`**：在 `Update` 与 `LateUpdate` 之间切换，使逻辑跑到帧末。

---

## 6. 内置 `yield return` 类型（默认上下文）

默认上下文 `CoroutineYieldHandleContext.Default` 注册了下表处理器（见 `CoroutineYieldHandleContext` 构造函数）。

| `yield return` 类型                                                         | 处理器类                          | 行为摘要                                                      |
| ------------------------------------------------------------------------- | ----------------------------- | --------------------------------------------------------- |
| `WaitForSeconds`                                                          | `YieldWaitForSeconds`         | 通过非托管方式按 `Time.deltaTime` 递减内部计时（会修改 `WaitForSeconds` 实例） |
| `WaitForSecondsRealtime`                                                  | `YieldWaitForSecondsRealtime` | 依赖 Unity 的 `keepWaiting`                                  |
| `WaitForFixedUpdate`                                                      | `YieldWaitForFixedUpdate`     | 切换运行环境至物理更新相位                                             |
| `WaitForEndOfFrame`                                                       | `YieldWaitForEndOfFrame`      | 切换运行环境至 `LateUpdate`                                      |
| `WaitUntil`                                                               | `YieldWaitUntil`              | `!keepWaiting` 时继续                                        |
| `WaitWhile`                                                               | `YieldWaitWhile`              | `keepWaiting` 为真时继续等待                                     |
| `AsyncOperation`（含子类，如 `UnityWebRequestAsyncOperation`、`ResourceRequest`） | `YieldAsyncOperation`         | `isDone` 为真时继续                                            |
| `Task`                                                                    | `YieldTask`                   | `IsCompleted` 为真时继续（不替代 `async/await` 的同步上下文语义，见下文注意）     |
| `IEnumerator`                                                             | `YieldEnumerator`             | **子协程**：压入同一 `recorder` 的栈，子迭代器跑完再弹出                      |
| `Func<bool>`                                                              | `YieldPredicate`              | 委托返回 `true` 时继续                                           |
| `CoroutineID`                                                             | `YieldCoroutineID`            | 注册对另一条协程的依赖并进入等待                                          |

未注册类型的 `yield` 值：无处理器时视为**立刻完成**该步（与 `yield return null` 类似，不阻塞处理器层）。

---

## 7. 自定义 `YieldHandler`

### 7.1 步骤

1. 继承 **`YieldHandler`**，指定 **`YieldType`**（要匹配的 `yield return` 对象运行时类型）。
2. 实现 **`protected abstract bool HandleYield(VCoroutine.CoroutineRecorder recorder)`**
   - 返回 **`true`**：本步 yield 已处理完，调度器会清空 `Yield` 并在后续帧继续 `MoveNext`。
   - 返回 **`false`**：仍在等待，本帧不再 `MoveNext`，下帧再调处理器。
3. 常用数据：从 **`recorder.Yield`** 读取当前对象；可按需写回（例如倒计时剩余秒数存在 `Yield` 上）。

### 7.2 注册到默认上下文

```csharp
CoroutineYieldHandleContext.RegisterHandlerToDefaultContext(new MyHandler());
```

若需移除：

```csharp
CoroutineYieldHandleContext.UnregisterHandlerFromDefaultContext<MyYieldType>();
```

### 7.3 独立上下文

```csharp
var ctx = new CoroutineYieldHandleContext(new List<YieldHandler> { new MyHandler(), /* ... */ });
VCoroutine.Run(MyEnumerator(), ctx);
```

示例工程 **`ExtensionTest`** 演示了 **`yield return float`**（自定义 `YieldFloat`）：在默认上下文注册后，`TestWaitTime` 使用默认；`TestTimer` 传入仅含 `YieldFloat` 的上下文，实现与默认内置类型隔离的组合。

### 7.4 类型查找规则

`GetHandler(Type)`：**先精确类型**，否则在已注册键中查找 **`IsAssignableFrom`** 的匹配，并缓存解析结果。因此处理器可注册基类型（如 `AsyncOperation`）服务多种子类。

---

## 8. `CoroutineID` 与 `CoroutineRecorder`

- **`CoroutineID`**：`readonly struct`，封装 `ulong`，值相等比较可用。
- 调度内部使用 **`CoroutineRecorder`**（`VCoroutine` 嵌套类），包含状态、迭代栈、`dependencies`、`Yield` 当前值、`Context` 等。一般业务代码无需直接使用；扩展 `YieldHandler` 时通过 `recorder` 访问即可。

---

## 9. 完整示例索引（包内 Sample）

工程导入 Sample **「VCoroutine Demo」** 后，可参考 `Assets/Samples/VCoroutine/1.0.0/VCoroutine Demo/Scripts/`：

| 脚本               | 内容                                                                                                                                                                            |
| ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `FuncTest`       | `WaitForEndOfFrame`、`null`、`WaitForFixedUpdate`、`WaitUntil`/`WaitWhile`、`Func<bool>`、`SendWebRequest`、`Task.Run`、嵌套 `IEnumerator`、`WaitForSecondsRealtime` / `WaitForSeconds` |
| `DependencyTest` | 多协程 `Run` + `CombineDependencies` / `CombineDependency` 拓扑                                                                                                                    |
| `ExtensionTest`  | 自定义 `YieldHandler` + 默认注册与独立 `CoroutineYieldHandleContext`                                                                                                                    |

---

## 10. 注意事项与最佳实践

1. **首帧调度**：依赖 `Run` 后立刻执行的首帧逻辑时，请考虑 5.1 的 `LateUpdate` 合帧行为，必要时延迟一帧或放在 `LateUpdate`/`yield return null` 之后验证。
2. **`WaitForSeconds` 实现**：使用 `GCHandle` 固定对象并直接修改内部浮点字段，属于**实现细节**，升级 Unity 版本存在潜在兼容性风险；关键项目可改用自定义 `float`/`struct` 处理器计时。
3. **`Task`**：仅轮询 `IsCompleted`；若在后台线程完成任务，后续 `MoveNext` 仍在 Unity 主线程调用，但**不**自动把 `async` 状态机“铺”回 Unity 生命周期，跨线程 UI 写入仍需自行 `SynchronizationContext`/主线程队列。
4. **循环依赖**：生产环境建议保持 **`checkLoopDependency: true`**；`DependencyTest` 注释说明了关闭检测时可能导致依赖链无法推进。
5. **`Stop`/`Resume`**：只对处于约定状态的协程生效，调用条件不满足时**静默无效**，调用方若需强保证应自行维护状态。
6. **运行器销毁**：`VCoroutineRunner` 被销毁时会清空内部表并回收对象池；正常游戏进程内一般不应销毁该隐藏对象。

---

## 11. 与 Unity `Coroutine` 的对比（简要）

| 项目       | Unity `StartCoroutine`                | VCoroutine                                         |
| -------- | ------------------------------------- | -------------------------------------------------- |
| 宿主       | 需 `MonoBehaviour`                     | 全局静态入口                                             |
| 停止方式     | `StopCoroutine` / `StopAllCoroutines` | `Kill` / `Stop` / `Resume` + ID                    |
| yield 扩展 | 固定集合 + `CustomYieldInstruction`       | `YieldHandler` + 可替换上下文                            |
| 多协程编排    | 自行管理                                  | `CombineDependencies` / `yield return CoroutineID` |

---

## 12. 版本与仓库信息

- **包名**：`org.avalon.vcoroutine`
- **版本**：1.0.0（以 `package.json` 为准）
- **最低 Unity**：2019.1（`package.json` `unity` 字段）
- **仓库**：[GitHub - Avalon712/VCoroutine: C# Coroutine For Unity3D · GitHub](https://github.com/Avalon712/VCoroutine.git)

---

*文档生成依据：包内 `Runtime` 源码与 Sample；若后续版本 API 变更，请以实际代码为准。*
