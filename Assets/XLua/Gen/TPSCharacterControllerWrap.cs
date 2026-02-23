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
    public class TPSCharacterControllerWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(TPSCharacterController);
			Utils.BeginObjectRegister(type, L, translator, 0, 5, 13, 8);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetPlayerControl", _m_SetPlayerControl);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SwitchState", _m_SwitchState);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "CalculateMoveDirectionAndRotation", _m_CalculateMoveDirectionAndRotation);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetTargetSpeed", _m_GetTargetSpeed);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "ForceReload", _m_ForceReload);
			
			
			Utils.RegisterFunc(L, Utils.GETTER_IDX, "Motor", _g_get_Motor);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "Animator", _g_get_Animator);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "MainCameraTransform", _g_get_MainCameraTransform);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "IdleState", _g_get_IdleState);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "MoveState", _g_get_MoveState);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "JumpState", _g_get_JumpState);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "walkSpeed", _g_get_walkSpeed);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "runSpeed", _g_get_runSpeed);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "sprintSpeed", _g_get_sprintSpeed);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "aimMoveSpeed", _g_get_aimMoveSpeed);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "rotationSmoothTime", _g_get_rotationSmoothTime);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "jumpHeight", _g_get_jumpHeight);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "gravity", _g_get_gravity);
            
			Utils.RegisterFunc(L, Utils.SETTER_IDX, "MainCameraTransform", _s_set_MainCameraTransform);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "walkSpeed", _s_set_walkSpeed);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "runSpeed", _s_set_runSpeed);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "sprintSpeed", _s_set_sprintSpeed);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "aimMoveSpeed", _s_set_aimMoveSpeed);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "rotationSmoothTime", _s_set_rotationSmoothTime);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "jumpHeight", _s_set_jumpHeight);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "gravity", _s_set_gravity);
            
			
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
				if(LuaAPI.lua_gettop(L) == 1)
				{
					
					var gen_ret = new TPSCharacterController();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to TPSCharacterController constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetPlayerControl(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    bool __isPlayerControlled = LuaAPI.lua_toboolean(L, 2);
                    
                    gen_to_be_invoked.SetPlayerControl( __isPlayerControlled );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SwitchState(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    CharacterState __nextState = (CharacterState)translator.GetObject(L, 2, typeof(CharacterState));
                    
                    gen_to_be_invoked.SwitchState( __nextState );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_CalculateMoveDirectionAndRotation(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    UnityEngine.Vector2 __inputMove;translator.Get(L, 2, out __inputMove);
                    bool __isAiming = LuaAPI.lua_toboolean(L, 3);
                    
                        var gen_ret = gen_to_be_invoked.CalculateMoveDirectionAndRotation( __inputMove, __isAiming );
                        translator.PushUnityEngineVector3(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetTargetSpeed(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    UnityEngine.Vector2 __inputMove;translator.Get(L, 2, out __inputMove);
                    bool __isAiming = LuaAPI.lua_toboolean(L, 3);
                    bool __isSprinting = LuaAPI.lua_toboolean(L, 4);
                    
                        var gen_ret = gen_to_be_invoked.GetTargetSpeed( __inputMove, __isAiming, __isSprinting );
                        LuaAPI.lua_pushnumber(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_ForceReload(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.ForceReload(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_Motor(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.Motor);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_Animator(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.Animator);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_MainCameraTransform(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.MainCameraTransform);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_IdleState(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.IdleState);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_MoveState(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.MoveState);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_JumpState(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.JumpState);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_walkSpeed(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.walkSpeed);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_runSpeed(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.runSpeed);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_sprintSpeed(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.sprintSpeed);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_aimMoveSpeed(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.aimMoveSpeed);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_rotationSmoothTime(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.rotationSmoothTime);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_jumpHeight(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.jumpHeight);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_gravity(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.gravity);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_MainCameraTransform(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.MainCameraTransform = (UnityEngine.Transform)translator.GetObject(L, 2, typeof(UnityEngine.Transform));
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_walkSpeed(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.walkSpeed = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_runSpeed(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.runSpeed = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_sprintSpeed(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.sprintSpeed = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_aimMoveSpeed(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.aimMoveSpeed = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_rotationSmoothTime(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.rotationSmoothTime = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_jumpHeight(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.jumpHeight = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_gravity(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                TPSCharacterController gen_to_be_invoked = (TPSCharacterController)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.gravity = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
		
		
		
		
    }
}
