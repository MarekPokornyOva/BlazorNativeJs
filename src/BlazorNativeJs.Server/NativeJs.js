var JsObjects = function()
{
	var objects = [];

	//https://stackoverflow.com/questions/105034/create-guid-uuid-in-javascript
	function generateUUID()
	{
		var d = new Date().getTime();//Timestamp
		var d2 = (performance && performance.now && (performance.now() * 1000)) || 0;//Time in microseconds since page-load or 0 if unsupported
		return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c)
		{
			var r = Math.random() * 16;//random number between 0 and 16
			if (d > 0) {//Use timestamp until depleted
				r = (d + r) % 16 | 0;
				d = Math.floor(d / 16);
			} else {//Use microseconds since page-load if supported
				r = (d2 + r) % 16 | 0;
				d2 = Math.floor(d2 / 16);
			}
			return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
		});
	}

	return {
		set: function(object)
		{
			var id = generateUUID();
			//TODO: check the id doesn't exist in objects variable yet.
			objects[id] = object;
			return id;
		},
		get: function (id)
		{
			return objects[id];
		},
		remove: function (id)
		{
			objects[id] = null;
		}
	};
}();

var NativeJs = function ()
{
	function processResult(res)
	{
		res = res === null ? null :
			res === Object(res) ? '__ref::' + JsObjects.set(res) :
				res; //JSON.stringify(res);

		//https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/typeof
		//https://javascriptweblog.wordpress.com/2011/08/08/fixing-the-javascript-typeof-operator/

		return res;
	}

	function processArg(arg)
	{
		if (arg)
		{
			if (arg.substr && arg.substr(0, 7) === "__ref::")
				return JsObjects.get(arg.substr(7));
			if (arg === Object(arg) && arg.type)
				return handleExpression(arg);
		}
		return arg;
	}

	function processArgs(args)
	{
		if (args) {
			var res = [];
			args.map(function (val) { res.push(processArg(val)); });
			return res;
		}
		else
			return args;
	}

	function handleGetMemberExpression(expression)
	{
		var obj = handleExpression(expression.object);
		return obj[expression.member];
	}

	function handleSetMemberExpression(expression)
	{
		handleExpression(expression.object)[expression.member] = processArg(expression.value);
	}

	function handleCallExpression(expression)
	{
		var obj = handleExpression(expression.object);
		args = processArgs(expression.args);
		var memberName = expression.member;
		var res = memberName
			? obj[memberName].apply(obj, args)
			: obj.apply(null, args);
		return res;
	}

	function handleGetIndexExpression(expression)
	{
		var obj = handleExpression(expression.object);
		var args = processArgs(expression.indexes);
		for (var a = 0; a < args.length; a++)
			obj = obj[args[a]];
		return obj;
	}

	function handleSetIndexExpression(expression)
	{
		var obj = handleExpression(expression.object);
		var args = processArgs(expression.indexes);
		for (var a = 0; a < args.length - 1; a++)
			obj = obj[args[a]];
		obj[args.length - 1] = processArg(expression.value);
	}

	function handleRawExpression(expression)
	{
		return expression.expr === "window" ? window : eval(expression.expr);
	}

	function handleGetNativeExpression(expression)
	{
		var res = JsObjects.get(expression.id);
		return res;
	}

	function handleExpression(expression)
	{
		var type = expression.type;
		if (type === "expr")
			return handleRawExpression(expression);
		else if (type === "getmember")
			return handleGetMemberExpression(expression);
		else if (type === "call")
			return handleCallExpression(expression)
		else if (type === "getindex")
			return handleGetIndexExpression(expression);
		else if (type === "native")
			return handleGetNativeExpression(expression);
		else if (type === "setmember") {
			handleSetMemberExpression(expression);
			return;
		}
		else if (type === "setindex") {
			handleSetIndexExpression(expression);
			return;
		}
		throw new Error(expression.type + " is not supported.");
	}

	return {
		getMemberValue: function (objRef, memberName)
		{
			var res = JsObjects.get(objRef)[memberName];
			return processResult(res);
		},
		setMemberValue: function (objRef, memberName, value)
		{
			value = processArg(value);
			JsObjects.get(objRef)[memberName] = value;
		},
		callMember: function (objRef, functionName, args)
		{
			args = processArgs(args);
			var obj = JsObjects.get(objRef);
			var res = obj[functionName].apply(obj, args);
			return processResult(res);
		},
		call: function (objRef, args)
		{
			args = processArgs(args);
			var res = JsObjects.get(objRef).apply(null, args);
			return processResult(res);
		},
		handleEventArgs: function (event,dotnetEvent)
		{
			dotnetEvent.data['__ref'] = JsObjects.set(event);
			return dotnetEvent;
		},
		handleFuncArgs: function (args)
		{
			if (args) {
				res = [];
				for (var a = 0; a < args.length; a++)
					res.push(processResult(args[a]));
				return res;
			}
			else
				return args;
		},
		getIndex: function (objRef, _, indexes)
		{
			var res = JsObjects.get(objRef);
			indexes.map(function (x) { res = res[x]; });
			return processResult(res);
		},
		execExpr: function (json)
		{
			var expression = JSON.parse(json);
			var res = handleExpression(expression);
			return processResult(res);
		}
	};
}();

window.addEventListener("load", function ()
{
	//Kidnap DotNet.invokeMethodAsync function to be able to inject custom handler.
	var DotNet_invokeMethodAsync = DotNet.invokeMethodAsync;
	DotNet.invokeMethodAsync = function ()
	{
		if (arguments[1] === "DispatchEvent" && arguments[0] === "Microsoft.AspNetCore.Blazor")
			arguments[0] = "BlazorNativeJs"; //Call our handler instead of default one. Default will be called from our.
		return DotNet_invokeMethodAsync.apply(null, arguments);
	};
});
