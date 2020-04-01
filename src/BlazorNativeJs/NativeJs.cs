#region using
using Microsoft.JSInterop;
using Mono.WebAssembly.Interop;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
#endregion using

namespace BlazorNativeJs
{
	public static class NativeJs
	{
		static readonly MethodInfo _invokeJSMethInfo;
		static readonly PropertyInfo _jsonSerializerOptionsPropInfo;
		static NativeJs()
		{
			_invokeJSMethInfo=typeof(MonoWebAssemblyJSRuntime).GetMethod("InvokeJS",BindingFlags.NonPublic|BindingFlags.Instance);
			_jsonSerializerOptionsPropInfo=typeof(JSRuntime).GetProperty("JsonSerializerOptions",BindingFlags.NonPublic|BindingFlags.Instance);
		}

		static Func<string,string,string> InvokeJS;
		static JsonSerializerOptions _jsonSerializerOptions;
		public static void Initialize(JSInProcessRuntime jSRuntime)
		{
			_jSRuntime=jSRuntime;
			InvokeJS=(identifier,argsJson) => (string)_invokeJSMethInfo.Invoke(jSRuntime,new object[] { identifier,argsJson });
			_jsonSerializerOptions=(JsonSerializerOptions)_jsonSerializerOptionsPropInfo.GetValue(jSRuntime,new object[0]);
		}

		static JSInProcessRuntime _jSRuntime;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static T Invoke<T>(string identifier,params object[] args)
			=> _jSRuntime.Invoke<T>(identifier,args);

		static void Invoke(string identifier,params object[] args)
			=> _jSRuntime.InvokeVoid(identifier,args);

		internal static NativeJsObject GetEventNative(string eventArgsJson)
		{
			JsonElement el=(JsonElement)JsonSerializer.Deserialize(eventArgsJson,typeof(object),_jsonSerializerOptions);
			if (!el.TryGetProperty("__ref",out el))
				return null;
			string val =el.GetString();
			if ((val!=null)&&(val.Length==_guidStringLen))
				return new NativeJsObject(Guid.Parse(val));
			return null;
		}

		internal const string _ref = "__ref::";
		internal const int _refLen = 7;
		internal const int _guidStringLen = 36;
		static object InvokeJsAndProcessResponse(string identifier,params object[] args)
		{
			string res = InvokeJS(identifier,JsonSerializer.Serialize(args,_jsonSerializerOptions));
			return ParseJsonValue(res);
		}

		internal static object ParseJsonValue(string value)
			=> 
				value==null ? null :
				(value.Length==_guidStringLen+2/*quotes*/+_refLen)&&(value[0]=='"')&&(string.CompareOrdinal(value,1,_ref,0,_refLen)==0) ? (object)new NativeJsObject(Guid.Parse(value.Substring(_refLen+1,_guidStringLen))) :
				ParseJsonArgs(value);

		internal static DynamicJsonValue ParseJsonArgs(string value)
			=> new DynamicJsonValue((JsonElement)JsonSerializer.Deserialize(value,typeof(object),_jsonSerializerOptions));
		

		public static dynamic GetDocument()
			=> new NativeJsObject(Guid.Parse(Invoke<string>("eval","JsObjects.set(document)")));

		public static dynamic GetWindow()
			=> new NativeJsObject(Guid.Parse(Invoke<string>("eval","JsObjects.set(window)")));

		internal static void ReleaseJsObjectReference(NativeJsObject jsObject)
			=> Invoke("eval",$"JsObjects.remove('{jsObject.NativeId:D}')");

		internal static string[] GetMembers(NativeJsObject jsObject)
			=> Invoke<string[]>("eval",$"var src=JsObjects.get('{jsObject.NativeId:D}');var res=[];for (var key in src)res.push(key);res");

		internal static object GetMemberValue(NativeJsObject jsObject,string memberName)
			=> InvokeJsAndProcessResponse("NativeJs.getMemberValue",jsObject.NativeId.ToString("D"),memberName);

