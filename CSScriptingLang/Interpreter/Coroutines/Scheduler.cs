using CSScriptingLang.Core.Logging;

namespace CSScriptingLang.Interpreter.Coroutines;

public class Scheduler
{
    private static Logger Logger = Logs.Get<Scheduler>();

    public Interpreter Interpreter { get; set; }

    // private List<CoroutineExecutionContext> Active = new();

    public Scheduler(Interpreter interpreter) {
        Interpreter = interpreter;
    }

    // public void AddCoroutine(CoroutineExecutionContext coroutineContext) {
        // Active.Add(coroutineContext);
    // }

    public void RunCoroutines() {
        /*foreach (var coroutineContext in Active.ToList()) // Use ToList to avoid modifying the list during iteration
        {
            if (coroutineContext.WakeUpTime.HasValue && DateTime.Now < coroutineContext.WakeUpTime.Value) {
                continue; // Skip this coroutine, not ready to resume yet
            }

            coroutineContext.WakeUpTime = null;

            if (!coroutineContext.IsCompleted) {
                ExecuteCoroutine(coroutineContext);
            } else {
                Active.Remove(coroutineContext);
            }
        }*/
    }

    /*private void ExecuteCoroutine(CoroutineExecutionContext coroutineContext) {
        // Restore context
        var currentContext = coroutineContext.Interpreter.Context;
        coroutineContext.Interpreter.Context = coroutineContext;

        switch (coroutineContext.ProgramCounter) {
            case 0:
                // Begin execution of coroutine
                Logger.Debug("Coroutine started");
                coroutineContext.ProgramCounter++;
                return; // Yield here

            case 1:
                // Simulate execution and state saving
                Logger.Debug("Coroutine executing...");
                coroutineContext.WakeUpTime = DateTime.Now.AddSeconds(2);
                return; // Suspend and yield

            case 2:
                // Resume and complete coroutine
                Logger.Debug("Coroutine resumed and completed");
                coroutineContext.IsCompleted = true;
                break;
        }

        // Restore the original context
        coroutineContext.Interpreter.Context = currentContext;
    }*/
    
    public void Tick() {
        RunCoroutines();
    }
    public bool HasActiveCoroutines() {
        return false; // Active.Any();
    }
}