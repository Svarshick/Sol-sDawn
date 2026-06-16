p1 = vector(0, 0)
p2 = vector(5, 0)
sColor = color(0, 257, 257)
eColor = color(0, 0, 0, 0)

while true do
    t0 = timer(0.1)
    trace = animation.lineTrace(p1, p2, 10, 0.5, sColor, eColor)
    wait(t0)
    p2 = rotate(p2, 0.2)
end 