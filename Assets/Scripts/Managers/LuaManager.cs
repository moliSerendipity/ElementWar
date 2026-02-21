using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using XLua;

public static class LuaManagerConfig
{
    public const string AddressablesLabel = "LuaScript";                        // Lua 脚本在 Addressables 中的 Label

    public const string AddressPrefix = "Lua/";                                 // Lua 脚本在 Address (Key) 中的前缀
    public const string AddressSuffix = ".lua.txt";                             // Lua 脚本在 Address (Key) 中的后缀

    public const string MainLuaEntry = "main";                                  // Lua 入口脚本名称 (不带扩展名)
    public const float GCInterval = 1.0f;                                       // GC 间隔 (秒)
}

/// <summary>
/// Lua虚拟机管理器
/// 职责：
/// 1. 维护 LuaEnv 单例
/// 2. 对接 Addressables 实现 Lua 脚本预加载 + 缓存
/// 3. 提供 C# 调用 Lua 的安全入口
/// 4. 内存管理（自动GC、资源释放）
/// </summary>
public class LuaManager : SingleMonoBase<LuaManager>
{
    public LuaEnv GlobalLuaEnv { get; private set; }                            // 全局唯一的 Lua 虚拟机

    // Lua 脚本缓存，避免重复加载
    private readonly Dictionary<string, byte[]> luaScriptCache = new Dictionary<string, byte[]>();
    private bool isPreloadCompleted = false;                                    // 预加载完成标志
    private float GCTimer = 0f;                                                 // GC 计时器

