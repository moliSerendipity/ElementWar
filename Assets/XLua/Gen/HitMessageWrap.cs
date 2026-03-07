#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
#endif

using XLua;
using System.Collections.Generic;


namespace XLua.CSObjectWrap
{
    using Utils = XLua.Utils;
    public class HitMessageWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(HitMessage);
			Utils.BeginObjectRegister(type, L, translator, 0, 0, 5, 5);
			
			
			
			Utils.RegisterFunc(L, Utils.GETTER_IDX, "instigatorID", _g_get_instigatorID);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "targetUID", _g_get_targetUID);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "targetConfigID", _g_get_targetConfigID);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "ammoID", _g_get_ammoID);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "hitMultiplier", _g_get_hitMultiplier);
            
			Utils.RegisterFunc(L, Utils.SETTER_IDX, "instigatorID", _s_set_instigatorID);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "targetUID", _s_set_targetUID);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "targetConfigID", _s_set_targetConfigID);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "ammoID", _s_set_ammoID);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "hitMultiplier", _s_set_hitMultiplier);
            
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 1, 0, 0);
			
			
            
			
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            
			try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
				
				if (LuaAPI.lua_gettop(L) == 1)
				{
				    translator.PushHitMessage(L, default(HitMessage));
			        return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to HitMessage constructor!");
            
        }
        
		
        
		
        
        
        
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_instigatorID(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                HitMessage gen_to_be_invoked;translator.Get(L, 1, out gen_to_be_invoked);
                LuaAPI.xlua_pushinteger(L, gen_to_be_invoked.instigatorID);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_targetUID(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                HitMessage gen_to_be_invoked;translator.Get(L, 1, out gen_to_be_invoked);
                LuaAPI.xlua_pushinteger(L, gen_to_be_invoked.targetUID);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_targetConfigID(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                HitMessage gen_to_be_invoked;translator.Get(L, 1, out gen_to_be_invoked);
                LuaAPI.xlua_pushinteger(L, gen_to_be_invoked.targetConfigID);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_ammoID(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                HitMessage gen_to_be_invoked;translator.Get(L, 1, out gen_to_be_invoked);
                LuaAPI.xlua_pushinteger(L, gen_to_be_invoked.ammoID);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_hitMultiplier(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                HitMessage gen_to_be_invoked;translator.Get(L, 1, out gen_to_be_invoked);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.hitMultiplier);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_instigatorID(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                HitMessage gen_to_be_invoked;translator.Get(L, 1, out gen_to_be_invoked);
                gen_to_be_invoked.instigatorID = LuaAPI.xlua_tointeger(L, 2);
            
                translator.UpdateHitMessage(L, 1, gen_to_be_invoked);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_targetUID(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                HitMessage gen_to_be_invoked;translator.Get(L, 1, out gen_to_be_invoked);
                gen_to_be_invoked.targetUID = LuaAPI.xlua_tointeger(L, 2);
            
                translator.UpdateHitMessage(L, 1, gen_to_be_invoked);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_targetConfigID(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                HitMessage gen_to_be_invoked;translator.Get(L, 1, out gen_to_be_invoked);
                gen_to_be_invoked.targetConfigID = LuaAPI.xlua_tointeger(L, 2);
            
                translator.UpdateHitMessage(L, 1, gen_to_be_invoked);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_ammoID(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                HitMessage gen_to_be_invoked;translator.Get(L, 1, out gen_to_be_invoked);
                gen_to_be_invoked.ammoID = LuaAPI.xlua_tointeger(L, 2);
            
                translator.UpdateHitMessage(L, 1, gen_to_be_invoked);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_hitMultiplier(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                HitMessage gen_to_be_invoked;translator.Get(L, 1, out gen_to_be_invoked);
                gen_to_be_invoked.hitMultiplier = (float)LuaAPI.lua_tonumber(L, 2);
            
                translator.UpdateHitMessage(L, 1, gen_to_be_invoked);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
		
		
		
		
    }
}
