v1 = vector(0, 0)
v2 = vector(5, 0)
c = color(257, 257, 257)

l = animation.line(v1, v2, c)

while true do
    l.point2 = rotate(l.point2, 0.1)
    print('draw')
    coroutine.yield()
end 