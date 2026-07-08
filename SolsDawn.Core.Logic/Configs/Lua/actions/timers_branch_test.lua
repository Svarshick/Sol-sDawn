-- ============================================================
--  Demo: Race, Routines, Branches – using only print
--  Run this as an enemy action (e.g. boss/demo.lua)
-- ============================================================

-- Helper: a now‑fired event (timer(0) fires on next update)
local scale = 1
local start = timer(0)

-- 1) Parallel warning routine (runs immediately, ends after 2s)
local warning = start:onFire(function()
    print("⚠️  Warning started")
    wait(timer(2 * scale))      -- wait 2 seconds (timer fires, event ends)
    print("⚠️  Warning ended")
end)
-- warning is a LuaRoutine, we can kill it later

-- 2) Timers for the parry window
local pw_open  = start:after(0.3 * scale)     -- opens after 0.3s
local pw_close = pw_open:after(0.4 * scale)   -- closes 0.4s after opening

pw_open:onFire(function()
    print("🔓 Parry window OPEN")
end)

pw_close:onFire(function()
    print("🔒 Parry window CLOSED (no parry)")
end)

-- 3) Simulated player parry: a timer that fires inside the window
local parried = start:after(0.5 * scale)      -- fires at 0.5s (window closes at 0.7s)
parried:onFire(function()
    print("⚔️  Player PARRY!")
end)

-- 4) Race: who wins? parried vs window‑close
local branch = race(parried, pw_close)
branch:onEnd(function()
    print("🏁 Branch resolved, cleaning up warning")
    warning:kill()                    -- end the warning routine
end)

-- 5) Parry path (will win because parried fires first)
local parryRoutine = branch:onWinner(parried):onFire(function()
    print("💥 Parry reaction: start")
    wait(timer(1))                    -- reaction lasts 1s
    print("💥 Parry reaction: finish")
end)

-- 6) Attack path (should be cancelled, so this never runs)
local attackRoutine = branch:onWinner(pw_close):onFire(function()
    print("🗡️  Attack: start")
    wait(timer(0.8 * scale))
    print("🗡️  Attack: finish")
end)

-- 7) Get the completion events of the two possible outcomes
--     (Assuming LuaRoutineProxy has a .finished property – see note below)
local parryEnd  = parryRoutine.finished
local attackEnd = attackRoutine.finished

-- 8) Whole action ends when whichever path finishes
local actionEnd = race(parryEnd, attackEnd)
print("⏳ Waiting for action to end…")
wait(actionEnd.finished)
print("✅ Action complete")
