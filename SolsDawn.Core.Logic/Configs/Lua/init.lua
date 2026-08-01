require("test/move_input")
subroutine(test)
while true do
    coroutine.yield()
end
--[[
subroutine("player")
subroutine("actions/simple_attack")
while true do
    run("player_controller")
end ]]