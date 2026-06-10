local M = {}

function M.doSomething()
    print("Action executed from boss_actions.lua module!")
end

function M.calculateValue(x)
    return x * 2
end

return M