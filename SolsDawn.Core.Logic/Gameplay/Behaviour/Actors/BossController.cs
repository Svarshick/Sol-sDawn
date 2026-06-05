using System;
using System.IO;
using MoonSharp.Interpreter;
using SolsDawn.Core.Logic.Configs;
using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

public class BossController
{
    private readonly FightBlackboard _blackboard;
    private readonly Script _script;
    private DynValue _coroutine;
    private bool _isScriptFinished;

    public BossController(FightBlackboard blackboard, string scriptPath)
    {
        _blackboard = blackboard;
        _script = Loader.CreateLuaScript(blackboard);
        
        LoadScript(scriptPath);
    }

    private void LoadScript(string scriptPath)
    {
        try
        {
            if (!File.Exists(scriptPath))
            {
                var errorMsg = $"Script file not found at path: {scriptPath}";
                LogLoudError(errorMsg, "FILE NOT FOUND");
                throw new FileNotFoundException(errorMsg);
            }

            Console.WriteLine($"[LUA] Loading script: {scriptPath}");
            string code = File.ReadAllText(scriptPath);
            DynValue function = _script.LoadString(code, null, scriptPath);
            _coroutine = _script.CreateCoroutine(function);
        }
        catch (InterpreterException ex)
        {
            LogLoudError(ex.DecoratedMessage, "LUA COMPILATION ERROR");
            throw new LogicException($"Lua compilation failed:\n{ex.DecoratedMessage}");
        }
        catch (Exception ex)
        {
            LogLoudError(ex.Message, "SCRIPT SETUP ERROR");
            throw;
        }
    }

    public void Update()
    {
        if (_isScriptFinished || _coroutine == null)
            return;

        if (_blackboard.Boss.State is Boss.PendingState)
        {
            try
            {
                if (_coroutine.Coroutine.State == CoroutineState.NotStarted)
                {
                    Console.WriteLine("[LUA] Initiating boss behavior coroutine.");
                    _coroutine.Coroutine.Resume();
                }
                else if (_coroutine.Coroutine.State == CoroutineState.Suspended)
                {
                    _coroutine.Coroutine.Resume();
                }

                if (_coroutine.Coroutine.State == CoroutineState.Dead)
                {
                    _isScriptFinished = true;
                    Console.WriteLine("[LUA] Boss behavior script execution completed.");
                }
            }
            catch (InterpreterException ex)
            {
                LogLoudError(ex.DecoratedMessage, "LUA RUNTIME ERROR");
                _isScriptFinished = true;
                throw new LogicException($"Lua execution failed at runtime:\n{ex.DecoratedMessage}");
            }
            catch (Exception ex)
            {
                LogLoudError(ex.Message, "NATIVE ERROR DURING LUA EXECUTION");
                _isScriptFinished = true;
                throw;
            }
        }
    }

    /// <summary>
    /// Outputs a highly visible, formatted console block when a script exception is encountered.
    /// </summary>
    private void LogLoudError(string message, string errorType)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"{errorType}");
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }
}