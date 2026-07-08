sColor = color(0, 50, 257)
eColor = color(0, 0, 0, 0)

while true do
    target = vector(3, 2)
    line = animation.lineTrace(boss_position, target, 10, 0.5, sColor, eColor)
    boss_position = target
    wait(1)
    target = vector(-3, 2)
    line = animation.lineTrace(boss_position, target, 10, 0.5, sColor, eColor)
    boss_position = target
    wait(1)
end 