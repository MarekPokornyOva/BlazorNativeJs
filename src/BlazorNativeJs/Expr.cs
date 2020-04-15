#region using
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;
using System.Text.Json;
#endregion using

namespace BlazorNativeJs
{
	abstract class Expr:DynamicObject
	{
		internal abstract void Visit(Utf8JsonWriter writer);

		public override bool TryConvert(ConvertBinder binder,out object result)
		{
			result=Eval();
			if ((result is NativeJs.DynamicJsonValue djv)&&(binder.Type!=typeof(object)))
				result=djv.Cast(binder.Type);
			return true;
		}

		internal object Eval()
		{
			StringBuilder sb = new StringBuilder();
			Utf8JsonWriter writer = new Utf8JsonWriter(new StringBuilderStream(sb));
			this.Visit(writer);
			writer.Flush();
			object result = NativeJs.ExecExpr(sb.ToString(),true);
			return result;
		}

		public override bool TryGetMember(GetMemberBinder binder,out object result)
		{
			result=new GetMemberExpr(this,binder.Name);
			return true;
		}

		public override bool TryInvokeMember(InvokeMemberBinder binder,object[] args,out object result)
		{
			result=new CallExpr(this,binder.Name,args);
			return true;
		}

		public override bool TryInvoke(InvokeBinder binder,object[] args,out object result)
		{
			result=new CallExpr(this,null,args);
			return true;
		}

		public override bool TryGetIndex(GetIndexBinder binder,object[] indexes,out object result)
		{
			result=new GetIndexExpr(this,indexes);
			return true;
		}

		public override bool TrySetMember(SetMemberBinder binder,object value)
		{
			StringBuilder sb = new StringBuilder();
			Utf8JsonWriter writer = new Utf8JsonWriter(new StringBuilderStream(sb));
			writer.WriteStartObject();
			writer.WriteString("type","setmember");
			writer.WritePropertyName("object");
			this.Visit(writer);
			writer.WriteString("member",binder.Name);
			writer.WritePropertyName("value");
			WriteValue(writer,value);
			writer.WriteEndObject();
			writer.Flush();
			NativeJs.ExecExpr(sb.ToString(),false);
			return true;
		}

		public override bool TrySetIndex(SetIndexBinder binder,object[] indexes,object value)
		{
			StringBuilder sb = new StringBuilder();
			Utf8JsonWriter writer = new Utf8JsonWriter(new StringBuilderStream(sb));

			writer.WriteStartObject();
			writer.WriteString("type","setindex");
			writer.WritePropertyName("object");
			this.Visit(writer);
			WriteArray(writer,"indexes",indexes);
			writer.WritePropertyName("value");
			WriteValue(writer,value);
			writer.WriteEndObject();
			writer.Flush();
			NativeJs.ExecExpr(sb.ToString(),false);
			return true;
		}

		protected void WriteValue(Utf8JsonWriter writer,object value)
		{
			value=NativeJs.ResolveJsObject(value);
			if (value==null)
				writer.WriteNullValue();
			else
			{
				switch (value)
				{
					case bool @bool:
						writer.WriteBooleanValue(@bool);
						break;
					case string str:
						writer.WriteStringValue(str);
						break;
					case DateTime dt:
						writer.WriteStringValue(dt);
						break;
					case byte @byte:
						writer.WriteNumberValue(@byte);
						break;
					case decimal dec:
						writer.WriteNumberValue(dec);
						break;
					case double dbl:
						writer.WriteNumberValue(dbl);
						break;
					case short int16:
						writer.WriteNumberValue(int16);
						break;
					case int int32:
						writer.WriteNumberValue(int32);
						break;
					case long int64:
						writer.WriteNumberValue(int64);
						break;
					case sbyte @sbyte:
						writer.WriteNumberValue(@sbyte);
						break;
					case float flt:
						writer.WriteNumberValue(flt);
						break;
					case ushort uint16:
						writer.WriteNumberValue(uint16);
						break;
					case uint uint32:
						writer.WriteNumberValue(uint32);
						break;
					case ulong uint64:
						writer.WriteNumberValue(uint64);
						break;
					case Expr expr:
						expr.Visit(writer);
						break;
					default:
						throw new NotSupportedException($"{value.GetType()} is not supported on writing JS value");
				}
			}
		}

