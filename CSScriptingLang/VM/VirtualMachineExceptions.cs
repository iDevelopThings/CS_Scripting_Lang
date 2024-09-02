namespace CSScriptingLang.VM;

public class VirtualMachineExceptions { }

public class VMVariableNotFoundException : Exception
{
    public VMVariableNotFoundException(string variableName)
        : base($"Variable '{variableName}' not found") { }
}