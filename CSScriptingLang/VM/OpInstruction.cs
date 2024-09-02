using System.Runtime.CompilerServices;
using CSScriptingLang.Utils;

namespace CSScriptingLang.VM;

public enum OpCode
{
    Nop,

    Load,  // Pushes a value onto the stack or var table depending on the operand
    Store, // Stores the top value on the stack in a variable
    Pop,   // Pops a value from the stack

    DeclareInlineFunction,       // Declares a function
    LoadTemporaryInlineFunction, // Loads a function
    LoadFunctionParameter,       // Loads a function parameter

    DeclareFunction, // Declares a function
    CallFunction,    // Calls a function
    CallNativeFunction, // Calls a native function

    BinaryOp, // Pops two values, performs a binary operation, and pushes the result
    UnaryOp,  // Pops a value, performs a unary operation, and pushes the result

    Return, // Returns from a function

    Jump,        // Jumps to a specific instruction index
    JumpIfFalse, // Jumps if the top of the stack is false
    JumpIfTrue,  // Jumps if the top of the stack is true

    BeginBlock, // Begin a new block scope
    EndBlock,   // End the current block scope

    LoopSetup,     // Sets up a loop
    LoopCondition, // Checks the loop condition
    LoopIteration, // Iterates the loop

    CreateObject,  // Creates an object
    LoadProperty,  // Loads a property from an object
    StoreProperty, // Stores a property in an object
    LoadIndex,     // Loads an index from an object/array

    Marker, // Marks a location in the code

    Halt, // Stops execution
}

public struct OpInstruction
{
    public OpCode OpCode;

    // Optional operand (e.g., constant value or variable name)
    public object Operand;

    // For debugging, not part of vm execution
    public int Index;

    // For debugging, not part of vm execution
    public string Caller;

    public OpInstruction(OpCode opCode, object operand = null, [CallerMemberName] string caller = "") {
        OpCode  = opCode;
        Operand = operand;
        Caller  = caller;
    }

    public string OperandString => Operand?.ToString() ?? "null";

    public override string ToString() {
        var str = OpCode.ToString();
        if (Operand != null)
            str += " " + Operand;
        return str;
    }
}