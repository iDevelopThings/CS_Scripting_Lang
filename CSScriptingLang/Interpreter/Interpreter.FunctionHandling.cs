using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter;

public partial class Interpreter
{
    
    /*private (Value fn, Value inst) TryGetFunctionValue(CallExpression node, ExecContext ctx) {
        if (node.Variable != null) {
            if (node.Variable is InlineFunctionDeclaration inlineFn) {
                throw new NotImplementedException("Inline function execution not implemented");
                // return Value.Function(inlineFn);
            }

            var variableResult = Execute(node.Variable, ctx);
            var variable       = variableResult.Get<Value>(value => value.Is.Function); 
            var obj            = variableResult.Get<Value>(value => !value.Is.Function);
            if (variable is {Is.Function: true} fn) {
                return (fn, obj);
            }
        }

        VariableSymbol symbol = null;

        if (!ctx.Functions.Get(node.Name, out var function)) {
            if (!ctx.Variables.Get(node.Name, out symbol)) {
                LogError(node, $"Function '{node.Name}' not found");
                return (null, null);
            }

            if (symbol.Val is {Is.Function: true} fn) {
                return (fn, null);
            }

            LogError(node, $"Symbol '{node.Name}' is not a function");
            return (null, null);

        }

        if (ctx.Variables.Get(node.Name, out symbol)) {
            return (symbol.Val, null);
        }

        throw new Exception("Failed to get function value");
        // return Value.Function(function);
    }*/

    /*private (InlineFunctionDeclaration, Value) TryGetFunction(CallExpression node, ExecContext ctx) {
        if (node.Variable != null) {
            if (node.Variable is InlineFunctionDeclaration inlineFn) {
                return (inlineFn, null);
            }

            var variableResult = Execute(node.Variable, ctx);
            var variable       = variableResult.Get<Value>();
            if (variable is Value fn) {
                return (fn.Value, fn.Outer);
            }
        }

        if (!ctx.Functions.Get(node.Name, out var function)) {
            if (!ctx.Variables.Get(node.Name, out var symbol)) {
                LogError(node, $"Function '{node.Name}' not found");
                return (null, null);
            }

            if (symbol.Value is Value fn) {
                return (fn.Value, fn.Outer);
            }

            LogError(node, $"Symbol '{node.Name}' is not a function");
            return (null, null);

        }

        return (function, null);
    }*/