		internal static object GetIndex(NativeJsObject jsObject,object[] indexes)
			=> InvokeJsAndProcessResponse("NativeJs.getIndex",jsObject.NativeId.ToString("D"),null,indexes);

		internal static void SetMemberValue(NativeJsObject jsObject,string memberName,object value)
			=> Invoke("NativeJs.setMemberValue",jsObject.NativeId.ToString("D"),memberName,ResolveJsObject(value));

		static object ResolveJsObject(object value)
			=> value is NativeJsObject jsVal ? _ref+jsVal.NativeId.ToString("D") : value;

		internal static object Call(NativeJsObject jsObject,string functionName,object[] args)
			=> InvokeJsAndProcessResponse("NativeJs.callMember",jsObject.NativeId.ToString("D"),functionName,args.Select(ResolveJsObject).ToArray());

		internal static object CallSelf(NativeJsObject jsObject,object[] args)
			=> InvokeJsAndProcessResponse("NativeJs.call",jsObject.NativeId.ToString("D"),args.Select(ResolveJsObject).ToArray());

		public static dynamic NewObject()
			=> new NativeJsObject(Guid.Parse(Invoke<string>("eval","JsObjects.set({})")));

		readonly static Dictionary<Guid,WeakReference> _functions=new Dictionary<Guid,WeakReference>();
		public static dynamic NewFunction(Delegate function)
		{
			//https://stackoverflow.com/questions/20129236/creating-functions-dynamically-in-js
			Guid id = Guid.NewGuid();
			_functions.Add(id,new WeakReference(function));
			var funBody = $"return DotNet.invokeMethodAsync(\"{nameof(BlazorNativeJs)}\",\"{nameof(WebAssemblyEventDispatcher.DispatchFunction)}\",{{\"funcId\":\"{id:D}\"}},JSON.stringify(NativeJs.handleFuncArgs(arguments)));";
			return new NativeJsObject(Guid.Parse(Invoke<string>("eval",$"JsObjects.set(new Function('{funBody}'))")),() => _functions.Remove(id));
		}

		#region NewFunction overloads
		public static dynamic NewFunction(Action function)
			=> NewFunction((Delegate)function);

		public static dynamic NewFunction<T>(Action<T> function)
			=> NewFunction((Delegate)function);

		public static dynamic NewFunction<T1,T2>(Action<T1,T2> function)
			=> NewFunction((Delegate)function);

		public static dynamic NewFunction<T1,T2,T3>(Action<T1,T2,T3> function)
			=> NewFunction((Delegate)function);

		public static dynamic NewFunction<T1,T2,T3,T4>(Action<T1,T2,T3,T4> function)
			=> NewFunction((Delegate)function);

		public static dynamic NewFunction<T1,T2,T3,T4,T5>(Action<T1,T2,T3,T4,T5> function)
			=> NewFunction((Delegate)function);

		public static dynamic NewFunction<T1,T2,T3,T4,T5,T6>(Action<T1,T2,T3,T4,T5,T6> function)
			=> NewFunction((Delegate)function);

		public static dynamic NewFunction<T1,T2,T3,T4,T5,T6,T7>(Action<T1,T2,T3,T4,T5,T6,T7> function)
			=> NewFunction((Delegate)function);

		public static dynamic NewFunction<T1,T2,T3,T4,T5,T6,T7,T8>(Action<T1,T2,T3,T4,T5,T6,T7,T8> function)
			=> NewFunction((Delegate)function);

		public static dynamic NewFunction<T1,T2,T3,T4,T5,T6,T7,T8,T9>(Action<T1,T2,T3,T4,T5,T6,T7,T8,T9> function)
			=> NewFunction((Delegate)function);

		public static dynamic NewFunction<T1,T2,T3,T4,T5,T6,T7,T8,T9,T10>(Action<T1,T2,T3,T4,T5,T6,T7,T8,T9,T10> function)
			=> NewFunction((Delegate)function);
		#endregion NewFunction overloads

