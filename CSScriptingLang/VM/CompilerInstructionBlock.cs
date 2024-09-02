using CSScriptingLang.Utils;
using CSScriptingLang.VM.Instructions;

namespace CSScriptingLang.VM;

public class CompilerInstructionBlock : PooledObject<CompilerInstructionBlock>
{
    private static Stack<CompilerInstructionBlock> Stack = new();

    public static CompilerInstructionBlock Global;

    public Caller                         _caller;
    public ByteCodeCompiler               _compiler;
    public CompilerInstructionBlock       _parent;
    public List<CompilerInstructionBlock> Children = new();

    public List<Instruction> Instructions =>
        _compiler.Instructions.GetRange(StartInstructionIndex, EndInstructionIndex - StartInstructionIndex);


    public int ChildrenStartingIndex => Children.Count == 0 ? StartInstructionIndex : Children.Min(c => c.StartInstructionIndex);
    public int ChildrenEndingIndex   => Children.Count == 0 ? EndInstructionIndex : Children.Max(c => c.EndInstructionIndex);

    public int MainStartInstructionIndex => ChildrenStartInstructionIndex == -1 ? StartInstructionIndex : ChildrenStartInstructionIndex;
    public List<Instruction> MainStartInstructions() => _compiler.Instructions.GetRange(
        StartInstructionIndex,
        _compiler.Instructions.Count - MainStartInstructionIndex
    );


    public int MainEndInstructionIndex => ChildrenEndInstructionIndex == -1 ? EndInstructionIndex : ChildrenEndInstructionIndex;

    public List<Instruction> MainEndInstructions() => MainEndInstructionIndex == EndInstructionIndex
        ? new()
        : _compiler.Instructions.GetRange(
            ChildrenEndInstructionIndex == -1 ? EndInstructionIndex : ChildrenEndInstructionIndex,
            _compiler.Instructions.Count - MainEndInstructionIndex
        );


    public int StartInstructionIndex;
    public int EndInstructionIndex;
    public int ChildrenStartInstructionIndex = -1;
    public int ChildrenEndInstructionIndex   = -1;

    public CompilerInstructionBlock() {
        if (Global == null) {
            Global = this;
            Stack.Push(this);
        }
    }

    public CompilerInstructionBlock(ByteCodeCompiler compiler, CompilerInstructionBlock parent = null) : this() {
        _compiler             = compiler;
        _parent               = parent;
        StartInstructionIndex = compiler.Instructions.Count;
    }

    public static CompilerInstructionBlock Child() {
        var block = Rent(Global._compiler, Global, Caller.GetFromFrame());
        Global.Children.Add(block);
        Global.ChildrenStartInstructionIndex = block.StartInstructionIndex;

        Global = block;

        return block;
    }

    public static CompilerInstructionBlock Rent(ByteCodeCompiler compiler, CompilerInstructionBlock parent, Caller caller) {
        var block = Rent();
        block._caller               = caller;
        block._compiler             = compiler;
        block._parent               = parent;
        block.StartInstructionIndex = compiler.Instructions.Count;
        Stack.Push(block);
        return block;
    }

    public override void Dispose() {
        EndInstructionIndex = _compiler.Instructions.Count;

        if (_parent != null)
            _parent.ChildrenEndInstructionIndex = Math.Max(_parent.ChildrenEndInstructionIndex, EndInstructionIndex);

        ByteCodeCompiler.Instance.Blocks.Add(InstructionBlockData.From(this));

        if (Stack.Count > 0)
            Stack.Pop();
        if (Stack.Count > 0)
            Global = Stack.Peek();

        base.Dispose();

    }
}

public struct InstructionBlockData
{
    public int Start { get; set; }
    public int End   { get; set; }

    public List<Instruction> Instructions => ByteCodeCompiler.Instance.Instructions.GetRange(Start, End - Start);

    public List<InstructionBlockData> Children = new();

    public InstructionBlockData() {
        Start = 0;
        End   = 0;
    }

    public static InstructionBlockData From(CompilerInstructionBlock block) {
        var blockData = new InstructionBlockData {
            Start = block.StartInstructionIndex,
            End   = block.EndInstructionIndex
        };

        foreach (var child in block.Children)
            blockData.Children.Add(From(child));

        return blockData;
    }
}