		protected void WriteArray(Utf8JsonWriter writer,string propName,IEnumerable<object> values)
		{
			writer.WriteStartArray(propName);
			foreach (object arg in values)
				WriteValue(writer,arg);
			writer.WriteEndArray();
		}

		#region StringBuilderStream
		class StringBuilderStream:Stream
		{
			readonly StringBuilder _sb;
			internal StringBuilderStream(StringBuilder sb)
				=> _sb=sb;

			public override bool CanRead => throw new NotSupportedException();

			public override bool CanSeek => throw new NotSupportedException();

			public override bool CanWrite => true;

			public override long Length => throw new NotSupportedException();

			public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

			public override void Flush()
			{ }

			public override int Read(byte[] buffer,int offset,int count) => throw new NotSupportedException();

			public override long Seek(long offset,SeekOrigin origin) => throw new NotSupportedException();

			public override void SetLength(long value) => throw new NotSupportedException();

			readonly static Encoding _encoding = Encoding.UTF8;
			public override void Write(byte[] buffer,int offset,int count)
				=> _sb.Append(_encoding.GetString(buffer,offset,count));
		}
		#endregion StringBuilderStream
	}

	sealed class GetMemberExpr:Expr
	{
		readonly Expr _parent;
		readonly string _memberName;
		public GetMemberExpr(Expr parent,string memberName)
		{
			_parent=parent;
			_memberName=memberName;
		}

		internal override void Visit(Utf8JsonWriter writer)
		{
			writer.WriteStartObject();
			writer.WriteString("type","getmember");
			writer.WritePropertyName("object");
			_parent.Visit(writer);
			writer.WriteString("member",_memberName);
			writer.WriteEndObject();
		}
	}

	sealed class CallExpr:Expr
	{
		readonly Expr _parent;
		readonly string _memberName;
		readonly object[] _args;
		public CallExpr(Expr parent,string memberName,object[] args)
		{
			_parent=parent;
			_memberName=memberName;
			_args=args;
		}

		internal override void Visit(Utf8JsonWriter writer)
		{
			writer.WriteStartObject();
			writer.WriteString("type","call");
			writer.WritePropertyName("object");
			_parent.Visit(writer);
			writer.WriteString("member",_memberName);
			WriteArray(writer,"args",_args);
			writer.WriteEndObject();
		}
	}

	sealed class GetIndexExpr:Expr
	{
		readonly Expr _parent;
		readonly object[] _indexes;
		public GetIndexExpr(Expr parent,object[] indexes)
		{
			_parent=parent;
			_indexes=indexes;
		}

		internal override void Visit(Utf8JsonWriter writer)
		{
			writer.WriteStartObject();
			writer.WriteString("type","getindex");
			writer.WritePropertyName("object");
			_parent.Visit(writer);
			WriteArray(writer,"indexes",_indexes);
			writer.WriteEndObject();
		}
	}

	sealed class NativeExpr:Expr
	{
		readonly Guid _id;
		public NativeExpr(Guid id)
			=> _id=id;

		internal override void Visit(Utf8JsonWriter writer)
		{
			writer.WriteStartObject();
			writer.WriteString("type","native");
			writer.WriteString("id",_id.ToString("D"));
			writer.WriteEndObject();
		}
	}

	sealed class RootExpr:Expr
	{
		internal override void Visit(Utf8JsonWriter writer)
		{
			throw new InvalidOperationException("Can't evaluate Root");
		}

		public override bool TryGetMember(GetMemberBinder binder,out object result)
		{
			result=new NativeJsExpr(binder.Name);
			return true;
		}
	}

	sealed class NativeJsExpr:Expr
	{
		readonly string _expression;
		public NativeJsExpr(string expression)
			=> _expression=expression;

		internal override void Visit(Utf8JsonWriter writer)
		{
			writer.WriteStartObject();
			writer.WriteString("type","expr");
			writer.WriteString("expr",_expression);
			writer.WriteEndObject();
		}
	}
}
