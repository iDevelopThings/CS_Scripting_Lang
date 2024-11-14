using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter;

public partial class Interpreter
{

    /*private ExecResult Execute(MemberAccessExpression node, ExecContext ctx) {
        var result = NewResult();

        var objResult = Execute(node.Object, ctx);

        // Temporarily switches our context's module to the module of the declaration
        using var _ = ctx.SwitchModule(objResult.Get<Module>());

        // ReSharper disable once UnusedVariable
        var (objSymbol, obj) = objResult.Get<VariableSymbol, Value>();
        if (obj != null) {
            return ctx.AddMemberAccessReference(ref result, obj, node.Identifier);
        }

        if (ctx.GetVariable(node.Identifier, out var s)) {
            return ctx.AddVariableAccessReference(ref result, s);
        }


        if (objResult.TryGet<Module>(out var module)) {
            if (node.Parent is CallExpression) {
                if (module.Declarations.FunctionsByName.TryGetValue(node.Identifier, out var fn)) {
                    result += fn;
                    return result;

                }
                // if (module.GetFunction(node.Name, out var function) && ctx.Functions.Get(node.Name, out var rtFunction) && function == rtFunction) {
                //     result += ValueFactory.Function.Make(rtFunction);
                //     return result;
                // }

                LogError(node.Parent, $"Failed to find function '{node.GetPath()}'.");
                return result;
            }

            if (module.Declarations.VariablesByName.TryGetValue(node.Identifier, out var variable) && ctx.Variables.Get(node.Identifier, out var symbol) && variable == symbol) {
                result += variable;
                result += variable.Val;
                return result;
            }

            // if (module.GetVariable(node.Name, out _) && ctx.Symbols.Get(node.Name, out var symbol)) {
            //     result += symbol;
            //     result += symbol.Val;
            //     return result;
            // }
        }

        if (node.Parent is CallExpression) {
            LogError(node.Parent, $"Failed to find function '{node.GetPath()}'.");
            return result;
        }

        LogError(node, $"Failed to get value from property access node (path='{node.GetPath()}')");
        return result;
    }*/
    /*private ExecResult Execute(IndexAccessExpression node, ExecContext ctx) {
        var result = NewResult();

        var objResult = Execute(node.Object, ctx);

        var (variable, obj) = objResult.Get<VariableSymbol, Value>();
        if (variable == null && obj == null) {
            LogError(node.Object, $"Failed to get value from object node");
            return result;
        }

        var indexResult = Execute(node.Index, ctx);
        var (indexSymbol, index) = indexResult.Get<VariableSymbol, Value>();
        if (indexSymbol == null && index == null) {
            LogError(node.Index, $"Failed to get value from index node");
            return result;
        }

        return ctx.AddIndexAccessReference(ref result, obj, index);

        /*
        Value property = obj[index];

        if (property != null) {
            result += property;
        }

        return result;#1#
    }
    private ExecResult Execute(ObjectLiteralExpression node, ExecContext ctx) {
        var result = NewResult();

        var obj = Value.Object(node.Properties.Select(p => {
            var value = Execute(p.Value, ctx).Get<Value>();
            return (p.Name, value);
        }));


        result += obj;


        /*
        node.ObjectType ??= TypeTable.RegisterObjectType(
            "",
            null
        );

        var rtObj = ValueFactory.Object.Make(node.ObjectType);

        foreach (var property in node.Properties) {
            var rtValue = Execute(property.Value, ctx).Get<Value>();
            if (rtValue == null) {
                LogError(property.Value, $"Failed to get value from object property");
                continue;
            }

            rtObj.SetField(property.Name, rtValue);
        }

        result += rtObj;
        #1#

        return result;
    }
    public ExecResult Execute(ArrayLiteralExpression node, ExecContext ctx) {
        var result = NewResult();

        var elements = node.Elements.Select(e => {
            var value = Execute(e, ctx).Get<Value>();
            return value;
        });

        var arr = Value.Array(elements);

        result += arr;

        /*
        var arr = ValueFactory.Array.Make();

        foreach (var element in node.Elements) {
            var value = Execute(element, ctx);

            arr.Add(value.Get<Value>());
        }

        result += arr;
        #1#

        return result;
    }
    private ExecResult ExecuteLiteral(LiteralValueExpression node, ExecContext ctx) {
        var result = NewResult();

        switch (node) {
            case ObjectLiteralExpression obj:
                return Execute(obj, ctx);
            case ArrayLiteralExpression arr:
                return Execute(arr, ctx);

            case LiteralNumberExpression:
            case BooleanExpression:
            case StringExpression:
                result += node.Execute(ctx);
                return result;

            default:
                throw new NotImplementedException($"Unhandled literal node type: {node.GetType().Name}");

        }
    }
    private ExecResult Execute(VariableDeclarationNode node, ExecContext ctx) {
        var result = NewResult();

        foreach (var initializer in node.Initializers) {

            if (ctx.Variables.Get(initializer.Name, out var symbol)) {
                if (!symbol.IsBaseDeclaration) {
                    LogError(initializer, $"Variable '{initializer.Name}' already declared");
                    continue;
                }
            }


            if (initializer.Val is LiteralValueExpression literal) {
                var literalResult = literal.Execute(ctx);
                ctx.Variables.Set(initializer.Name, literalResult.Value);

                continue;
            }

            var valueResult = ctx.EvaluateAsRValue(() => Execute(initializer.Val, ctx));

            ctx.Variables.Set(
                initializer.Name,
                valueResult.TryGetLast<Value>(out var value)
                    ? value.GetOrClone()
                    : Value.Null()
            );


        }

        /*
        var symbols = node.Variables.Select(v => ctx.Symbols.Declare(v.Name)).ToList();

        if (node.Value is TupleListDeclarationNode tupleList) {
            for (var i = 0; i < tupleList.Nodes.Count; i++) {
                symbols[i].Value = ExecuteGetValue(tupleList.Nodes[i], ctx);
            }
        } else {
            // ctx.Symbols.Declare(node.VariableName);
            Execute(node.Assignment, ctx);
        }
        #1#


        return result;
    }
    private ExecResult Execute(ForRangeStatement node, ExecContext ctx) {
        var result = NewResult();

        /*
        using var _ = ctx.UsingScope();

        var rangeResult = Execute(node.Range, ctx);


        var rangeMin   = (Value) rangeResult[0];
        var rangeValue = (Value) rangeResult[1];

        if (rangeMin == null || rangeValue == null) {
            LogError(node, "Failed to get range values");
            return result;
        }

        if (node.Indexers?[0] is not VariableInitializerNode variableInitializerNode) {
            LogError(node, "Invalid loop indexer declaration");
            return result;
        }

        if (variableInitializerNode.Variable is null) {
            LogError(node, "Invalid loop indexer declaration");
            return result;
        }

        VariableInitializerNode loopElementDecl = null;
        if (node.Indexers.Nodes.Count > 1) {
            if (node.Indexers[1] is not VariableInitializerNode elementDecl) {
                LogError(node, "Invalid loop element declaration");
                return result;
            }

            loopElementDecl = elementDecl;
        }

        /*
        Value GetElement(int idx) {
            if (rangeArray != null) {
                return rangeArray[idx];
            }

            if (rangeObj != null) {
                // rangeobj has a `fields` dictionary, we need to get the field at the index
                return rangeObj.Fields.ElementAtOrDefault(idx).Value;
            }

            return null;
        }
        #2#

        if (rangeValue is not IIterable rangeIterable) {
            LogError(node, "Invalid range value");
            return result;
        }

        var iterator = rangeIterable.GetIterator();

        var            loopIndexVar   = ctx.Variables.Declare(variableInitializerNode.Name);
        VariableSymbol loopElementVar = null;

        if (loopElementDecl != null) {
            loopElementVar = ctx.Variables.Declare(loopElementDecl.Name);
        }

        while (iterator.MoveNext()) {
            loopIndexVar.Val = iterator.CurrentIndex;

            if (loopElementVar != null) {
                loopElementVar.Val = iterator.Current; // The value from the iterator
            }

            ExecuteBlock(node.Body, ctx);

        }

        /*
        if (rangeValue is ValueArray rangeAsArr) {
            rangeArray = rangeAsArr;
            rangeMax   = rangeArray.Length;
        } else if (rangeValue is Number num) {
            rangeMax = (int) num.GetUntypedValue();
        } else if (rangeValue is Object obj) {
            rangeObj = obj;
            rangeMax = obj.Fields.Count;
        } else {
            LogError(node, "Invalid range value");
            return result;
        }
        #2#

        /*
        var rangeMaxValue = ValueFactory.Make(rangeMax);

        var loopIndexVar = ctx.Variables.Declare(variableInitializerNode.Name);
        loopIndexVar.Value = ValueFactory.Make(rangeMin.GetUntypedValue());

        VariableSymbol loopElementVar = null;
        if (loopElementDecl != null) {
            loopElementVar       = ctx.Variables.Declare(loopElementDecl.Name);
            loopElementVar.Value = GetElement(0);
        }
        #2#

        /*while (true) {
            var condition = loopIndexVar.Value.Operator_LessThan(rangeMaxValue);
            if (!condition!.ToBool())
                break;

            ExecuteBlock(node.Body, ctx);

            loopIndexVar.Value.Operator(OperatorType.Increment, ValueFactory.Number_Int32.Make(1));

            if (loopElementVar != null) {
                var idx = loopIndexVar.Value.Value<int>();
                if (rangeArray != null) {
                    if (idx >= rangeArray.Length) {
                        break;
                    }

                    loopElementVar.Value = GetElement(idx);
                }

                if (rangeObj != null) {
                    if (idx >= rangeObj.Fields.Count) {
                        break;
                    }

                    loopElementVar.Value = GetElement(idx);
                }
            }
        }#2#
        #1#

        return result;
    }*/
    /*private ExecResult Execute(VariableNode node, ExecContext ctx) {
        var result = NewResult();

        if (ModuleResolver.TryGet(node.Name, out var module)) {
            result += module;

            return result;
        }

        if (ctx.Variables.Get(node.Name, out var variable)) {
            return ctx.AddVariableAccessReference(ref result, variable);
        }

        LogError(node, $"Variable '{node.Name}' not found");

        return result;
    }*/
}