    protected override void Awake()
    {
        base.Awake();

        InitLuaEnv();
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (GlobalLuaEnv == null || !isPreloadCompleted)
            return;

        // 清理 C# → Lua 引用，防止内存泄漏
        GlobalLuaEnv.Tick();

        // 定时触发Lua GC
        GCTimer += Time.deltaTime;
        if (GCTimer > LuaManagerConfig.GCInterval)
        {
            GlobalLuaEnv.GcStep(0);
            GCTimer = 0f;
            Debug.Log($"[LuaManager][{Time.time:0.000}] Lua GC executed.");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        SafeDisposeLuaEnv();
        ClearLuaScriptCache();
    }

    #region 初始化 Lua 虚拟机，自定义加载器
    /// <summary>
    /// 初始化 Lua 虚拟机，自定义 Lua 加载器
    /// </summary>
    private void InitLuaEnv()
    {
        if (GlobalLuaEnv == null)
        {
            GlobalLuaEnv = new LuaEnv();
            // 添加自定义加载器（从缓存读取，避免重复加载）
            GlobalLuaEnv.AddLoader(CachedLuaLoader);
            Debug.Log($"[LuaManager][{Time.time:0.000}] XLua Environment Initialized.");
        }
    }

    /// <summary>
    /// 带缓存的 Lua 加载器 (从缓存读取，无缓存则抛异常，需提前预加载)
    /// </summary>
    /// <param name="_filePath"></param>
    /// <returns></returns>
    private byte[] CachedLuaLoader(ref string _filePath)
    {
        try
        {
            // 严格路径校验（防注入）：禁止绝对路径、上级目录、空路径
            if (string.IsNullOrEmpty(_filePath) || _filePath.Contains("..") || _filePath.StartsWith("/") || _filePath.StartsWith("\\"))
            {
                Debug.LogError($"[LuaManager][{Time.time:0.000}] Illegal Lua path: {_filePath}");
                return null;
            }
            // 转换路径格式
            // 约定：Lua 里的 require('main') 对应 Addressables Key "Lua/main.lua.txt"
            // 将 '.' 替换为 '/' 适配文件夹结构
            string loadPath = _filePath.Replace('.', '/');

            // 拼接前缀和后缀
            string cacheKey = $"{LuaManagerConfig.AddressPrefix}{loadPath}{LuaManagerConfig.AddressSuffix}";

            // 从缓存读取，避免重复加载
            if (luaScriptCache.TryGetValue(cacheKey, out byte[] bytes))
                return bytes;

            // 未找到缓存，触发降级方案 (Fallback)：同步加载
            Debug.LogError($"[LuaManager][{Time.time:0.000}] Cached lua script miss for '{cacheKey}'. Triggering synchronous fallback load. Please check preload logic.");

            var handle = Addressables.LoadAssetAsync<TextAsset>(cacheKey);
            TextAsset luaAsset = handle.WaitForCompletion();

            if (luaAsset != null)
            {
                byte[] resultBytes = luaAsset.bytes;
                // 自动加入缓存，下次 require 就不用再读了
                luaScriptCache[cacheKey] = resultBytes;
                Addressables.Release(handle);
                return resultBytes;
            }

            Debug.LogError($"[LuaManager][{Time.time:0.000}] Sync fallback failed. Script not found: {cacheKey}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LuaManager][{Time.time:0.000}] Load Lua script failed: {_filePath}, Error: {ex.Message}");
            return null;
        }
    }
    #endregion

    #region 销毁 Lua 虚拟机，清空缓存
    /// <summary>
    /// 安全销毁 Lua 虚拟机，先通知 Lua 侧清理资源，再触发 GC，最后释放 GlobalLuaEnv 实例
    /// </summary>
    private void SafeDisposeLuaEnv()
    {
        if (GlobalLuaEnv != null)
        {
            try
            {
                // 先执行Lua的销毁回调（通知Lua清理资源）
                SafeDoString("if OnLuaEnvDispose then OnLuaEnvDispose() end", "LuaEnvDispose");
                // 销毁
                GlobalLuaEnv.Dispose();
                Debug.Log($"[LuaManager][{Time.time:0.000}] XLua Environment Disposed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LuaManager][{Time.time:0.000}] Dispose LuaEnv failed: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                GlobalLuaEnv = null;
            }
        }
    }

    /// <summary>
    /// 清空Lua脚本缓存
    /// </summary>
    private void ClearLuaScriptCache()
    {
        luaScriptCache.Clear();
        Debug.Log($"[LuaManager][{Time.time:0.000}] Lua script cache cleared.");
    }
    #endregion

    /// <summary>
    /// 异步预加载所有打上 LuaScript 标签的脚本进入内存缓存
    /// </summary>
    public void PreloadAllLuaScripts(Action _onCompleted)
    {
        if (isPreloadCompleted)
        {
            _onCompleted?.Invoke();
            return;
        }

        Debug.Log($"[LuaManager] Start preloading all Lua scripts with label: {LuaManagerConfig.AddressablesLabel}");

        // 先找到所有带有该标签的文件的“地址信息 (Location)”
        Addressables.LoadResourceLocationsAsync(LuaManagerConfig.AddressablesLabel, typeof(TextAsset)).Completed += (locHandle) =>
        {
            var locations = locHandle.Result;
            if (locations == null || locations.Count == 0)
            {
                Debug.LogError($"[LuaManager] No Lua scripts found, please check the Addressables tag '{LuaManagerConfig.AddressablesLabel}' configuration");
                _onCompleted.Invoke();
                return;
            }

            int totalCount = locations.Count;
            int loadedCount = 0;

            // 根据地址，把文件一个一个读进内存
            foreach (var location in locations)
            {
                string addressableKey = location.PrimaryKey;
                Addressables.LoadAssetAsync<TextAsset>(addressableKey).Completed += (assetHandle) =>
                {
                    if (assetHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        luaScriptCache[addressableKey] = assetHandle.Result.bytes;
                        ++loadedCount;
                    }
                    else
                    {
                        Debug.LogError($"[LuaManager] Failed to preload Lua script: {addressableKey}");
                    }
                    // 加载完后立即释放 TextAsset 对象 (只保留 byte[] 缓存)
                    Addressables.Release(assetHandle);

                    // 检查是否全部加载完毕
                    if (loadedCount == totalCount)
                    {
                        isPreloadCompleted = true;
                        // 释放位置句柄
                        Addressables.Release(locHandle);
                        Debug.Log($"[LuaManager] All Lua scripts preloaded. Total: {totalCount}");
                        _onCompleted?.Invoke();
                    }
                };
            }
        };
    }

    /// <summary>
    /// 安全执行 Lua 脚本，捕获异常并输出错误日志
    /// </summary>
    /// <param name="_scriptContent"></param>
    /// <param name="_chunkName"></param>
    public void SafeDoString(string _scriptContent, string _chunkName = "LuaChunk")
    {
        if (GlobalLuaEnv == null || !isPreloadCompleted)
        {
            Debug.LogWarning($"[LuaManager][{Time.time:0.000}] LuaEnv not ready, skip execute: {_chunkName}");
            return;
        }
        try
        {
            GlobalLuaEnv.DoString(_scriptContent, _chunkName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LuaManager][{Time.time:0.000}] Execute Lua failed: {_chunkName}\nError: {ex.Message}\nStackTrace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 启动入口脚本 (需先完成预加载)
    /// </summary>
    public void StartGame()
    {
        if (!isPreloadCompleted)
        {
            Debug.LogWarning($"[LuaManager][{Time.time:0.000}] Core Lua scripts not preloaded, start game failed.");
            return;
        }

        // 加载并执行 main.lua
        SafeDoString($"require('{LuaManagerConfig.MainLuaEntry}')", "LuaMainEntry");
        Debug.Log($"[LuaManager][{Time.time:0.000}] Lua main script executed.");
    }
}
