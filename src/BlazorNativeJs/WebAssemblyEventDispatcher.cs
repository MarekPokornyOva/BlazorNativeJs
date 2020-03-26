#region using
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Reflection;
using System.Threading.Tasks;
#endregion using

namespace BlazorNativeJs
{
	public static class WebAssemblyEventDispatcher
	{
		static Type _webEventDataType = Type.GetType("Microsoft.AspNetCore.Components.Web.WebEventData, Microsoft.AspNetCore.Blazor");
		static MethodInfo _webEventDataParse = _webEventDataType.GetMethod("Parse",BindingFlags.Public|BindingFlags.Static,null,new Type[] { typeof(WebEventDescriptor),typeof(string) },null);
		static PropertyInfo _eventHandlerId = _webEventDataType.GetProperty("EventHandlerId");
		static PropertyInfo _eventFieldInfo = _webEventDataType.GetProperty("EventFieldInfo");
		static PropertyInfo _eventArgs = _webEventDataType.GetProperty("EventArgs");
		static MethodInfo _rendererRegistryFind = Type.GetType("Microsoft.AspNetCore.Blazor.Rendering.RendererRegistry, Microsoft.AspNetCore.Blazor").GetMethod("Find",BindingFlags.NonPublic|BindingFlags.Static);

		[JSInvokable("DispatchEvent")]
		public static Task DispatchEvent(WebEventDescriptor eventDescriptor,string eventArgsJson)
		{
			//WebEventData webEventData = WebEventData.Parse(eventDescriptor,eventArgsJson);
			//return RendererRegistry.Find(eventDescriptor.BrowserRendererId).DispatchEventAsync(webEventData.EventHandlerId,webEventData.EventFieldInfo,webEventData.EventArgs);

			object webEventData = _webEventDataParse.Invoke(null,new object[] { eventDescriptor,eventArgsJson });
			Renderer renderer = (Renderer)_rendererRegistryFind.Invoke(null,new object[] { eventDescriptor.BrowserRendererId });
			EventArgs eventArgs = (EventArgs)_eventArgs.GetValue(webEventData);

			eventArgs=eventDescriptor.EventArgsType switch
			{
				"change" => new NativeChangeEventArgs((ChangeEventArgs)eventArgs,NativeJs.GetEventNative(eventArgsJson)),
				"clipboard" => new NativeClipboardEventArgs((ClipboardEventArgs)eventArgs,NativeJs.GetEventNative(eventArgsJson)),
				"drag" => new NativeDragEventArgs((DragEventArgs)eventArgs,NativeJs.GetEventNative(eventArgsJson)),
				"error" => new NativeErrorEventArgs((ErrorEventArgs)eventArgs,NativeJs.GetEventNative(eventArgsJson)),
				"focus" => new NativeFocusEventArgs((FocusEventArgs)eventArgs,NativeJs.GetEventNative(eventArgsJson)),
				"keyboard" => new NativeKeyboardEventArgs((KeyboardEventArgs)eventArgs,NativeJs.GetEventNative(eventArgsJson)),
				"mouse" => new NativeMouseEventArgs((MouseEventArgs)eventArgs,NativeJs.GetEventNative(eventArgsJson)),
				"pointer" => new NativePointerEventArgs((PointerEventArgs)eventArgs,NativeJs.GetEventNative(eventArgsJson)),
				"progress" => new NativeProgressEventArgs((ProgressEventArgs)eventArgs,NativeJs.GetEventNative(eventArgsJson)),
				"touch" => new NativeTouchEventArgs((TouchEventArgs)eventArgs,NativeJs.GetEventNative(eventArgsJson)),
				"unknown" => new NativeEventArgs((EventArgs)eventArgs,NativeJs.GetEventNative(eventArgsJson)),
				"wheel" => new NativeWheelEventArgs((WheelEventArgs)eventArgs,NativeJs.GetEventNative(eventArgsJson)),
				_ => eventArgs
			};
			return renderer.DispatchEventAsync((ulong)_eventHandlerId.GetValue(webEventData),(EventFieldInfo)_eventFieldInfo.GetValue(webEventData),eventArgs);
		}

		#region Native event args
		internal interface INativeEventArgs
		{
			dynamic Native { get; }
		}

		internal class NativeDragEventArgs:DragEventArgs, INativeEventArgs
		{
			readonly NativeJsObject _jsObject;
			internal NativeDragEventArgs(DragEventArgs src,NativeJsObject jsObject)
			{
				CopyProps(src,this);
				_jsObject=jsObject;
			}

			public dynamic Native => _jsObject;
		}

