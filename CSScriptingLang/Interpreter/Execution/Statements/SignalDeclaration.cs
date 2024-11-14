using System.Runtime.InteropServices;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using SharpX;

namespace CSScriptingLang.Interpreter.Execution.Statements;

public class Signal
{
    public string       Name           { get; set; }
    public List<string> ParameterTypes { get; set; }

    public static int IdCounter = 0;

    public List<Listener> Listeners = new();

    // Mainly used for testing
    public List<Action<Value[]>> InternalListeners = new();

    public struct Listener
    {
        // a wrapped function, using a `ValueReference` to store the function
        public Value Fn { get; set; }
        public int   Id { get; set; }

        public Listener(Value fn) {
            Fn = fn;
            Id = IdCounter++;
        }
    }


    public void AddListener(Value listenerRef) {
        if (listenerRef.Type != RTVT.ValueReference)
            throw new InterpreterRuntimeException($"Cannot bind a listener to a signal without a reference value fn");
        Listeners.Add(new Listener(listenerRef));
    }
    public void AddListener(Action<Value[]> listenerRef) {
        InternalListeners.Add(listenerRef);
    }
    public void RemoveListener(Value listenerRef) {
        if (listenerRef.Type != RTVT.ValueReference)
            throw new InterpreterRuntimeException($"Cannot remove a listener from a signal without a reference value fn");

        Listeners.RemoveAll(l => l.Fn.Equals(listenerRef));
    }
    public void RemoveListener(Action<Value[]> listenerRef) {
        InternalListeners.Remove(listenerRef);
    }
    public void Emit(FunctionExecContext ctx, Value[] args) {
        foreach (var internalListener in InternalListeners) {
            internalListener?.Invoke(args);
        }
        
        foreach (var listener in Listeners) {
            var fn     = listener.Fn;
            var refVal = fn.As.ValueReference();

            var result = ctx.Call(refVal.Value, refVal.Object, args);

            Console.WriteLine($"Emitting signal {Name} to listener {listener.Id}");
        }
    }
}

[ASTNode]
public partial class SignalDeclaration : Statement, ITopLevelDeclarationNode
{
    public SignalPrototype Prototype { get; set; } = null;

    public DeclarationContext DeclarationContext { get; set; } = new();

    [VisitableNodeProperty]
    public ArgumentListDeclarationNode Parameters { get; set; } = new();

    public string Name { get; set; }

    public SignalDeclaration() { }
    public SignalDeclaration(ArgumentListDeclarationNode parameters) {
        Parameters = parameters;
    }

    public SignalPrototype HandleDeclaration(ExecContext ctx) {
        if (Prototype == null) {
            Prototype = TypesTable.DeclareSignal(ctx, this);
        }
        return Prototype;
    }

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        HandleDeclaration(ctx);

        var type = Prototype.ValueType;

        var ctor = type.GetConstructorFn();
        var val  = ctx.Call(ctor, type);
        // var val  = ctor.Call(ctx, type);

        var variable = ctx.Variables.Declare(Prototype.Symbol.Name, val);

        return ctx.VariableAccessReference(variable).ToMaybe();
    }


}