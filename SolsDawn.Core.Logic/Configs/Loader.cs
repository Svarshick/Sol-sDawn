using System;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;
using SolsDawn.Core.Logic.Gameplay.Behaviour;
using SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;
using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Configs;

public static class Loader
{
    public static Script CreateLuaScript(FightBlackboard blackboard)
    {
        UserData.RegisterType<Vector2>();
        UserData.RegisterType<OrbStats>();
        UserData.RegisterType<Color>();

        var script = new Script(CoreModules.Basic | CoreModules.Metatables | CoreModules.LoadMethods);

        script.Globals["CreateVector"] = (Func<float, float, Vector2>)((x, y) => new Vector2(x, y));
        script.Globals["Units"] = (Func<float, float, Vector2>)((x, y) => blackboard.Layout.ToPixels(new Vector2(x, y)));
        script.Globals["UnitsFloat"] = (Func<float, float>)(units => blackboard.Layout.ToPixels(units));
        script.Globals["Rotate"] = (Func<Vector2, float, Vector2>)((v, r) => Vector2.Rotate(v, r));
        script.Globals["Normalize"] = (Func<Vector2, Vector2>)(v => Vector2.Normalize(v));
        
        script.Globals["GetPlayerPosition"] = (Func<Vector2>)(() => blackboard.Player.GameObject.Transform.Position);
        script.Globals["GetBossPosition"] = (Func<Vector2>)(() => blackboard.Boss.GameObject.Transform.Position);
        
        script.Globals["DefaultOrbStats"] = MainConfig.DefaultOrbStats;
        script.Globals["AlnoraRecoilOrbsStats"] = MainConfig.AlnoraRecoilOrbsStats;
        
        script.Globals["IsBossLastBladeParried"] = (Func<bool>)(() => blackboard.IsBossLastBladeParried);
        script.Globals["IsBossLastFireParried"] = (Func<bool>)(() => blackboard.IsBossLastFireParried);
        script.Globals["IsBossLastBladeSuccess"] = (Func<bool>)(() => blackboard.IsBossLastBladeSuccess);
        script.Globals["IsBossLastFireSuccess"] = (Func<bool>)(() => blackboard.IsBossLastFireSuccess);
        
        script.Globals["GetCameraCenter"] = (Func<Vector2>)(() => blackboard.Layout.CameraCenter());
        script.Globals["GetCameraTopLeft"] = (Func<Vector2>)(() => blackboard.Layout.CameraTopLeft());
        script.Globals["GetCameraTopRight"] = (Func<Vector2>)(() => blackboard.Layout.CameraTopRight());
        script.Globals["GetCameraBottomLeft"] = (Func<Vector2>)(() => blackboard.Layout.CameraBottomLeft());
        script.Globals["GetCameraBottomRight"] = (Func<Vector2>)(() => blackboard.Layout.CameraBottomRight());

        script.Globals["Wait"] = (Func<float, DynValue>)(seconds =>
        {
            var state = new Boss.IdleState(blackboard.Boss, seconds);
            IntentionsPool.Add(State.Intend(blackboard.Boss.GameObject, state));
            return DynValue.NewYieldReq(Array.Empty<DynValue>());
        });

        script.Globals["Teleport"] = (Func<Vector2, DynValue>)(position =>
        {
            var state = new Boss.TeleportState(blackboard.Boss, position);
            IntentionsPool.Add(State.Intend(blackboard.Boss.GameObject, state));
            return DynValue.NewYieldReq(Array.Empty<DynValue>());
        });

        script.Globals["Blade"] = (Func<Vector2, DynValue>)(lookPosition =>
        {
            var state = new Boss.BladeTelegraphState(blackboard.Boss, blackboard, lookPosition);
            IntentionsPool.Add(State.Intend(blackboard.Boss.GameObject, state));
            return DynValue.NewYieldReq(Array.Empty<DynValue>());
        });

        script.Globals["Fire"] = (Func<Vector2, DynValue>)(lookPosition =>
        {
            var state = new Boss.FireTelegraphState(blackboard.Boss, blackboard, () => lookPosition);
            IntentionsPool.Add(State.Intend(blackboard.Boss.GameObject, state));
            return DynValue.NewYieldReq(Array.Empty<DynValue>());
        });

        // Instant Action Commands
        script.Globals["SpawnOrb"] = (Action<Vector2, Vector2, OrbStats>)((position, target, stats) =>
        {
            blackboard.OrbController.Spawn(position, () => target, stats);
        });

        string setupMetatableScript = @"
            local v_test = CreateVector(0, 0)
            local meta = getmetatable(v_test)
            if meta then
                meta.__add = function(a, b) return CreateVector(a.X + b.X, a.Y + b.Y) end
                meta.__sub = function(a, b) return CreateVector(a.X - b.X, a.Y - b.Y) end
                meta.__mul = function(a, b)
                    if type(b) == 'number' then return CreateVector(a.X * b, a.Y * b) end
                    if type(a) == 'number' then return CreateVector(b.X * a, b.Y * a) end
                    return CreateVector(a.X * b.X, a.Y * b.Y)
                end
                meta.__div = function(a, b) return CreateVector(a.X / b, a.Y / b) end
                meta.__tostring = function(v) return string.format('Vector2(%f, %f)', v.X, v.Y) end
            end
        ";
        script.DoString(setupMetatableScript);

        return script;
    }
}