		internal class NativeMouseEventArgs:MouseEventArgs, INativeEventArgs
		{
			readonly NativeJsObject _jsObject;
			internal NativeMouseEventArgs(MouseEventArgs src,NativeJsObject jsObject)
			{
				CopyProps(src,this);
				_jsObject=jsObject;
			}

			public dynamic Native => _jsObject;
		}

		internal class NativeChangeEventArgs:ChangeEventArgs, INativeEventArgs
		{
			readonly NativeJsObject _jsObject;
			internal NativeChangeEventArgs(ChangeEventArgs src,NativeJsObject jsObject)
			{
				CopyProps(src,this);
				_jsObject=jsObject;
			}

			public dynamic Native => _jsObject;
		}

		internal class NativeClipboardEventArgs:ClipboardEventArgs, INativeEventArgs
		{
			readonly NativeJsObject _jsObject;
			internal NativeClipboardEventArgs(ClipboardEventArgs src,NativeJsObject jsObject)
			{
				CopyProps(src,this);
				_jsObject=jsObject;
			}

			public dynamic Native => _jsObject;
		}

		internal class NativeErrorEventArgs:ErrorEventArgs, INativeEventArgs
		{
			readonly NativeJsObject _jsObject;
			internal NativeErrorEventArgs(ErrorEventArgs src,NativeJsObject jsObject)
			{
				CopyProps(src,this);
				_jsObject=jsObject;
			}

			public dynamic Native => _jsObject;
		}

		internal class NativeFocusEventArgs:FocusEventArgs, INativeEventArgs
		{
			readonly NativeJsObject _jsObject;
			internal NativeFocusEventArgs(FocusEventArgs src,NativeJsObject jsObject)
			{
				CopyProps(src,this);
				_jsObject=jsObject;
			}

			public dynamic Native => _jsObject;
		}

		internal class NativeKeyboardEventArgs:KeyboardEventArgs, INativeEventArgs
		{
			readonly NativeJsObject _jsObject;
			internal NativeKeyboardEventArgs(KeyboardEventArgs src,NativeJsObject jsObject)
			{
				CopyProps(src,this);
				_jsObject=jsObject;
			}

			public dynamic Native => _jsObject;
		}

		internal class NativePointerEventArgs:PointerEventArgs, INativeEventArgs
		{
			readonly NativeJsObject _jsObject;
			internal NativePointerEventArgs(PointerEventArgs src,NativeJsObject jsObject)
			{
				CopyProps(src,this);
				_jsObject=jsObject;
			}

			public dynamic Native => _jsObject;
		}

		internal class NativeProgressEventArgs:ProgressEventArgs, INativeEventArgs
		{
			readonly NativeJsObject _jsObject;
			internal NativeProgressEventArgs(ProgressEventArgs src,NativeJsObject jsObject)
			{
				CopyProps(src,this);
				_jsObject=jsObject;
			}

			public dynamic Native => _jsObject;
		}

		internal class NativeTouchEventArgs:TouchEventArgs, INativeEventArgs
		{
			readonly NativeJsObject _jsObject;
			internal NativeTouchEventArgs(TouchEventArgs src,NativeJsObject jsObject)
			{
				CopyProps(src,this);
				_jsObject=jsObject;
			}

			public dynamic Native => _jsObject;
		}

		internal class NativeEventArgs:EventArgs, INativeEventArgs
		{
			readonly NativeJsObject _jsObject;
			internal NativeEventArgs(EventArgs src,NativeJsObject jsObject)
			{
				CopyProps(src,this);
				_jsObject=jsObject;
			}

			public dynamic Native => _jsObject;
		}

		internal class NativeWheelEventArgs:WheelEventArgs, INativeEventArgs
		{
			readonly NativeJsObject _jsObject;
			internal NativeWheelEventArgs(WheelEventArgs src,NativeJsObject jsObject)
			{
				CopyProps(src,this);
				_jsObject=jsObject;
			}

			public dynamic Native => _jsObject;
		}

		internal static void CopyProps<T>(T source,T dest)
		{
			foreach (PropertyInfo pi in typeof(T).GetProperties())
				pi.SetValue(dest,pi.GetValue(source));
		}
		#endregion Native event args
	}
}

namespace Microsoft.AspNetCore.Components.Web
{
	public static class BlazorEventArgsExtensions
	{
		public static dynamic GetNative(this EventArgs eventArgs)
			=> eventArgs is BlazorNativeJs.WebAssemblyEventDispatcher.INativeEventArgs nativeEventArgs
				? nativeEventArgs.Native
				: null;
	}
}