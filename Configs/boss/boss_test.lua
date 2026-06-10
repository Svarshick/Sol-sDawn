local actions = require("boss/actions/boss_actions")
actions.doSomething()
local result = actions.calculateValue(10)
print("Result is: " .. tostring(result))

while true do 
    print("bla")
    wait()
end