		internal static WeakReference GetFunction(string id)
			=> _functions.TryGetValue(Guid.Parse(id),out WeakReference result) ? result : null;

		internal class DynamicJsonValue:DynamicObject
		{
			readonly JsonElement _json;
			internal DynamicJsonValue(JsonElement json)
				=> _json=json;

			public override bool TryConvert(ConvertBinder binder,out object result)
			{
				result=Cast(binder.Type);
				return true;
			}

			internal T Cast<T>()
				=> (T)Cast(typeof(T));

			internal object Cast(Type type)
				=> Convert.ChangeType(
					_json.ValueKind switch
					{
						JsonValueKind.Undefined => null,
						JsonValueKind.Null => null,
						JsonValueKind.True => true,
						JsonValueKind.False => false,
						JsonValueKind.String => _json.GetString(),
						JsonValueKind.Number => type==typeof(byte) ? (object)_json.GetByte()
								: type==typeof(DateTime) ? (object)_json.GetDateTime()
								: type==typeof(decimal) ? (object)_json.GetDecimal()
								: type==typeof(double) ? _json.GetDouble()
								: type==typeof(short) ? _json.GetInt16()
								: type==typeof(int) ? _json.GetInt32()
								: type==typeof(long) ? _json.GetInt64()
								: type==typeof(sbyte) ? _json.GetSByte()
								: type==typeof(float) ? _json.GetSingle()
								: type==typeof(ushort) ? _json.GetUInt16()
								: type==typeof(uint) ? _json.GetUInt32()
								: type==typeof(ulong) ? _json.GetUInt64()
								: throw new NotSupportedException($"Unsupported number type on reading JS value"),
						JsonValueKind.Array => _json.EnumerateArray().Cast<JsonElement>().Select(x => ParseJsonValue(x.GetRawText())).ToArray(),
						_ => throw new NotSupportedException($"{_json.ValueKind} type is not supported on reading JS value")
					}
					,type);
		}
	}

	public class NativeJsObject:DynamicObject
	{
		readonly Guid _id;
		public NativeJsObject(Guid id)
		{
			_id=id;
		}

		readonly Action _destroyCallback;
		public NativeJsObject(Guid id,Action destroyCallback):this(id)
		{
			_destroyCallback=destroyCallback;
		}

		internal Guid NativeId => _id;

		~NativeJsObject()
		{
			NativeJs.ReleaseJsObjectReference(this);
			_destroyCallback?.Invoke();
		}

		public override string ToString()
			=> _id.ToString();

		public override bool Equals(object obj)
			=> obj is NativeJsObject js&&js._id.Equals(_id);

		public override int GetHashCode()
			=> _id.GetHashCode();

		public override IEnumerable<string> GetDynamicMemberNames()
			=> NativeJs.GetMembers(this);

		public override bool TryGetMember(GetMemberBinder binder,out object result)
		{
			//return base.TryGetMember(binder,out result);
			result=NativeJs.GetMemberValue(this,binder.Name);
			return true;
		}

		public override bool TrySetMember(SetMemberBinder binder,object value)
		{
			//return base.TrySetMember(binder,value);
			NativeJs.SetMemberValue(this,binder.Name,value);
			return true;
		}

		public override bool TryInvokeMember(InvokeMemberBinder binder,object[] args,out object result)
		{
			//return base.TryInvokeMember(binder,args,out result);
			result=NativeJs.Call(this,binder.Name,args);
			return true;
		}

		public override bool TryInvoke(InvokeBinder binder,object[] args,out object result)
		{
			//return base.TryInvoke(binder,args,out result);
			result=NativeJs.CallSelf(this,args);
			return true;
		}

		public override bool TryGetIndex(GetIndexBinder binder,object[] indexes,out object result)
		{
			//return base.TryGetIndex(binder,indexes,out result);
			result=NativeJs.GetIndex(this,indexes);
			return true;
		}
	}
}
