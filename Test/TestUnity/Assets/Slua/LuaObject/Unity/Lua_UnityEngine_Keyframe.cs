﻿using System;
using SLua;
using System.Collections.Generic;
[UnityEngine.Scripting.Preserve]
public class Lua_UnityEngine_Keyframe : LuaObject {
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int constructor(IntPtr l) {
		try {
			int argc = LuaDLL.lua_gettop(l);
			UnityEngine.Keyframe o;
			if(argc==8){
				System.Single a1;
				checkType(l,3,out a1);
				System.Single a2;
				checkType(l,4,out a2);
				System.Single a3;
				checkType(l,5,out a3);
				System.Single a4;
				checkType(l,6,out a4);
				System.Single a5;
				checkType(l,7,out a5);
				System.Single a6;
				checkType(l,8,out a6);
				o=new UnityEngine.Keyframe(a1,a2,a3,a4,a5,a6);
				pushValue(l,true);
				pushValue(l,o);
				return 2;
			}
			else if(argc==6){
				System.Single a1;
				checkType(l,3,out a1);
				System.Single a2;
				checkType(l,4,out a2);
				System.Single a3;
				checkType(l,5,out a3);
				System.Single a4;
				checkType(l,6,out a4);
				o=new UnityEngine.Keyframe(a1,a2,a3,a4);
				pushValue(l,true);
				pushValue(l,o);
				return 2;
			}
			else if(argc==4){
				System.Single a1;
				checkType(l,3,out a1);
				System.Single a2;
				checkType(l,4,out a2);
				o=new UnityEngine.Keyframe(a1,a2);
				pushValue(l,true);
				pushValue(l,o);
				return 2;
			}
			else if(argc<=1){
				o=new UnityEngine.Keyframe();
				pushValue(l,true);
				pushObject(l,o);
				return 2;
			}
			return error(l,"New object failed.");
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int get_time(IntPtr l) {
		try {
			UnityEngine.Keyframe self;
			checkValueType(l,1,out self);
			pushValue(l,true);
			pushValue(l,self.time);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int set_time(IntPtr l) {
		try {
			UnityEngine.Keyframe self;
			checkValueType(l,1,out self);
			float v;
			checkType(l,2,out v);
			self.time=v;
			setBack(l,self);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int get_value(IntPtr l) {
		try {
			UnityEngine.Keyframe self;
			checkValueType(l,1,out self);
			pushValue(l,true);
			pushValue(l,self.value);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int set_value(IntPtr l) {
		try {
			UnityEngine.Keyframe self;
			checkValueType(l,1,out self);
			float v;
			checkType(l,2,out v);
			self.value=v;
			setBack(l,self);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int get_inTangent(IntPtr l) {
		try {
			UnityEngine.Keyframe self;
			checkValueType(l,1,out self);
			pushValue(l,true);
			pushValue(l,self.inTangent);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int set_inTangent(IntPtr l) {
		try {
			UnityEngine.Keyframe self;
			checkValueType(l,1,out self);
			float v;
			checkType(l,2,out v);
			self.inTangent=v;
			setBack(l,self);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int get_outTangent(IntPtr l) {
		try {
			UnityEngine.Keyframe self;
			checkValueType(l,1,out self);
			pushValue(l,true);
			pushValue(l,self.outTangent);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int set_outTangent(IntPtr l) {
		try {
			UnityEngine.Keyframe self;
			checkValueType(l,1,out self);
			float v;
			checkType(l,2,out v);
			self.outTangent=v;
			setBack(l,self);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int get_inWeight(IntPtr l) {
		try {
			UnityEngine.Keyframe self;
			checkValueType(l,1,out self);
			pushValue(l,true);
			pushValue(l,self.inWeight);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int set_inWeight(IntPtr l) {
		try {
			UnityEngine.Keyframe self;
			checkValueType(l,1,out self);
			float v;
			checkType(l,2,out v);
			self.inWeight=v;
			setBack(l,self);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int get_outWeight(IntPtr l) {
		try {
			UnityEngine.Keyframe self;
			checkValueType(l,1,out self);
			pushValue(l,true);
			pushValue(l,self.outWeight);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int set_outWeight(IntPtr l) {
		try {
			UnityEngine.Keyframe self;
			checkValueType(l,1,out self);
			float v;
			checkType(l,2,out v);
			self.outWeight=v;
			setBack(l,self);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int get_weightedMode(IntPtr l) {
		try {
			UnityEngine.Keyframe self;
			checkValueType(l,1,out self);
			pushValue(l,true);
			pushEnum(l,(int)self.weightedMode);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int set_weightedMode(IntPtr l) {
		try {
			UnityEngine.Keyframe self;
			checkValueType(l,1,out self);
			UnityEngine.WeightedMode v;
			checkEnum(l,2,out v);
			self.weightedMode=v;
			setBack(l,self);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[UnityEngine.Scripting.Preserve]
	static public void reg(IntPtr l) {
		getTypeTable(l,"UnityEngine.Keyframe");
		addMember(l,"time",get_time,set_time,true);
		addMember(l,"value",get_value,set_value,true);
		addMember(l,"inTangent",get_inTangent,set_inTangent,true);
		addMember(l,"outTangent",get_outTangent,set_outTangent,true);
		addMember(l,"inWeight",get_inWeight,set_inWeight,true);
		addMember(l,"outWeight",get_outWeight,set_outWeight,true);
		addMember(l,"weightedMode",get_weightedMode,set_weightedMode,true);
		createTypeMetatable(l,constructor, typeof(UnityEngine.Keyframe),typeof(System.ValueType));
	}
}