    /*
    private List<(VariableSymbol argSymbol, Value argValue)> ResolveFunctionArgumentValues(CallExpression node, FunctionExecContext fnContext) {
        var declaration = fnContext.Function;

        var allArgs = node.Arguments.ExpressionNodes
           .Select(arg => {
                var res    = Execute(arg, fnContext);
                var symbol = res.Get<VariableSymbol>();
                var value  = res.Get<Value>();

                if (value != null) {
                    return (symbol, value);
                }

                /*if (ExecuteGetValueAndSymbol(arg, fnContext, out var argSymbol, out var argValue)) {
                    return (argSymbol, argValue);
                }#1#

                LogError(arg, $"Failed to get value from argument node");
                return (null, null);
            })
           .ToList();

        if (!declaration.IsStatic) {
            if (fnContext.ThisSymbol != null) {
                allArgs.Insert(0, (fnContext.ThisSymbol, fnContext.This));
            }
        }

        return allArgs;
    }
    private void ResolveFunctionArguments(List<Value> args, FunctionExecContext fnContext) {
        var declaration = fnContext.Function;

        var varArgs = new List<Value>();

        for (var i = 0; i < args.Count; i++) {
            if (!declaration.Parameters.Get(i, out var argDef)) {
                throw new Exception("Argument definition not found");
            }

            var val = args[i];

            if (argDef.IsVariadic) {
                // If varArgs, collect remaining arguments

                fnContext.Params.Add(fnContext.Scope.SetValue(argDef.Name, Value.Array(varArgs)));

                var remainingArgs = args.Skip(i).ToList();
                varArgs.AddRange(remainingArgs);

                break;
            }

            fnContext.Params.Add(fnContext.Scope.Set(argDef.Name, val));
        }
    }
    private void ResolveFunctionArguments(List<(VariableSymbol argSymbol, Value argValue)> args, FunctionExecContext fnContext) {
        var declaration = fnContext.Function;

        var varArgs     = new List<Value>();
        var indexOffset = 0;
        for (var i = 0; i < args.Count; i++) {
            var (argSymbol, argValue) = args[i];
            var name = argSymbol?.Name;

            var argDeclExists = declaration.Parameters.Get(i, out var argDef);

            if (!declaration.IsStatic && i == 0 && fnContext.ThisSymbol != null) {
                fnContext.Params.Add(fnContext.ThisSymbol);
                if (argDeclExists && argDef.Name == "this") {
                    indexOffset++;
                    continue;
                }

                continue;
            }

            name = argDeclExists switch {
                true  => argDef.Name,
                false => throw new Exception("Argument definition not found")
            };

            if (argDef.IsVariadic) {
                // If varArgs, collect remaining arguments
                var varArgsParamSymbol = fnContext.Scope.SetValue(name, Value.Array(varArgs));

                fnContext.Params.Add(varArgsParamSymbol);

                var remainingArgs = args.Skip(i).Select(a => a.argValue).ToList();
                varArgs.AddRange(remainingArgs);

                break;
            }

            VariableSymbol symbol = null;

            var callingArg = fnContext.CurrentCallFrame.ReturnExpression?.Arguments.Nodes[i - indexOffset];
            var value      = argSymbol?.Val ?? argValue;
            /*if (argSymbol != null && callingArg is VariableNode {IsRef: false}) {
                value  = argSymbol.Val?.GetOrClone();
                symbol = fnContext.Scope.Set(name, value);
            } else if (argSymbol != null && callingArg is VariableNode {IsRef: true}) {
                value  = argSymbol.Val?.GetOrClone();
                symbol = fnContext.Scope.Set(name, value);
                symbol.IsReference(argSymbol);
            } else {
                symbol = fnContext.Scope.Set(name, value);
            }#1#

            symbol = fnContext.Scope.Set(name, value);
            if (symbol != null)
                fnContext.Params.Add(symbol);
        }

    }

    public ExecResult ExecuteFunctionCall(
        FunctionExecContext fnContext,
        Value[]             args = null,
        CallExpression    node = null
    ) {
        var result      = NewResult();
        var declaration = fnContext.Function;


        var executionTimer = new TimedScope(InterpreterConfig.CanDebugFunction(declaration.Name));

        fnContext.PushScope();
        fnContext.PushFrame(
            returnExpression: node,
            name: declaration.Name
        );

        if (fnContext.This != null) {
            fnContext.ThisSymbol = fnContext.Scope.Set("this", fnContext.This);
        }

        if (args != null) {
            ResolveFunctionArguments(args.ToList(), fnContext);
        } else if (node != null) {
            var fnArgs = ResolveFunctionArgumentValues(node, fnContext);
            ResolveFunctionArguments(fnArgs, fnContext);
        }

        if (InterpreterConfig.CanDebugFunction(declaration.Name)) {
            Logger.Debug($"Executing function call(IsNative={declaration.IsNative}): {declaration.Name}");
            Logger.Debug($"Param Count: {fnContext.Params.Count}");

            foreach (var p in fnContext.Params) {
                if (p.Name == "this")
                    continue;

                Logger.Debug($"\t - {p.Name} = {p.Val.Inspect()}");
            }
        }

        if (declaration.IsNative) {
            declaration.NativeFunction(fnContext);
            result += fnContext.ReturnValues;
        } else {
            ExecuteBlock(declaration.Body, fnContext, false);
            result += fnContext.ReturnValues;

        }

        if (InterpreterConfig.CanDebugFunction(declaration.Name)) {
            executionTimer.Stop();
            executionTimer.Print(Logger, $"{declaration.Name} - Execution Time");

            Logger.Debug($"Return Values: {fnContext.ReturnValues.Count}");

            foreach (var v in fnContext.ReturnValues) {
                Logger.Debug($"\t - {v.Inspect()}");
            }

        }

        fnContext.PopFrame();
        fnContext.PopScope();

        return result;
    }

    private ExecResult Execute(CallExpression node, ExecContext ctx) {
        var result = NewResult();

        node.Execute(ctx);
        using var _ = ctx.SetCaller(node);

        var (fn, inst) = TryGetFunctionValue(node, ctx);
        if (fn == null) {
            LogError(node, $"Function '{node.Name}' not found");
            return result;
        }

        var args = node.Arguments.Execute(ctx).Select(v => v.Value).ToArray();

        var returnValue = ctx.Call(
            fn,
            inst,
            args
        );

        result += returnValue;

        return result;
    }
    
    
    private ExecResult Execute(AwaitStatement node, ExecContext ctx) {
        var result = NewResult();

        return result;
    }*/
}