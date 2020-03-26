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
			if (res==null)
				return null;
			if ((res.Length==_guidStringLen+2/*quotes*/+_refLen)&&(res[0]=='"')&&(string.CompareOrdinal(res,1,_ref,0,_refLen)==0))
				return new NativeJsObject(Guid.Parse(res.Substring(_refLen+1,_guidStringLen)));

			JsonElement json = (JsonElement)JsonSerializer.Deserialize(res,typeof(object),_jsonSerializerOptions);
			object result = json.ValueKind switch
			{
				JsonValueKind.String=>json.GetString(),
				_=>throw new NotSupportedException($"{json.ValueKind} type is not supported on reading JS value")
			};
			return result;
		}

		public static dynamic GetDocument()
			=> new NativeJsObject(Guid.Parse(Invoke<string>("eval","JsObjects.set(document)")));

		public static dynamic GetWindow()
			=> new NativeJsObject(Guid.Parse(Invoke<string>("eval","JsObjects.set(window)")));

		internal static void ReleaseJsObjectReference(NativeJsObject jsObject)
			=> Invoke("eval",$"JsObjects.remove('{jsObject.NativeId.ToString("D")}')");

		internal static string[] GetMembers(NativeJsObject jsObject)
			=> Invoke<string[]>("eval",$"var src=JsObjects.get('{jsObject.NativeId.ToString("D")}');var res=[];for (var key in src)res.push(key);res");

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
	}

	public class NativeJsObject:DynamicObject
	{
		readonly Guid _id;
		public NativeJsObject(Guid id)
		{
			_id=id;
		}

		internal Guid NativeId => _id;

		~NativeJsObject()
		{
			NativeJs.ReleaseJsObjectReference(this);
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
